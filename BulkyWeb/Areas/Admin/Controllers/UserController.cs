using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        [BindProperty]
        public UserRoleVM userRoleVM { get; set; }
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public UserController(IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult RoleManagement(string userId)
        {
            userRoleVM = new UserRoleVM()
            {
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company"),
                Roles = _roleManager.Roles.Select(i => new SelectListItem()
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                Companies = _unitOfWork.Company.GetAll().Select(i => new SelectListItem()
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };
            userRoleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
            return View(userRoleVM);
        }
        [HttpPost]
        public IActionResult RoleManagement()
        {
            string oldRoleName = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userRoleVM.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();
            ApplicationUser userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == userRoleVM.ApplicationUser.Id);

            if (!(userRoleVM.ApplicationUser.Role == oldRoleName))
            {
                
                if(userRoleVM.ApplicationUser.Role == SD.Role_Company)
                {
                    userFromDb.CompanyId = userRoleVM.ApplicationUser.CompanyId;
                }
                if (oldRoleName == SD.Role_Company)
                {
                    userFromDb.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(userFromDb);
                _unitOfWork.Save();
                _userManager.RemoveFromRoleAsync(userFromDb, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, userRoleVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else if (userRoleVM.ApplicationUser.Role == SD.Role_Company)
            {
                if (userFromDb.CompanyId != userRoleVM.ApplicationUser.CompanyId)
                {
                    userFromDb.CompanyId = userRoleVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(userFromDb);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction(nameof(Index));
        }
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var allObj = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company");

            foreach (var obj in allObj)
            {
                obj.Role = _userManager.GetRolesAsync(obj).GetAwaiter().GetResult().FirstOrDefault();

                if (obj.Company == null)
                {
                    obj.Company = new() { Name = ""};
                }
            }
            return Json(new { data = allObj });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
            if (userFromDb == null)
            {
                return Json(new { success = false, message = "Error while locking/unlocking" });
            }

            if (userFromDb.LockoutEnd != null && userFromDb.LockoutEnd > DateTime.Now)
            {
                userFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                userFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(userFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Locked/Unlocked successful" });
        }
        #endregion
    }
}
