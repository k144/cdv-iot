
using System.ComponentModel.DataAnnotations;

namespace Database.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Role { get; set; } // e.g., "Admin", "User"
    }
}
