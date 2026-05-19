namespace Contoso.Transactions.Services
{
    public interface ITransactionQueueService
    {
        Task<(bool, string?)> EnqueueAsync(QueuedTransaction transaction, CancellationToken cancellationToken);
    }
}
