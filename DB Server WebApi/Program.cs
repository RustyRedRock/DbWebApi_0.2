using DB_Server_WebApi.DB_Contents;
using DB_Server_WebApi.Models;
using DB_Server_WebApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // Importez ce namespace !
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization; // Ajoutez
using System.Globalization; // Ajoutez


var builder = WebApplication.CreateBuilder(args);

// --- Configuration de la base de données ---
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GameDatabase")));

// --- Configuration d'Identity ---
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true; // Exiger la confirmation de l'e-mail
})
.AddEntityFrameworkStores<GameDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
    opt.TokenLifespan = TimeSpan.FromHours(3));

// --- Configuration de l'authentification par COOKIES ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Utilisez Always en production !
    options.Cookie.SameSite = SameSiteMode.Strict; // Strict en production ! Lax ou None en dev si besoin.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/auth/login";
    options.AccessDeniedPath = "/api/auth/accessdenied";
});

// --- Enregistrement des services ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(); // Votre implémentation de IEmailSender

// --- Configuration du logging ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- Ajout du contexte HTTP (optionnel) ---
// builder.Services.AddHttpContextAccessor(); // Décommentez si vous en avez besoin

// --- API Controllers ---
builder.Services.AddControllers();
builder.Services.AddAuthorization(); //Pour utiliser [Authorize]

// --- Configuration de CORS ---

// OPTION 1: Développement (permissif)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // IMPORTANT avec les cookies !
    });
});

// OPTION 2: Production (plus restrictif)
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("MyCorsPolicy", builder =>
//     {
//         builder.WithOrigins("https://yourgame.com") // Remplacez par l'URL de votre client WebGL
//                .AllowAnyMethod()
//                .AllowAnyHeader()
//                .AllowCredentials(); // IMPORTANT avec les cookies !
//     });
// });

// --- Configuration de SMTP ---
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// --- Blazor Server Configuration ---
builder.Services.AddRazorPages(); // Required for Blazor
builder.Services.AddServerSideBlazor(); // Adds Blazor Server services
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" }); // Important for WebAssembly/WebGL
});

// --- Global Translation ---
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
// Configurez la culture par défaut (optionnel, mais recommandé)
var defaultCulture = new CultureInfo("en-US"); // Ou "fr-FR", etc.
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;


var app = builder.Build();

// --- Middleware ---
app.UseResponseCompression(); // Enable response compression (Blazor)

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseWebAssemblyDebugging(); // Enable if you are using Blazor WebAssembly
}
else
{
    app.UseExceptionHandler("/Error"); // A Blazor error page.  Create this!
    app.UseHsts(); // Enforces HTTPS in production
}

app.UseHttpsRedirection(); // Redirige HTTP vers HTTPS (important en production)

// --- Serve Static Files (WebGL and Blazor) ---
app.UseStaticFiles(); // Serve files from wwwroot (including Blazor's _framework folder)

app.UseCors(); //  AVANT UseRouting, UseAuthentication et UseAuthorization
app.UseRouting();
app.UseAuthentication(); //  AVANT UseAuthorization
app.UseAuthorization();

app.MapControllers();

// --- Blazor Endpoints ---
app.MapBlazorHub();         // Sets up the SignalR hub for Blazor
app.MapFallbackToPage("/_Host"); // Handles routing to the Blazor app

// --- Migration de la base de données (DEV/TEST seulement) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<GameDbContext>();
        context.Database.Migrate(); // Applique les migrations
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();



// Command de migration

// dotnet ef migrations add InitialCreate --project "G:\Server Apps\DB Server WebApi\DB Server WebApi.csproj" --context GameDbContext
// dotnet ef database update --project "G:\Server Apps\DB Server WebApi\DB Server WebApi.csproj" --context GameDbContext