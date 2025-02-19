namespace DB_Server_WebApi.Models
{
    // Models/JwtOptions.cs (ou Options/JwtOptions.cs)
    public class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty; // Optionnel
        public string Audience { get; set; } = string.Empty; // Optionnel
        // Vous pouvez ajouter d'autres options ici si besoin (par exemple, ExpireDays)
    }
}
