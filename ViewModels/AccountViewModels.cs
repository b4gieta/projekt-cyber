using System.ComponentModel.DataAnnotations;

namespace projekt_cyber.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Podaj login.")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Podaj hasło.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Podaj aktualne hasło.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Powtórz nowe hasło.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są identyczne.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
