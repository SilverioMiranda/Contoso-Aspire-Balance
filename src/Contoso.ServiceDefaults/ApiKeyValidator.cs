using Microsoft.Extensions.Configuration;

namespace Contoso.ServiceDefaults
{
    public class ApiKeyValidator(IConfiguration configuration) : IApiKeyValidator
    {
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            var validApiKey = configuration["ApiKeyAuthentication:ApiKey"] ?? "contoso";
            return await Task.FromResult(string.Equals(apiKey, validApiKey, StringComparison.Ordinal)).ConfigureAwait(false);
        }
    }
}
