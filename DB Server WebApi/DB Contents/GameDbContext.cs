
using DB_Server_WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DB_Server_WebApi.DB_Contents
{
    public class GameDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }
        
        public new DbSet<User> Users { get; set; }
        public DbSet<World> Worlds { get; set; }
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Cell> Cells { get; set; } // Optionnel, si les secteurs ont des sous-grilles
        public DbSet<Planet> Planets { get; set; }
        public DbSet<Asteroid> Asteroids { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configuration des Schémas ---
            modelBuilder.HasDefaultSchema("public"); //On remet le schema par default a public (optionnel)

            // Configuration pour le schéma 'auth' (pour Identity)
            modelBuilder.Entity<User>().ToTable("Users", "auth"); // Place User dans le schéma auth
            modelBuilder.Entity<IdentityRole>().ToTable("Roles", "auth");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "auth");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "auth");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "auth");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "auth");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "auth");


            // Configuration pour le schéma 'game'
            modelBuilder.Entity<World>().ToTable("Worlds", "game");
            modelBuilder.Entity<Sector>().ToTable("Sectors", "game");
            modelBuilder.Entity<Cell>().ToTable("Cells", "game");
            modelBuilder.Entity<Planet>().ToTable("Planets", "game");
            modelBuilder.Entity<Asteroid>().ToTable("Asteroids", "game");

            // --- Configuration des Entités (Uniquement les propriétés personnalisées et les relations) ---

            // Configuration de User (uniquement les propriétés personnalisées)
            // Vous pouvez laisser ce bloc vide si vous êtes satisfait des conventions par défaut pour Credits et Experience.
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Credits).IsRequired(); //Par default, les int sont required
                entity.Property(u => u.Experience).IsRequired();
            });


            // World
            modelBuilder.Entity<World>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(w => w.Name).IsUnique();
            });

            // Sector (Macro-cellule)
            modelBuilder.Entity<Sector>(entity =>
            {
                entity.HasKey(s => new { s.WorldId, s.X, s.Y }); // Clé composite
                entity.Property(s => s.X).IsRequired();
                entity.Property(s => s.Y).IsRequired();
                entity.Property(s => s.HasSubGrid).IsRequired(); // Indique si le secteur a une sous-grille

                entity.HasOne(s => s.World)
                    .WithMany(w => w.Sectors)
                    .HasForeignKey(s => s.WorldId)
                    .OnDelete(DeleteBehavior.Cascade); // Suppression en cascade

                //Relations optionnelles, un secteur peut contenir 0 ou 1 Planet, 0 ou plusieurs asteroids
                entity.HasOne(s => s.Planet)
                    .WithOne(p => p.Sector)
                    .HasForeignKey<Planet>(p => new { p.WorldId, p.SectorX, p.SectorY })
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(s => s.Asteroids)
                    .WithOne(a => a.Sector)
                    .HasForeignKey(a => new { a.WorldId, a.SectorX, a.SectorY })
                    .OnDelete(DeleteBehavior.SetNull); //Ou Cascade, selon votre choix
            });

            // Cell (Micro-cellule, optionnel)
            modelBuilder.Entity<Cell>(entity =>
            {
                entity.HasKey(c => new { c.WorldId, c.SectorX, c.SectorY, c.X, c.Y }); // Clé composite
                entity.Property(c => c.X).IsRequired();
                entity.Property(c => c.Y).IsRequired();

                entity.HasOne(c => c.Sector)
                    .WithMany(s => s.Cells)
                    .HasForeignKey(c => new { c.WorldId, c.SectorX, c.SectorY })
                    .OnDelete(DeleteBehavior.Cascade); // Important: suppression en cascade

                // Si vous avez des objets DANS les cellules (pas au niveau du secteur), ajoutez les relations ici.
            });

            // Planet
            modelBuilder.Entity<Planet>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                // Les coordonnées sont maintenant au niveau du SECTEUR.
                entity.Property(p => p.WorldId).IsRequired();
                entity.Property(p => p.SectorX).IsRequired();
                entity.Property(p => p.SectorY).IsRequired();

                entity.HasIndex(p => new { p.WorldId, p.SectorX, p.SectorY }).IsUnique(); //Une seule planet par secteur

                // La ForeignKey est déjà définie dans Sector
            });

            // Asteroid
            modelBuilder.Entity<Asteroid>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ResourceType).IsRequired().HasMaxLength(50); // Type de ressource
                                                                                    // Coordonnées au niveau du SECTEUR.
                entity.Property(a => a.WorldId).IsRequired();
                entity.Property(a => a.SectorX).IsRequired();
                entity.Property(a => a.SectorY).IsRequired();

                //Pas d'unicité ici, on peut avoir plusieurs astéroides dans un secteur.
                entity.HasIndex(a => new { a.WorldId, a.SectorX, a.SectorY });

                // La ForeignKey est déjà définie dans Sector

            });
        }
    }

    // Classes d'entités
    public class World
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<Sector> Sectors { get; set; } = new List<Sector>(); // Navigation Property
    }

    public class Sector
    {
        public int WorldId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool HasSubGrid { get; set; } // Indique si ce secteur contient une sous-grille de Cellules
        public required World World { get; set; }     // Navigation Property
        public List<Cell> Cells { get; set; } = new List<Cell>(); // Navigation Property (optionnelle)

        public Planet? Planet { get; set; } //Navigation Property
        public List<Asteroid> Asteroids { get; set; } = new List<Asteroid>(); //Navigation Property


    }
    //Optionnel
    public class Cell
    {
        public int WorldId { get; set; }
        public int SectorX { get; set; }
        public int SectorY { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public required Sector Sector { get; set; }// Navigation Property

        // Ajoutez ici les propriétés/relations pour les objets DANS les cellules (si nécessaire)
    }

    public class Planet
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int WorldId { get; set; }
        public int SectorX { get; set; }
        public int SectorY { get; set; }

        public required Sector Sector { get; set; }//Navigation
    }

    public class Asteroid
    {
        public int Id { get; set; }
        public required string ResourceType { get; set; }
        public int WorldId { get; set; }
        public int SectorX { get; set; }
        public int SectorY { get; set; }

        public required Sector Sector { get; set; } //Navigation

    }
}
