namespace Contoso.CacheService
{
    public interface IContosoCache
    {
        Task<decimal?> GetBalanceAsync(DateOnly date, CancellationToken cancellationToken = default);
        Task SetBalanceAsync(DateOnly date, decimal balance, TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
