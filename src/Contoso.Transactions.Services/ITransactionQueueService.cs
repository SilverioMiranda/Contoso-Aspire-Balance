namespace Contoso.Transactions.Services
{
    public interface ITransactionQueueService
    {
        Task<(bool, string?)> EnqueueAsync(TransactionRequest transaction, CancellationToken cancellationToken);
    }
}
