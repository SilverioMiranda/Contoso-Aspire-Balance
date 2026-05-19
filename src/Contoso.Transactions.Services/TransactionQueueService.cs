using Contoso.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Contoso.Transactions.Services
{
    public class TransactionQueueService(IEventPublisher publisher, ILogger<TransactionQueueService> logger) : ITransactionQueueService
    {
        private const string TopicName = "transactions";

        public async Task<(bool, string?)> EnqueueAsync(QueuedTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RetryPolicy.ExecuteAsync(
                    async context => await publisher.PublishAsync(TopicName, transaction).ConfigureAwait(false),
                    new Context { ["logger"] = logger }).ConfigureAwait(false);

                return (true, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (false, "Requisição cancelada.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enfileirar a transação {RequestId}.", transaction.RequestId);
                return (false, "Falha ao processar a transação após múltiplas tentativas.");
            }
        }

        private static AsyncRetryPolicy RetryPolicy =>
            Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        var retryLogger = context["logger"] as ILogger;
                        retryLogger?.LogWarning(
                            exception,
                            "Tentativa {RetryCount} falhou. Nova tentativa em {DelaySeconds} segundos.",
                            retryCount,
                            timeSpan.TotalSeconds);
                    });
    }
}
