using DB_Server_WebApi.DB_Contents;
using DB_Server_WebApi.DTOs;
using DB_Server_WebApi.Logs;
using DB_Server_WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DB_Server_WebApi.Services
{
    /// <summary>
    /// Interface pour le service d'authentification.
    /// </summary>
    public interface IAuthService
    {
        Task SendPasswordResetEmailAsync(string email);
        Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task ResendConfirmationEmailAsync(string email);
        Task<IdentityResult> Register(RegisterDto registerDto);
        Task<string?> Login(LoginDto loginDto);
        Task ConfirmEmail(string userId, string token);
        Task SendConfirmationEmail(User user);
        Task<User?> GetUserByLoginAsync(string login);
    }

    /// <summary>
    /// Implémentation du service d'authentification.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly GameDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService> _logger; // Utilisation de ILogger<T>
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructeur du service d'authentification.
        /// </summary>
        /// <param name="context">Le contexte de base de données.</param>
        /// <param name="tokenService">Le service de génération de tokens.</param>
        /// <param name="emailSender">Le service d'envoi d'e-mails.</param>
        /// <param name="logger">Le service de logging.</param>
        /// <param name="userManager">Le gestionnaire d'utilisateurs.</param>
        /// <param name="signInManager">Le gestionnaire de connexion.</param>
        public AuthService(GameDbContext context,
                            IEmailSender emailSender,
                            ILogger<AuthService> logger, // Injection de ILogger<AuthService>
                            UserManager<User> userManager,
                            SignInManager<User> signInManager, 
                            IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration)); // Initialisez _configuration
        }

        /// <summary>
        /// Récupère un utilisateur par son nom d'utilisateur ou son adresse e-mail.
        /// </summary>
        /// <param name="login">Le nom d'utilisateur ou l'adresse e-mail.</param>
        /// <returns>L'utilisateur, ou null s'il n'est pas trouvé.</returns>
        public async Task<User?> GetUserByLoginAsync(string login)
        {
            // Recherche par nom d'utilisateur ou par e-mail (insensible à la casse)
            return await _userManager.Users
                .FirstOrDefaultAsync(u => u.UserName.ToLower() == login.ToLower() || u.Email.ToLower() == login.ToLower());
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur.
        /// </summary>
        /// <param name="registerDto">DTO contenant les informations d'enregistrement.</param>
        /// <returns>Un IdentityResult indiquant le succès ou l'échec de l'opération.</returns>
        public async Task<IdentityResult> Register(RegisterDto registerDto)
        {
            _logger.LogInformation("Tentative d'enregistrement pour l'utilisateur : {UserName}", registerDto.UserName);

            bool userNameExists = await _context.Users.AnyAsync(u => u.NormalizedUserName == registerDto.UserName.ToUpper()); //Utilisation de Normalized
            bool emailExists = await _context.Users.AnyAsync(u => u.NormalizedEmail == registerDto.Email.ToUpper());//Utilisation de Normalized

            if (userNameExists && emailExists)
            {
                throw new DuplicateUserException("Le nom d'utilisateur et l'adresse e-mail existent déjà.");
            }
            else if (userNameExists)
            {
                throw new DuplicateUserException("Le nom d'utilisateur existe déjà.");
            }
            else if (emailExists)
            {
                throw new DuplicateUserException("L'adresse e-mail existe déjà.");
            }

            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password); // Crée l'utilisateur

            if (result.Succeeded)
            {
                _logger.LogInformation("Utilisateur créé : {UserName}", user.UserName);
                await SendConfirmationEmail(user); // Envoie l'e-mail de confirmation
            }
            else
            {
                // Log des erreurs spécifiques d'Identity
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Erreur lors de la création de l'utilisateur {UserName}: {ErrorCode} - {ErrorDescription}", registerDto.UserName, error.Code, error.Description);
                }
            }

            return result;
        }


        /// <summary>
        /// Connecte un utilisateur existant.
        /// </summary>
        /// <param name="loginDto">DTO contenant les informations de connexion.</param>
        /// <returns>Un token JWT en cas de succès, ou null en cas d'échec.</returns>
        public async Task<string?> Login(LoginDto loginDto) //Changez le retour
        {
            _logger.LogInformation("Tentative de connexion pour : {Login}", loginDto.Login);

            var user = await _userManager.FindByNameAsync(loginDto.Login) ??
                        await _userManager.FindByEmailAsync(loginDto.Login);

            if (user == null)
            {
                _logger.LogWarning("Utilisateur introuvable : {Login}", loginDto.Login);
                throw new UserNotFoundException($"Utilisateur introuvable : '{loginDto.Login}'");
            }

            // Utilisez PasswordSignInAsync, PAS CheckPasswordSignInAsync
            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!user.EmailConfirmed)
                {
                    if (_signInManager.Options.SignIn.RequireConfirmedEmail)
                    {
                        _logger.LogWarning("Email non confirmé pour l'utilisateur : {Login}", loginDto.Login);
                        throw new EmailNotConfirmedException("Veuillez confirmer votre adresse e-mail.");
                    }
                }

                _logger.LogInformation("Connexion réussie pour l'utilisateur : {UserName}", user.UserName);
                return null; // Important: Retournez null (ou une autre valeur non-string)
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Compte verrouillé pour l'utilisateur : {Login}", loginDto.Login);
                throw new LockedOutException("Compte verrouillé. Veuillez réessayer plus tard."); // Créez cette exception
            }

            if (result.RequiresTwoFactor)
            {
                _logger.LogWarning("L'authentification a deux facteurs est requise pour l'utilisateur : {Login}", loginDto.Login);
                throw new TwoFactorRequiredException("L'authentification à deux facteurs est requise.");//Créez cette exception
            }

            _logger.LogWarning("Identifiants invalides pour l'utilisateur : {Login}", loginDto.Login);
            throw new InvalidCredentialsException("Nom d'utilisateur ou mot de passe incorrect.");
        }

        /// <summary>
        /// Envoie un e-mail de confirmation à l'utilisateur.
        /// </summary>
        /// <param name="user">L'utilisateur.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task SendConfirmationEmail(User user)
        {
            _logger.LogInformation("Envoi de l'email de confirmation à : {Email}", user.Email);

            if (user == null)
            {
                _logger.LogError("SendConfirmationEmail appelée avec un utilisateur null.");
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                _logger.LogError("L'utilisateur {UserId} n'a pas d'adresse e-mail.", user.Id);
                throw new InvalidOperationException("L'utilisateur n'a pas d'adresse e-mail.");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogDebug("Token généré : {Token}", token); // LOGUEZ LE TOKEN BRUT

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            _logger.LogDebug("Token encodé : {EncodedToken}", encodedToken); // LOGUEZ LE TOKEN ENCODÉ

            var baseUrl = _configuration["ConfirmEmailUrlBase"] ?? throw new InvalidOperationException("ConfirmEmailUrlBase is not configured.");
            var uriBuilder = new UriBuilder(baseUrl);
            uriBuilder.Query = $"userId={user.Id}&token={encodedToken}";
            var confirmEmailUrl = uriBuilder.ToString();

            try
            {
                await _emailSender.SendEmailAsync(user.Email, "Confirmez votre email",
                    $"Veuillez confirmer votre compte en cliquant sur ce lien: <a href='{confirmEmailUrl}'>lien</a>");
                _logger.LogInformation("Email de confirmation envoyé à : {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'e-mail de confirmation à : {Email}", user.Email);
                throw;
            }
        }


        /// <summary>
        /// Confirme l'adresse e-mail d'un utilisateur.
        /// </summary>
        /// <param name="userId">L'ID de l'utilisateur.</param>
        /// <param name="token">Le token de confirmation.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task ConfirmEmail(string userId, string token)
        {
            _logger.LogInformation("Confirmation de l'email pour l'utilisateur ID : {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ID utilisateur invalide : {UserId}", userId);
                throw new UserNotFoundException("ID utilisateur invalide.");
            }

            // DÉCODEZ LE TOKEN !
            _logger.LogDebug("Token reçu : {Token}", token); // LOGUEZ LE TOKEN REÇU
            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du décodage pour le user : {UserId}", userId);
                throw new InvalidTokenException("Jeton invalide.", ex); //Relancer l'exception avec l'exception d'origine
            }
            _logger.LogDebug("Token décodé : {DecodedToken}", decodedToken); // LOGUEZ LE TOKEN DÉCODÉ

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken); // Utilisez le token DÉCODÉ
            if (!result.Succeeded)
            {
                _logger.LogWarning("Jeton invalide pour l'utilisateur ID : {UserId}", userId);
                // Log des erreurs spécifiques d'Identity
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Erreur lors de la confirmation de l'e-mail: {ErrorCode} - {ErrorDescription}", error.Code, error.Description);
                }

                //Optionel, considérer les codes d'erreur,
                //Si l'erreur est "InvalidToken", vous pourriez lever une InvalidTokenException
                //Si c'est une autre erreur, vous pourriez lever une autre exception, ou relancer une exception générique.

                throw new InvalidTokenException("Jeton invalide.");
            }
            _logger.LogInformation("Email confirmé avec succès pour utilisateur ID: {UserId}", userId);
        }

        /// <summary>
        /// Re-envoie un demande de confirmation email
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task ResendConfirmationEmailAsync(string email)
        {
            _logger.LogInformation("Demande de renvoi de l'email de confirmation pour : {Email}", email);

            // 1. Trouver l'utilisateur par email.
            var user = await _userManager.FindByEmailAsync(email);

            // 2. Vérifier si l'utilisateur existe.
            if (user == null)
            {
                _logger.LogWarning("Utilisateur introuvable pour l'email : {Email}", email);
                throw new UserNotFoundException($"Utilisateur introuvable pour l'email : '{email}'");
            }

            // 3. Vérifier si l'email est déjà confirmé.
            if (user.EmailConfirmed)
            {
                _logger.LogWarning("L'email est déjà confirmé pour l'utilisateur : {Email}", email);
                throw new EmailAlreadyConfirmedException("L'email est déjà confirmé.");
            }

            // 4. Envoyer l'email de confirmation (réutiliser la méthode existante).
            await SendConfirmationEmail(user); //Appel de la méthode.

            _logger.LogInformation("Email de confirmation renvoyé à : {Email}", email);
        }
        /// <summary>
        /// Envoie un e-mail de réinitialisation de mot de passe à l'utilisateur.
        /// </summary>
        /// <param name="email">L'adresse e-mail de l'utilisateur.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task SendPasswordResetEmailAsync(string email)
        {
            _logger.LogInformation("Demande de réinitialisation de mot de passe pour : {Email}", email);

            var user = await _userManager.FindByEmailAsync(email); //Trouve l'utilisateur par son email

            if (user == null)
            {
                // IMPORTANT : Ne révélez PAS que l'utilisateur n'existe pas.
                // Retournez un succès (ou un message générique) pour éviter l'énumération des utilisateurs.
                _logger.LogWarning("Demande de réinitialisation de mot de passe pour un utilisateur inexistant : {Email}", email);
                return; // On quitte, comme si tout c'était bien passé.
            }

            if (!user.EmailConfirmed) //On s'assure que l'email est confirmé avant
            {
                _logger.LogWarning("Demande de réinitialisation de mot de passe pour un utilisateur avec un e-mail non confirmé : {Email}", email);
                throw new EmailNotConfirmedException("Impossible de réinitialiser le mot de passe. L'e-mail n'est pas confirmé.");
            }

            // Générer le token de réinitialisation de mot de passe
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encoder le token
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Construire l'URL de réinitialisation (adaptez l'URL de base à votre application)
            var baseUrl = _configuration["ResetPasswordUrlBase"] ?? throw new InvalidOperationException("ResetPasswordUrlBase is not configured."); ; // Lisez l'URL de base à partir de la configuration
            string resetPasswordUrl = $"{_configuration["ResetPasswordUrlBase"]}?email={Uri.EscapeDataString(user.Email)}&token={encodedToken}"; //Beaucoup plus simple

            // Envoyer l'e-mail
            try
            {
                await _emailSender.SendEmailAsync(user.Email, "Réinitialisation de votre mot de passe",
                   $"Veuillez réinitialiser votre mot de passe en cliquant sur ce lien: <a href='{resetPasswordUrl}'>lien</a>");
                _logger.LogInformation("E-mail de réinitialisation de mot de passe envoyé à : {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'e-mail de réinitialisation de mot de passe à : {Email}", user.Email);
                throw; // On relance l'exception
            }
        }

        /// <summary>
        /// Réinitialise le mot de passe d'un utilisateur.
        /// </summary>
        /// <param name="email">L'adresse e-mail de l'utilisateur.</param>
        /// <param name="token">Le jeton de réinitialisation de mot de passe.</param>
        /// <param name="newPassword">Le nouveau mot de passe.</param>
        /// <returns>Un IdentityResult indiquant le succès ou l'échec de l'opération.</returns>
        public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            _logger.LogInformation("Tentative de réinitialisation de mot de passe pour l'email : {Email}", email);

            // 1. Trouver l'utilisateur par email
            var user = await _userManager.FindByEmailAsync(email);

            // 2. Vérifier si l'utilisateur existe
            if (user == null)
            {
                _logger.LogWarning("Tentative de réinitialisation de mot de passe avec un email invalide : {Email}", email);
                // Ne révélez *pas* que l'utilisateur n'existe pas.  Retournez un succès apparent (pour la sécurité).
                return IdentityResult.Success; // On simule un succès
            }

            // 3. Décoder le token
            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du décodage du token de réinitialisation pour l'email : {Email}", email);
                throw new InvalidTokenException("Jeton invalide.", ex);
            }

            // 4. Réinitialiser le mot de passe
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Mot de passe réinitialisé avec succès pour l'utilisateur : {UserName}", user.UserName);
            }
            else
            {
                _logger.LogError("Erreur lors de la réinitialisation du mot de passe pour l'email {Email} : {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        /// <summary>
        /// Vérifie si un utilisateur existe déjà avec le nom d'utilisateur ou l'adresse e-mail spécifié.
        /// </summary>
        /// <param name="username">Le nom d'utilisateur.</param>
        /// <param name="email">L'adresse e-mail.</param>
        /// <returns>True si un utilisateur existe, False sinon.</returns>
        public async Task<bool> UserExists(string username, string email)
        {
            // Vérifie l'existence de l'utilisateur par nom d'utilisateur OU email (insensible à la casse)
            return await _context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower() || u.Email.ToLower() == email.ToLower());
        }
    }
}
