namespace Contoso.CacheService
{
    public interface IContosoCache
    {
        /// <summary>
        /// Obtém o saldo armazenado no Redis.
        /// </summary>
        /// <returns>O saldo atual como <see cref="long?"/> ou null se a chave não existir.</returns>
        Task<long?> GetBalanceAsync(DateOnly date);

        /// <summary>
        /// Incrementa o saldo no Redis por um valor específico.
        /// </summary>
        /// <param name="incrementValue">O valor a ser incrementado.</param>
        /// <returns>O novo saldo após o incremento como <see cref="long"/>.</returns>
        Task<long> IncrementBalanceAsync(DateOnly date, long incrementValue);

        /// <summary>
        /// Decrementa o saldo no Redis por um valor específico.
        /// </summary>
        /// <param name="decrementValue">O valor a ser decrementado.</param>
        /// <returns>O novo saldo após o decremento como <see cref="long"/>.</returns>
        Task<long> DecrementBalanceAsync(DateOnly date, long decrementValue);
    }
}
