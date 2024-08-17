using Contoso.CacheService;
using Contoso.Infrastructure.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Contoso.Transactions.Services
{
    public class TransactionQueueService(IContosoCache contosoCache,IEventPublisher publisher, TimeProvider timeProvider, ILogger<TransactionQueueService> logger, IDistributedCache cache) : ITransactionQueueService
    {
        private static readonly string TopicName = "transactions";

        public async Task<(bool, string?)> EnqueueAsync(TransactionRequest transaction, CancellationToken cancellationToken)
        {
            try
            {
                var date = timeProvider.GetUtcNow();
                
                await contosoCache.IncrementBalanceAsync(DateOnly.FromDateTime(date.Date), Convert.ToInt64(transaction.Amount));
                // Aplica a política de retry ao publicar o evento, passando o logger pelo contexto
                await RetryPolicy.ExecuteAsync(async (context) =>
                {
                    await publisher.PublishAsync(TopicName, transaction).ConfigureAwait(false);
                }, new Context { ["logger"] = logger }).ConfigureAwait(false);
                return (true,null);
            }
            catch (Exception ex)
            {
                // Se todas as tentativas falharem, retorna um status 503
                logger.LogError(ex, "Falha ao processar a transação após múltiplas tentativas.");
                return (false,"Falha ao processar a transação após múltiplas tentativas.");
            }
        }

        private static AsyncRetryPolicy RetryPolicy => Policy
               .Handle<Exception>() // Ajuste o tipo de exceção que deseja capturar
               .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                   (exception, timeSpan, retryCount, context) =>
                   {
                       // Log a tentativa de retry
                       var logger = context["logger"] as ILogger;
                       logger?.LogWarning(exception, "Tentativa {RetryCount} falhou. Tentando novamente em {TimeSpan} segundos.", retryCount, timeSpan.TotalSeconds);
                   });
    }
}
