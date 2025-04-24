using BulkyBookWebRazor.Data;
using BulkyBookWebRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyBookWebRazor.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        [BindProperty]
        public Category Category { get; set; }
        public DeleteModel(ApplicationDbContext db)
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
        public IActionResult OnPost()
        {
            if (Category != null)
            {
                TempData["success"] = "The category has been deleted successfully";
                _db.Category.Remove(Category);
                _db.SaveChanges();
            }
            return RedirectToPage("Index");
        }
    }
}
