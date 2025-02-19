using Microsoft.AspNetCore.Identity;

namespace DB_Server_WebApi.Models
{
    // Models/User.cs
    public class User : IdentityUser
    {
        // Propriétés héritées d'IdentityUser (vous n'avez pas besoin de les redéfinir) :
        // - Id (string, GUID)
        // - UserName (string)
        // - NormalizedUserName (string)
        // - Email (string)
        // - NormalizedEmail (string)
        // - EmailConfirmed (bool)
        // - PasswordHash (string)
        // - SecurityStamp (string)
        // - ConcurrencyStamp (string)
        // - PhoneNumber (string)
        // - PhoneNumberConfirmed (bool)
        // - TwoFactorEnabled (bool)
        // - LockoutEnd (DateTimeOffset?)
        // - LockoutEnabled (bool)
        // - AccessFailedCount (int)

        // Propriétés spécifiques au JEU (ajoutées à la classe User)
        public int Credits { get; set; } = 1000; // Exemple : Argent du joueur
        public int Experience { get; set; } = 0;   // Exemple : Points d'expérience

        //Exemple, des relation avec d'autre tables
        // public List<InventoryItem> Inventory { get; set; } = new();
        // ... autres propriétés de jeu ...
    }
}
