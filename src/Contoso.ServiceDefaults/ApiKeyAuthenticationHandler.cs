using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Contoso.ServiceDefaults
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IApiKeyValidator _apiKeyValidator;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyValidator apiKeyValidator)
            : base(options, logger, encoder)
        {
            _apiKeyValidator = apiKeyValidator;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-API-KEY", out var apiKeyHeaderValues))
            {
                return AuthenticateResult.Fail("API Key não fornecida.");
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (providedApiKey == null || ! await _apiKeyValidator.ValidateApiKeyAsync(providedApiKey).ConfigureAwait(false))
            {
                return AuthenticateResult.Fail("API Key inválida.");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, "API User") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
