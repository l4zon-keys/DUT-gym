namespace LoginFormASPCore6.Services
{
    // Default sender when no real SMTP credentials are configured - logs instead of
    // sending, so the app runs correctly without an email account on hand. Switch
    // Email:Provider to "Smtp" in appsettings.json once real credentials exist.
    public class LogEmailSender : IEmailSender
    {
        private readonly ILogger<LogEmailSender> logger;

        public LogEmailSender(ILogger<LogEmailSender> logger)
        {
            this.logger = logger;
        }

        public Task SendAsync(string toEmail, string subject, string body)
        {
            logger.LogInformation("[Email - not actually sent, no SMTP configured] To: {To} | Subject: {Subject} | Body: {Body}",
                toEmail, subject, body);
            return Task.CompletedTask;
        }
    }
}
