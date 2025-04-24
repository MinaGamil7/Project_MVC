using BulkyBookWebRazor.Data;
using BulkyBookWebRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyBookWebRazor.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        [BindProperty]
        public Category Category{ get; set; }
        public CreateModel(ApplicationDbContext db)
        {
            _db = db;
        }
        public void OnGet()
        {
        }
        public IActionResult OnPost()
        {
            TempData["success"] = "The category has been created successfully";
            _db.Category.Add(Category);
            _db.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
