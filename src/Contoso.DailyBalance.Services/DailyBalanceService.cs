using Contoso.CacheService;
using Contoso.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Contoso.DailyBalance.Services
{
    public class DailyBalanceService(IContosoCache contosoCache, ContosoDbContext dbContext, ILogger<DailyBalanceService> logger, TimeProvider timeProvider) : IDailyBalanceService
    {
        public static int CacheExpirationInSeconds = 5;
        public async Task<GetBalanceResponse> GetBalanceAsync(DateTime date,CancellationToken cancellationToken)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");

            var cache = await contosoCache.GetBalanceAsync(DateOnly.FromDateTime(date));

            if(cache.HasValue)
            {
                var r = new GetBalanceResponse
                {
                    Balance = cache.Value,
                    BalanceDate = timeProvider.GetUtcNow(),
                    IsFromCache = true
                };
                return r;
            }
            var dataCache = timeProvider.GetUtcNow();
            var soma = await dbContext.Transactions.Where(x=> x.CreatedAt.Date == date.Date).SumAsync(t => t.Value, cancellationToken: cancellationToken).ConfigureAwait(false);
            await contosoCache.IncrementBalanceAsync(DateOnly.FromDateTime(date), Convert.ToInt64(soma));

            var response = new GetBalanceResponse
            {
                Balance = soma,
                BalanceDate = dataCache,
                IsFromCache = false
            };

            return response;
        }
    }
}