using Microsoft.AspNetCore.Http;

namespace LoginFormASPCore6.Services
{
    // Default gateway while no real provider is wired up. Simulates a hosted
    // checkout redirect by bouncing straight back to the return URL, so the full
    // apply -> pay -> active flow is testable end-to-end without merchant credentials.
    public class MockPaymentGateway : IPaymentGateway
    {
        public string ProviderName => "Mock";

        public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request)
        {
            var reference = Guid.NewGuid().ToString("N");
            var separator = request.ReturnUrl.Contains('?') ? "&" : "?";
            var redirectUrl = $"{request.ReturnUrl}{separator}paymentId={request.Payment.Id}&reference={reference}";

            return Task.FromResult(new PaymentInitiationResult
            {
                Success = true,
                RedirectUrl = redirectUrl,
                GatewayReference = reference
            });
        }

        public Task<PaymentCallbackResult> HandleCallbackAsync(HttpRequest request)
        {
            if (!int.TryParse(request.Query["paymentId"], out var paymentId))
            {
                return Task.FromResult(new PaymentCallbackResult
                {
                    Success = false,
                    ErrorMessage = "Missing or invalid paymentId."
                });
            }

            return Task.FromResult(new PaymentCallbackResult
            {
                Success = true,
                PaymentId = paymentId,
                GatewayReference = request.Query["reference"].ToString()
            });
        }
    }
}
