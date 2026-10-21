using System.ComponentModel.DataAnnotations;

namespace projekt_cyber.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Podaj login.")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Powtórz hasło.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Hasła nie są identyczne.")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Podaj e-mail.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = "";
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Podaj login.")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
