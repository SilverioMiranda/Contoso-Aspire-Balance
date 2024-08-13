using Contoso.Data.Entities;

namespace Contoso.Transactions.Services
{
    public interface ITransactionService
    {
        Task<int> AddAsync(Transaction transaction, CancellationToken cancellationToken);
        Task<ItemsResponse<Transaction>> ListAsync(DateTime date, int page = 0, int? limit = 30, CancellationToken cancellationToken = default);
    }
}
