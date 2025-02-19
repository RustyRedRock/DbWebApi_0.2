using DB_Server_WebApi.DTOs;
using DB_Server_WebApi.Logs;
using DB_Server_WebApi.Models.DTOs;
using DB_Server_WebApi.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace DB_Server_WebApi.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion de l'authentification des utilisateurs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Route de base pour toutes les actions : api/Auth
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;  // Interface pour le service d'authentification
        private readonly ILogger<AuthController> _logger; // Utilisation de l'interface ILogger<T> de .NET

        /// <summary>
        /// Constructeur du contrôleur d'authentification.
        /// </summary>
        /// <param name="authService">Service d'authentification (injecté).</param>
        /// <param name="logger">Service de logging (injecté).</param>
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur.
        /// </summary>
        /// <param name="registerDto">Objet DTO contenant les informations d'enregistrement.</param>
        /// <returns>Un objet UserDto contenant le nom d'utilisateur et l'email en cas de succès, ou une erreur BadRequest en cas d'échec.</returns>
        [HttpPost("register")] // Route: POST api/Auth/register
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))] // Documentation du code de retour 200
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Documentation du code de retour 400
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            _logger.LogInformation("Tentative d'enregistrement pour l'utilisateur : {UserName}, Email: {Email}", registerDto.UserName, registerDto.Email); // Log email aussi

            if (!ModelState.IsValid) // Validation basée sur les Data Annotations du RegisterDto
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _authService.Register(registerDto); // Enregistre l'utilisateur via le service

                if (!result.Succeeded) // Vérifiez le résultat d'Identity
                {
                    //Si la création a échoué, on log les erreurs et on retourne un BadRequest
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Erreur Identity lors de la création de l'utilisateur {UserName}: {ErrorCode} - {ErrorDescription}", registerDto.UserName, error.Code, error.Description);
                        ModelState.AddModelError(error.Code, error.Description); //Ajoute les erreurs au modelstate
                    }
                    return BadRequest(ModelState);
                }

                //Récupération de l'utilisateur *après* un enregistrement réussi.
                var user = await _authService.GetUserByLoginAsync(registerDto.UserName);


                if (user is null)
                {
                    //Cela ne devrait *jamais* arriver si result.Succeeded est vrai.  C'est une sécurité.
                    _logger.LogError("L'utilisateur {UserName} n'a pas pu être récupéré après un enregistrement réussi.", registerDto.UserName);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." }); // 500, pas 400
                }

                _logger.LogInformation("Utilisateur enregistré avec succès : {UserName}", user.UserName);

                // IMPORTANT:  PAS DE TOKEN JWT.  Retournez juste les infos de l'utilisateur.
                return Ok(new UserDto { UserName = user.UserName, Email = user.Email });
            }
            catch (DuplicateUserException ex)
            {
                _logger.LogWarning(ex, "Tentative d'enregistrement avec un nom d'utilisateur/email déjà existant : {UserName}", registerDto.UserName);
                return BadRequest(new { message = ex.Message }); // Message d'erreur convivial
            }
            catch (Exception ex) // Attrape toute autre exception (par exemple, problèmes de base de données)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'enregistrement de l'utilisateur : {UserName}", registerDto.UserName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." });
            }
        }


        /// <summary>
        /// Connecte un utilisateur existant.
        /// </summary>
        /// <param name="loginDto">Objet DTO contenant les informations de connexion.</param>
        /// <returns>Un objet UserDto contenant le nom d'utilisateur, l'email et le token JWT en cas de succès, ou une erreur Unauthorized en cas d'échec.</returns>
        [HttpPost("login")] // Route: POST api/Auth/login
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            _logger.LogInformation("Tentative de connexion pour l'utilisateur/email : {Login}", loginDto.Login);

            try
            {
                var token = await _authService.Login(loginDto); // Tente de connecter l'utilisateur
                var user = await _authService.GetUserByLoginAsync(loginDto.Login);

                if (user is null)
                {
                    _logger.LogError("Utilisateur {Login} non trouvé, après la génération du token.", loginDto.Login);
                    return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect." }); //Bien que le token est été générer.
                }

                _logger.LogInformation("Connexion réussie pour l'utilisateur : {UserName}", user.UserName);
                return Ok(new UserDto { UserName = user.UserName, Email = user.Email});
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning(ex, "Utilisateur non trouvé : {Login}", loginDto.Login);
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect." });
            }
            catch (EmailNotConfirmedException ex)
            {
                _logger.LogWarning(ex, "Email non confirmé pour l'utilisateur : {Login}", loginDto.Login);
                return BadRequest(new { message = "Veuillez confirmer votre adresse e-mail." });
            }
            catch (InvalidCredentialsException ex)
            {
                _logger.LogWarning(ex, "Identifiants invalides pour l'utilisateur : {Login}", loginDto.Login);
                return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect." });
            }
            catch (LockedOutException ex)
            {
                _logger.LogWarning(ex, "Compte verrouillé pour l'utilisateur : {Login}", loginDto.Login);
                return Unauthorized(new { message = "Compte verrouillé. Veuillez réessayer plus tard." });
            }
            catch (TwoFactorRequiredException ex)
            {
                _logger.LogWarning(ex, "L'authentification à deux facteurs requise pour l'utilisateur : {Login}", loginDto.Login);
                return Unauthorized(new { message = "L'authentification à deux facteurs est requise." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la connexion de l'utilisateur : {Login}", loginDto.Login);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." });
            }
        }

        /// <summary>
        /// Confirme l'adresse e-mail d'un utilisateur.
        /// </summary>
        /// <param name="userId">ID de l'utilisateur.</param>
        /// <param name="token">Token de confirmation.</param>
        /// <returns>Un message de succès en cas de confirmation réussie, ou une erreur BadRequest en cas d'échec.</returns>
        [HttpGet("confirmemail")] // Route: GET api/Auth/confirmemail?userId=...&token=...
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            _logger.LogInformation("Tentative de confirmation d'email pour l'utilisateur ID : {UserId}", userId);

            try
            {
                await _authService.ConfirmEmail(userId, token);
                _logger.LogInformation("Email confirmé avec succès pour l'utilisateur ID : {UserId}", userId);
                return Ok("Email confirmed successfully!"); // Message de succès simple
            }
            catch (ArgumentException ex) // Vous pourriez avoir d'autres exceptions spécifiques ici
            {
                _logger.LogWarning(ex, "Erreur lors de la confirmation d'email pour l'utilisateur ID : {UserId}", userId);
                return BadRequest(new { message = ex.Message }); // Message d'erreur convivial
            }
            catch (Exception ex) //Pour tout autre type d'erreur.
            {
                _logger.LogError(ex, "Erreur inatendue lors de la confirmation d'email pour l'utilisateur ID : {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." }); // Retournez un 500 Internal Server Error
            }
        }

        [HttpPost("resendconfirmationemail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailDto emailDto)
        {
            _logger.LogInformation("Demande de renvoi de l'email de confirmation pour : {Email}", emailDto.Email);

            if (!ModelState.IsValid) // Validation
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _authService.ResendConfirmationEmailAsync(emailDto.Email);
                return Ok(new { message = "Un nouvel email de confirmation a été envoyé." });
            }
            catch (UserNotFoundException)
            {
                return NotFound(new { message = "Aucun utilisateur trouvé avec cette adresse e-mail." }); // 404 Not Found
            }
            catch (EmailAlreadyConfirmedException ex)
            {
                return BadRequest(new { message = ex.Message }); // 400 Bad Request
            }
            catch (Exception ex) // Attrape toute autre exception
            {
                _logger.LogError(ex, "Erreur inattendue lors du renvoi de l'e-mail de confirmation pour : {Email}", emailDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." });
            }
        }
        // Dans AuthController.cs

        [HttpPost("forgotpassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            _logger.LogInformation("Demande de réinitialisation de mot de passe pour l'email : {Email}", forgotPasswordDto.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _authService.SendPasswordResetEmailAsync(forgotPasswordDto.Email);
                return Ok(new { message = "Un e-mail de réinitialisation de mot de passe a été envoyé." });
            }
            catch (EmailNotConfirmedException ex)
            {
                _logger.LogWarning(ex, "Impossible de réinitialiser le mot de passe. Email non confirmé pour : {Email}", forgotPasswordDto.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la demande de réinitialisation de mot de passe pour l'email : {Email}", forgotPasswordDto.Email);
                //Pour la sécurité, on ne doit pas dire si l'utilisateur existe ou pas. On retourne un message de succès dans les deux cas.
                return Ok(new { message = "Un e-mail de réinitialisation de mot de passe a été envoyé." });
            }
        }

        [HttpPost("resetpassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            _logger.LogInformation("Tentative de réinitialisation de mot de passe pour l'email : {Email}", resetPasswordDto.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _authService.ResetPasswordAsync(resetPasswordDto.Email, resetPasswordDto.Token, resetPasswordDto.NewPassword);

                if (result.Succeeded)
                {
                    return Ok(new { message = "Votre mot de passe a été réinitialisé avec succès." });
                }
                else
                {
                    //Si le reset a échoué, on renvoie les erreurs pour aider le client
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return BadRequest(ModelState);
                }
            }
            catch (InvalidTokenException ex)
            {
                _logger.LogWarning(ex, "Jeton invalide lors de la réinitialisation du mot de passe pour l'email : {Email} ", resetPasswordDto.Email);
                return BadRequest(new { message = "Jeton invalide." }); // Message d'erreur convivial
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la réinitialisation de mot de passe pour : {Email}", resetPasswordDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur interne du serveur s'est produite." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(); // Or Redirect to a specific page
        }
    }
}
