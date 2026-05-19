using Contoso.CacheService;
using Contoso.Data;
using Microsoft.EntityFrameworkCore;

namespace Contoso.DailyBalance.Services
{
    public class DailyBalanceService(
        IContosoCache contosoCache,
        ContosoDbContext dbContext) : IDailyBalanceService
    {
        public const int CacheExpirationInSeconds = 5;

        public async Task<GetBalanceResponse> GetBalanceAsync(DateTime date, CancellationToken cancellationToken)
        {
            var targetDate = DateOnly.FromDateTime(date.Date);
            var cachedBalance = await contosoCache.GetBalanceAsync(targetDate, cancellationToken).ConfigureAwait(false);
            if (cachedBalance.HasValue)
            {
                return CreateResponse(targetDate, cachedBalance.Value, isFromCache: true);
            }

            var calculatedBalance = await CalculateBalanceValueAsync(targetDate, cancellationToken).ConfigureAwait(false);
            await contosoCache
                .SetBalanceAsync(targetDate, calculatedBalance, TimeSpan.FromSeconds(CacheExpirationInSeconds), cancellationToken)
                .ConfigureAwait(false);

            return CreateResponse(targetDate, calculatedBalance, isFromCache: false);
        }

        public async Task<decimal> CalculateBalanceValueAsync(DateOnly date, CancellationToken cancellationToken)
        {
            var exactBalance = await dbContext.Balances
                .AsNoTracking()
                .Where(x => x.Date == ToBalanceDate(date))
                .Select(x => (decimal?)x.Value)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (exactBalance.HasValue)
            {
                return exactBalance.Value;
            }

            var previousBalance = await dbContext.Balances
                .AsNoTracking()
                .Where(x => x.Date < ToBalanceDate(date))
                .OrderByDescending(x => x.Date)
                .Select(x => new { x.Date, x.Value })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var endExclusiveUtc = ToBalanceDate(date.AddDays(1));
            var query = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.CreatedAt < endExclusiveUtc);

            var baseBalance = 0m;
            if (previousBalance is not null)
            {
                baseBalance = previousBalance.Value;
                var startInclusiveUtc = ToBalanceDate(DateOnly.FromDateTime(previousBalance.Date.UtcDateTime.Date).AddDays(1));
                query = query.Where(x => x.CreatedAt >= startInclusiveUtc);
            }

            var delta = await query
                .SumAsync(x => (decimal?)x.Value, cancellationToken)
                .ConfigureAwait(false) ?? 0m;

            return baseBalance + delta;
        }

        private GetBalanceResponse CreateResponse(DateOnly date, decimal balance, bool isFromCache) =>
            new()
            {
                Balance = balance,
                BalanceDate = ToBalanceDate(date),
                IsFromCache = isFromCache,
            };

        private DateTimeOffset ToBalanceDate(DateOnly date) =>
            new(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
    }
}
