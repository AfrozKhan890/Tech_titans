using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketLink.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(120)]
        public string? Slug { get; set; }
        public int? ParentId { get; set; }
        [MaxLength(100)]
        public string? IconClass { get; set; }
        [MaxLength(300)]
        public string? ImageUrl { get; set; }
        public int SortOrder { get; set; }

        [NotMapped] public int Id => CategoryId;
        [NotMapped] public Category? Parent { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        [NotMapped] public ICollection<Category> Children { get; set; } = new List<Category>();
    }
}
