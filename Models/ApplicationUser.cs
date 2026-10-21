using Microsoft.AspNetCore.Identity;

namespace projekt_cyber.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsBlocked { get; set; } = false;
        public bool MustChangePassword { get; set; } = false;
        public DateTime? PasswordExpiresAt { get; set; }
        public DateTime? OneTimePasswordExpiresAt { get; set; }
    }
}
