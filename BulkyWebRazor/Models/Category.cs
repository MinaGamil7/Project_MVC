using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BulkyBookWebRazor.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        [MinLength(4)]
        [DisplayName("Category Name")]
        public string Name { get; set; }
        [Range(1, 100, ErrorMessage = "The order must be 1-100")]
        public int DisplayOrder { get; set; }
    }
}
