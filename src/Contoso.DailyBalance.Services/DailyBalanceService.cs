using Contoso.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Contoso.DailyBalance.Services
{
    public class DailyBalanceService(IDistributedCache cache, ContosoDbContext dbContext, ILogger<DailyBalanceService> logger, TimeProvider timeProvider) : IDailyBalanceService
    {
        public static int CacheExpirationInSeconds = 5;
        public async Task<GetBalanceResponse> GetBalanceAsync(DateTime date,CancellationToken cancellationToken)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");

            var cachekey = $"balance-{formattedDate}";
            try
            {
                var cachedBalance = await cache.GetStringAsync(cachekey, cancellationToken).ConfigureAwait(false);

                if (cachedBalance != null)
                {
                    var obj = JsonSerializer.Deserialize<GetBalanceResponse>(cachedBalance);

                    if (obj != null)
                    {
                        if (obj.BalanceDate.AddSeconds(CacheExpirationInSeconds) >= timeProvider.GetUtcNow())
                        {
                            obj.IsFromCache = true;
                            return obj;
                        }
                        else
                        {
                            await cache.RemoveAsync(cachekey, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Erro ao buscar saldo no cache");
            }
            var dataCache = timeProvider.GetUtcNow();
            var soma = await dbContext.Transactions.Where(x=> x.CreatedAt.Date == date.Date).SumAsync(t => t.Value, cancellationToken: cancellationToken).ConfigureAwait(false);
            var response = new GetBalanceResponse
            {
                Balance = soma,
                BalanceDate = dataCache,
                IsFromCache = false
            };
            try
            {
                await cache.SetStringAsync(cachekey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheExpirationInSeconds)
                }, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Erro ao buscar saldo no cache");
            }

            return response;
        }
    }
}