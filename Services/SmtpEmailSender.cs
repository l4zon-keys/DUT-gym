using System.Net;
using System.Net.Mail;

namespace LoginFormASPCore6.Services
{
    // Real email delivery via SMTP (works with Gmail + an App Password, Outlook,
    // or any standard SMTP provider). Activated by setting Email:Provider to "Smtp"
    // and filling in Email:Smtp:* in appsettings.json (or user-secrets/App Service
    // config - never commit real credentials to source control).
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<SmtpEmailSender> logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = configuration["Email:Smtp:Host"];
            var port = configuration.GetValue<int>("Email:Smtp:Port", 587);
            var username = configuration["Email:Smtp:Username"];
            var password = configuration["Email:Smtp:Password"];
            var fromAddress = configuration["Email:Smtp:FromAddress"] ?? username;
            var enableSsl = configuration.GetValue<bool>("Email:Smtp:EnableSsl", true);

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Email:Provider is Smtp but Email:Smtp:Host/Username/Password are not configured - skipping send to {To}.", toEmail);
                return;
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage(fromAddress!, toEmail, subject, body);
            await client.SendMailAsync(message);
        }
    }
}
