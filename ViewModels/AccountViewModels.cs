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
}
