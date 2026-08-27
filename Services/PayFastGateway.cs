using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace LoginFormASPCore6.Services
{
    // Real PayFast integration, configured against a real (free) PayFast sandbox merchant
    // account - MerchantId/MerchantKey in appsettings.json. PayFast's own commonly-cited
    // shared demo credentials (merchant_id 10000100) did not pass signature verification
    // when tested live, so a real sandbox account was needed instead.
    //
    // Note: this only trusts the browser's return_url redirect as confirmation of payment,
    // which is what MockPaymentGateway did too. A production integration must additionally
    // verify PayFast's server-to-server ITN (Instant Transaction Notification) callback
    // before trusting a payment - that requires a publicly reachable notify_url, which
    // isn't practical to stand up for local/school-project development.
    public class PayFastGateway : IPaymentGateway
    {
        private readonly IConfiguration configuration;

        public PayFastGateway(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string ProviderName => "PayFast";

        public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request)
        {
            var merchantId = configuration["PayFast:MerchantId"] ?? "10000100";
            var merchantKey = configuration["PayFast:MerchantKey"] ?? "46f0cd694581a";
            var processUrl = configuration["PayFast:ProcessUrl"] ?? "https://sandbox.payfast.co.za/eng/process";
            var passPhrase = configuration["PayFast:PassPhrase"];

            var reference = Guid.NewGuid().ToString("N");
            var separator = request.ReturnUrl.Contains('?') ? "&" : "?";
            var trackedReturnUrl = $"{request.ReturnUrl}{separator}paymentId={request.Payment.Id}&reference={reference}";

            var nameParts = (request.Student.EmpName ?? "Student").Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Order matters: this exact order is used both to build the query string and
            // to compute the signature, so the two stay self-consistent.
            var fields = new List<KeyValuePair<string, string>>
            {
                new("merchant_id", merchantId),
                new("merchant_key", merchantKey),
                new("return_url", trackedReturnUrl),
                new("cancel_url", request.CancelUrl),
                new("name_first", firstName),
                new("name_last", lastName),
                new("email_address", request.Student.Email),
                new("m_payment_id", request.Payment.Id.ToString()),
                new("amount", request.Plan.Price.ToString("F2", CultureInfo.InvariantCulture)),
                new("item_name", request.Plan.Name),
            };

            var signature = BuildSignature(fields, passPhrase);
            var queryString = string.Join("&", fields.Select(f => $"{f.Key}={PhpUrlEncode(f.Value)}"));
            var redirectUrl = $"{processUrl}?{queryString}&signature={signature}";

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

        private static string BuildSignature(List<KeyValuePair<string, string>> fields, string? passPhrase)
        {
            var signatureString = string.Join("&",
                fields.Where(f => !string.IsNullOrEmpty(f.Value))
                      .Select(f => $"{f.Key}={PhpUrlEncode(f.Value)}"));

            if (!string.IsNullOrEmpty(passPhrase))
            {
                signatureString += $"&passphrase={PhpUrlEncode(passPhrase)}";
            }

            var hash = MD5.HashData(Encoding.UTF8.GetBytes(signatureString));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        // PayFast (like PHP's urlencode) wants spaces as '+' and %XX escapes in uppercase hex -
        // .NET's Uri.EscapeDataString does neither, so it has to be done by hand.
        private static string PhpUrlEncode(string value)
        {
            var sb = new StringBuilder();
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                var c = (char)b;
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c is '-' or '_' or '.')
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('+');
                }
                else
                {
                    sb.Append('%').Append(b.ToString("X2"));
                }
            }
            return sb.ToString();
        }
    }
}
