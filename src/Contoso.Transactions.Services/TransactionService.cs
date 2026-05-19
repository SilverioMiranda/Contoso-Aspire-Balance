using Contoso.Data;
using Contoso.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Contoso.Transactions.Services
{
    public class TransactionService(ContosoDbContext dbContext) : ITransactionService
    {
        public async Task StoreAsync(QueuedTransaction transaction, CancellationToken cancellationToken)
        {
            dbContext.Transactions.Add(new Transaction
            {
                RequestId = transaction.RequestId,
                CreatedAt = transaction.RequestedAtUtc,
                Description = NormalizeDescription(transaction.Description),
                Value = transaction.Amount,
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
                var alreadyPersisted = await dbContext.Transactions
                    .AsNoTracking()
                    .AnyAsync(x => x.RequestId == transaction.RequestId, cancellationToken)
                    .ConfigureAwait(false);

                if (!alreadyPersisted)
                {
                    throw;
                }
            }
        }

        public async Task<ItemsResponse<Transaction>> ListAsync(DateTime date, int page = 0, int? limit = 30, CancellationToken cancellationToken = default)
        {
            var pageNumber = page < 0 ? 0 : page;
            var pageSize = limit is >= 1 and <= 30 ? limit.Value : 30;
            var (startUtc, endExclusiveUtc) = CreateUtcDayRange(DateOnly.FromDateTime(date.Date));

            var query = dbContext.Transactions
                .AsNoTracking()
                .Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endExclusiveUtc);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var transactions = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip(pageNumber * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

            return new ItemsResponse<Transaction>
            {
                Items = transactions,
                TotalCount = totalCount,
                TotalPages = totalPages,
            };
        }

        private static string? NormalizeDescription(string? description) =>
            string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        private static (DateTimeOffset StartUtc, DateTimeOffset EndExclusiveUtc) CreateUtcDayRange(DateOnly date)
        {
            var startUtc = new DateTimeOffset(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
            return (startUtc, startUtc.AddDays(1));
        }
    }
}
