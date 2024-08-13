namespace Contoso.ServiceDefaults
{
    public interface IApiKeyValidator
    {
        Task<bool> ValidateApiKeyAsync(string apiKey);
    }
}
