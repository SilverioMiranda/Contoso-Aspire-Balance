using StackExchange.Redis;
using System.Globalization;

namespace Contoso.CacheService
{
    public class ContosoCache(IConnectionMultiplexer connectionMultiplexer) : IContosoCache
    {
        private string GetKey(DateOnly date) => $"balance-{date:yyyy-MM-dd}";

        public async Task<decimal?> GetBalanceAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            var db = connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(GetKey(date)).ConfigureAwait(false);
            if (!value.HasValue)
            {
                return null;
            }

            return decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedBalance)
                ? parsedBalance
                : null;
        }

        public async Task SetBalanceAsync(DateOnly date, decimal balance, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var db = connectionMultiplexer.GetDatabase();
            var serializedBalance = balance.ToString(CultureInfo.InvariantCulture);
            await db.StringSetAsync(GetKey(date), serializedBalance, ttl).ConfigureAwait(false);
        }
    }
}
