using System.ComponentModel.DataAnnotations;

namespace DB_Server_WebApi.Models.DTOs
{
    public class ForgotPasswordDto    {
        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        public string Email { get; set; }
    }
}
