namespace Contoso.DailyBalance.Services
{
    public interface IDailyBalanceService
    {
        Task<GetBalanceResponse> GetBalanceAsync(DateTime date, CancellationToken cancellationToken);
        Task<decimal> CalculateBalanceValueAsync(DateOnly date, CancellationToken cancellationToken);
    }
}
