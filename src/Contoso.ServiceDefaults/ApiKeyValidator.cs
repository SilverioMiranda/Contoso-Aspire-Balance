namespace Contoso.ServiceDefaults
{
    public class ApiKeyValidator : IApiKeyValidator
    {
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            var validApiKey = "contoso";
            return await Task.FromResult(string.Equals(apiKey, validApiKey, StringComparison.Ordinal)).ConfigureAwait(false);
        }
    }
}
