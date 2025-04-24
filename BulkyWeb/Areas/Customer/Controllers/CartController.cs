using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitofwork;
        [BindProperty]
        public ShoppingCartVM cartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitofwork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim,includeProperties: "Product"),
                orderHeader = new OrderHeader()
            };
            IEnumerable<ProductImage> productImages = _unitofwork.ProductImage.GetAll();
            foreach (var list in cartVM.ShoppingCartList)
            {
                list.Product.ProductImages = productImages.Where(u => u.ProductId == list.Product.Id).ToList();
                list.Price = GetPriceBasedOnQuantity(list);
                cartVM.orderHeader.OrderTotal += list.Price * list.Count;
            }
            return View(cartVM);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim, includeProperties: "Product"),
                orderHeader = new OrderHeader()
            };
            cartVM.orderHeader.ApplicationUser = _unitofwork.ApplicationUser.Get(u => u.Id == claim);
            cartVM.orderHeader.Name = cartVM.orderHeader.ApplicationUser.Name;
            cartVM.orderHeader.PhoneNumber = cartVM.orderHeader.ApplicationUser.PhoneNumber;
            cartVM.orderHeader.PostalCode = cartVM.orderHeader.ApplicationUser.PostalCode;
            cartVM.orderHeader.State = cartVM.orderHeader.ApplicationUser.State;
            cartVM.orderHeader.City = cartVM.orderHeader.ApplicationUser.City;
            cartVM.orderHeader.StreetAddress = cartVM.orderHeader.ApplicationUser.StreetAddress;
            foreach (var list in cartVM.ShoppingCartList)
            {
                list.Price = GetPriceBasedOnQuantity(list);
                cartVM.orderHeader.OrderTotal += list.Price * list.Count;
            }
            return View(cartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cartVM.ShoppingCartList = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim, includeProperties: "Product");
            ApplicationUser applicationUser = _unitofwork.ApplicationUser.Get(u => u.Id == claim);
            cartVM.orderHeader.OrderDate = System.DateTime.Now;
            cartVM.orderHeader.ApplicationUserId = claim;

            foreach (var list in cartVM.ShoppingCartList)
            {
                list.Price = GetPriceBasedOnQuantity(list);
                cartVM.orderHeader.OrderTotal += list.Price * list.Count;
            }

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                cartVM.orderHeader.PaymentStatus = SD.PaymentStatusPending;
                cartVM.orderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                cartVM.orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                cartVM.orderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitofwork.OrderHeader.Add(cartVM.orderHeader);
            _unitofwork.Save();

            foreach (var list in cartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = list.ProductId,
                    OrderHeaderId = cartVM.orderHeader.Id,
                    Price = GetPriceBasedOnQuantity(list),
                    Count = list.Count
                };
                _unitofwork.OrderDetail.Add(orderDetail);
                _unitofwork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = "https://localhost:7180/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={cartVM.orderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach(var item in cartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title,
                            },
                            UnitAmount = (long)(item.Product.Price * 100),
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);

                _unitofwork.OrderHeader.UpdateStripePaymentId(cartVM.orderHeader.Id, session.Id, session.PaymentIntentId);
                _unitofwork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            return RedirectToAction(nameof(OrderConfirmation), new { id = cartVM.orderHeader.Id });
        }
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitofwork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitofwork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitofwork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitofwork.Save();
                }
                HttpContext.Session.Clear();
            }
            List<ShoppingCart> shoppingCarts = _unitofwork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitofwork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitofwork.Save();

            return View(id);
        }
        public IActionResult Remove(int cartId)
        {
            var cart = _unitofwork.ShoppingCart.Get(u => u.Id == cartId);
            _unitofwork.ShoppingCart.Remove(cart);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitofwork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
            _unitofwork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Plus(int cartId)
        {
            var cart = _unitofwork.ShoppingCart.Get(u => u.Id == cartId);
            cart.Count += 1;
            _unitofwork.ShoppingCart.Update(cart);
            _unitofwork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cartId)
        {
            var cart = _unitofwork.ShoppingCart.Get(u => u.Id == cartId);
            if (cart.Count <= 1)
            {
                _unitofwork.ShoppingCart.Remove(cart);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitofwork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
            }
            else
            {
                cart.Count -= 1;
                _unitofwork.ShoppingCart.Update(cart);
            }
            _unitofwork.Save();
            return RedirectToAction(nameof(Index));
        }
        private double GetPriceBasedOnQuantity(ShoppingCart cart)
        {
            if(cart.Count <= 50)
            {
                return cart.Product.Price;
            }
            else if (cart.Count > 50 && cart.Count <= 100)
            {
                return cart.Product.Price50;
            }
            else
            {
                return cart.Product.Price100;
            }
        }
    }
}
