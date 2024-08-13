using Contoso.Data;
using Contoso.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Transactions.Services
{
    public class TransactionService(ContosoDbContext dbContext) : ITransactionService
    {
        public async Task<int> AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            await dbContext.AddAsync(transaction, cancellationToken);
            return await dbContext.SaveChangesAsync();
        }
        public async Task<ItemsResponse<Transaction>> ListAsync(DateTime date, int page = 0, int? limit = 30, CancellationToken cancellationToken = default)
        {
            if (page < 0)
            {
                page = 0;
            }
            if (limit is null || limit > 30 || limit < 1)
            {
                limit = 30;
            }

            // Obtém o total de registros para calcular o TotalCount e TotalPages
            var totalCount = await dbContext.Transactions
                .CountAsync(x => x.CreatedAt.Date == date.Date, cancellationToken)
                .ConfigureAwait(false);

            var lancamentos = await dbContext.Transactions
                .Where(x => x.CreatedAt.Date == date.Date)
                .Skip(page * limit.GetValueOrDefault(30))
                .Take(limit.GetValueOrDefault(30))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var totalPages = (int)Math.Ceiling((double)totalCount / limit.GetValueOrDefault(30));

            return new ItemsResponse<Transaction>
            {
                Items = lancamentos,
                TotalCount = totalCount,
                TotalPages = totalPages,
            };
        }

    }
}
