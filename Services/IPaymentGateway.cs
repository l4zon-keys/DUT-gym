using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Http;

namespace LoginFormASPCore6.Services
{
    public class PaymentInitiationRequest
    {
        public Payment Payment { get; set; } = null!;
        public MembershipPlan Plan { get; set; } = null!;
        public User Student { get; set; } = null!;
        public string ReturnUrl { get; set; } = null!;
        public string CancelUrl { get; set; } = null!;
    }

    public class PaymentInitiationResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public string? GatewayReference { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PaymentCallbackResult
    {
        public bool Success { get; set; }
        public int PaymentId { get; set; }
        public string? GatewayReference { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Abstraction over the checkout provider so a real gateway (e.g. PayFast, Stripe)
    // can be swapped in later via DI without touching MembershipController or its views.
    public interface IPaymentGateway
    {
        string ProviderName { get; }

        Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request);

        Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request);
    }
}
