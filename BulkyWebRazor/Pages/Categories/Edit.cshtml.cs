using BulkyBookWebRazor.Data;
using BulkyBookWebRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyBookWebRazor.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        [BindProperty]
        public Category Category { get; set; }
        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }
        public void OnGet(int? Id)
        {
            if (Id != null && Id != 0)
            {
                Category = _db.Category.Find(Id);
            }
        }
        public IActionResult OnPost() {
            TempData["success"] = "The category has been updated successfully";
            _db.Category.Update(Category);
            _db.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
