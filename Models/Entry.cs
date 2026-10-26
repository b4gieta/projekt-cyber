using System.ComponentModel.DataAnnotations;

namespace projekt_cyber.Models
{
    public class Entry
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string AuthorId { get; set; } = "";
        public ApplicationUser? Author { get; set; }
    }
}
