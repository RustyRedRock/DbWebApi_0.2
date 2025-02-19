using System.ComponentModel.DataAnnotations;

namespace DB_Server_WebApi.DTOs
{
    // DTOs/RegisterDto.cs
    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(32, MinimumLength = 8)]
        [DataType(DataType.Password)] // Optionnel, mais utile
        public string Password { get; set; }
    }
}
