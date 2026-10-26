using System.ComponentModel.DataAnnotations;

namespace projekt_cyber.ViewModels
{
    public class EntryCreateViewModel
    {
        [Required(ErrorMessage = "Podaj tytuł.")]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Podaj treść.")]
        public string Content { get; set; } = "";
    }
}
