namespace LoginFormASPCore6.Services
{
    // Abstraction over email delivery - swap the DI registration for a real
    // provider (see SmtpEmailSender) without touching callers.
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}
