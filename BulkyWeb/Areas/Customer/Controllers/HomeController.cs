using System.Diagnostics;
using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitofwork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitofwork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitofwork.Product.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }

        public IActionResult Details(int id)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unitofwork.Product.Get(u => u.Id == id, includeProperties: "Category,ProductImages"),
                ProductId = id,
                Count = 1
            };
            return View(shoppingCart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart obj)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            obj.ApplicationUserId = claim;

            ShoppingCart cartFromDb = _unitofwork.ShoppingCart.Get(
                u => u.ApplicationUserId == claim && u.ProductId == obj.ProductId
            );
            if (cartFromDb == null)
            {
                _unitofwork.ShoppingCart.Add(obj);
                TempData["success"] = "Item has been added to cart";
                _unitofwork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitofwork.ShoppingCart.GetAll(
                    u => u.ApplicationUserId == claim).Count());

            }
            else
            {
                cartFromDb.Count += obj.Count;
                _unitofwork.ShoppingCart.Update(cartFromDb);
                _unitofwork.Save();
                TempData["success"] = "Item has been updated";
            }
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
