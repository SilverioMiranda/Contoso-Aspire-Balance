using Confluent.Kafka;
using Contoso.Infrastructure.Messaging;
using Contoso.Transactions.Services;
using Newtonsoft.Json;

namespace Contoso.Transactions.Worker
{
    public class TransactionsWorker(
        ILogger<TransactionsWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConsumer<string, string> consumer) : BackgroundService
    {
        private const string TopicName = "transactions";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            consumer.Subscribe(TopicName);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(5));
                        if (result is null)
                        {
                            continue;
                        }

                        await ProcessMessageAsync(result, stoppingToken).ConfigureAwait(false);
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "Erro ao consumir mensagens do tópico {TopicName}.", TopicName);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Erro inesperado ao processar mensagens do tópico {TopicName}.", TopicName);
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken stoppingToken)
        {
            var transaction = JsonConvert.DeserializeObject<QueuedTransaction>(result.Message.Value, JsonSerializationSettings.Settings);
            if (transaction is null)
            {
                logger.LogWarning("Mensagem inválida recebida no tópico {TopicName}. Offset {Offset}.", TopicName, result.Offset);
                consumer.Commit(result);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
            await transactionService.StoreAsync(transaction, stoppingToken).ConfigureAwait(false);
            consumer.Commit(result);
        }
    }
}
