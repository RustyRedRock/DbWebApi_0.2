using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options; // Importez ce namespace
using Microsoft.Extensions.Logging; // Importez ce namespace

namespace DB_Server_WebApi.Services
{
    // Services/IEmailSender.cs (l'interface reste inchangée)
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    // 1. Créez une classe pour représenter vos paramètres SMTP (fortement typée)
    public class SmtpSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; } // Ajoutez SenderName
        public string SenderEmail { get; set; } // Ajoutez SenderEmail
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; } // Ajoutez la configuration pour SSL
    }

    // Services/EmailSender.cs (implémentation améliorée)
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpEmailSender> _logger; // Utilisez ILogger

        // Utilisez IOptions<SmtpSettings> pour l'injection de dépendances
        public SmtpEmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpEmailSender> logger)
        {
            _smtpSettings = smtpSettings.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName), //Utilise les parametre de configuration
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true, // Indique que le corps est au format HTML
                };
                mailMessage.To.Add(email); //Ajoute le destinataire

                using (var client = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port))
                {
                    client.EnableSsl = _smtpSettings.UseSsl; // Utilise la configuration SSL
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    await client.SendMailAsync(mailMessage);
                }

                _logger.LogInformation("Email sent to {Email} with subject {Subject}", email, subject);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", email, subject);
                // Rethrow pour que l'appelant soit au courant de l'échec, ou gérer l'erreur différemment.
                throw;
            }
        }
    }
}
