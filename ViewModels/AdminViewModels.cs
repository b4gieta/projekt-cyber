using System.ComponentModel.DataAnnotations;

namespace projekt_cyber.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Podaj login.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Login musi mieć od 3 do 50 znaków.")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Wybierz rolę.")]
        public string Role { get; set; } = "Czytelnik";
    }

    public class AdminUserRowViewModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsBlocked { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public class CreatedUserViewModel
    {
        public string UserName { get; set; } = "";
        public string OneTimePassword { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    public class ChangeInitialPasswordViewModel
    {
        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Powtórz nowe hasło.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są identyczne.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
