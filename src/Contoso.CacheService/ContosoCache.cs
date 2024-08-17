using StackExchange.Redis;

namespace Contoso.CacheService
{
    public class ContosoCache(IConnectionMultiplexer connectionMultiplexer) : IContosoCache
    {
        private string GetKey(DateOnly date) => $"balance-{date:yyyy-MM-dd}";
        public async Task<long?> GetBalanceAsync(DateOnly date)
        {
            var db = connectionMultiplexer.GetDatabase();
            string key = GetKey(date);

            // Verifica se a chave existe
            if (!await db.KeyExistsAsync(key))
            {
                return null; // Retorna 0 ou outro valor padrão caso a chave não exista
            }

            // Obtém o valor da chave e converte para long
            var value = await db.StringGetAsync(key);

            // Verifica se o valor é nulo e converte para long
            if (value.HasValue)
            {
                return (long)value;
            }

            return 0; // Retorna 0 caso o valor seja nulo
        }
        public async Task SetBalanceAsync(DateOnly date,long balance)
        {
            var db = connectionMultiplexer.GetDatabase();
            string key = GetKey(date);

            // Define o valor da chave no Redis
            await db.StringSetAsync(key, balance);
        }
        public async Task<long> IncrementBalanceAsync(DateOnly date, long incrementValue)
        {
            var db = connectionMultiplexer.GetDatabase();
            // Chave para o valor no Redis
            string key = GetKey(date);

            // Incrementar o valor em 10
            long incrementedValue = await db.StringIncrementAsync(key, incrementValue);
            Console.WriteLine($"Valor após incremento de {incrementValue}: {incrementedValue}");
            return incrementedValue;
        }
        public async Task<long> DecrementBalanceAsync(DateOnly date, long decrementValue)
        {
            var db = connectionMultiplexer.GetDatabase();

            // Chave para o valor no Redis
            string key = GetKey(date);
            
            // Decrementar o valor em 10
            long decrementedValue = await db.StringDecrementAsync(key, decrementValue);
            Console.WriteLine($"Valor após decremento de {decrementValue}: {decrementedValue}");
            return decrementedValue;
        }
    }
}
