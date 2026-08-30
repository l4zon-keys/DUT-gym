namespace LoginFormASPCore6.Services
{
    // Abstraction over email delivery, same pattern as IPaymentGateway - swap the DI
    // registration for a real provider without touching callers.
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}
