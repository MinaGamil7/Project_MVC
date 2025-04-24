using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _hostingEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment hostingEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostingEnvironment = hostingEnvironment;
        }
        public IActionResult Index()
        {
            var objList = _unitOfWork.Product.GetAll(includeProperties:"Category");
            return View(objList);
        }
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };
            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category,ProductImages");
                if (productVM.Product == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM obj, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (obj.Product.Id != 0)
                {
                    TempData["success"] = "The product has been updated successfully";
                    _unitOfWork.Product.Update(obj.Product);
                }
                else
                {

                    TempData["success"] = "The product has been created successfully";
                    _unitOfWork.Product.Add(obj.Product);
                }
                _unitOfWork.Save();
                string webRootPath = _hostingEnvironment.WebRootPath;
                if (files != null)
                {
                    foreach (var file in files) {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"Images\Products\Product-" + obj.Product.Id;
                        var finalPath = Path.Combine(webRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new ProductImage()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = obj.Product.Id
                        };

                        if (obj.Product.ProductImages == null)
                        {
                            obj.Product.ProductImages = new List<ProductImage>();
                        }

                        obj.Product.ProductImages.Add(productImage);
                    }
                    TempData["success"] = "The product has been updated successfully";
                    _unitOfWork.Product.Update(obj.Product);
                    _unitOfWork.Save();

                }

                return RedirectToAction("Index", "Product");
            }
            else
            {
                ProductVM productVM = new ProductVM()
                {
                    Product = new Product(),
                    CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                    {
                        Text = u.Name,
                        Value = u.Id.ToString()
                    })
                };
                return View(productVM);
            }
        }

        public IActionResult DeleteImage(int ImageId)
        {
            var objFromDb = _unitOfWork.ProductImage.Get(u => u.Id == ImageId);
            if (objFromDb != null)
            {
                string webRootPath = _hostingEnvironment.WebRootPath;
                if (objFromDb.ImageUrl != null)
                {
                    var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _unitOfWork.ProductImage.Remove(objFromDb);
                _unitOfWork.Save();
                TempData["success"] = "The image has been deleted successfully";
            }
            return RedirectToAction(nameof(Upsert), new { id = objFromDb.ProductId });
        }
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int? id) {
            var obj = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Category");
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string productPath = @"Images\Products\Product-" + id;
            var finalPath = Path.Combine(_hostingEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] files = Directory.GetFiles(finalPath);
                foreach (var file in files)
                {
                    System.IO.File.Delete(file);
                }
                Directory.Delete(finalPath);
            }
            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete successful" });
        }
        #endregion
    }
}