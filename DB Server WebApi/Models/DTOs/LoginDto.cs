using System.ComponentModel.DataAnnotations;

namespace DB_Server_WebApi.DTOs
{
    // DTOs/LoginDto.cs
    public class LoginDto
    {
        [Required]
        public string Login { get; set; }// Peut être le nom d'utilisateur OU l'email

        [Required]
        public string Password { get; set; }
    }
}
