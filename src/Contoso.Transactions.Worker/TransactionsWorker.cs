using Confluent.Kafka;
using Contoso.Infrastructure.Messaging;
using Contoso.Transactions.Services;
using Newtonsoft.Json;

namespace Contoso.Transactions.Worker
{
    /// <summary>
    /// Esse BackgroundService é o responsável por realizar o calculo do saldo diário e persistir no cache e banco de dados.
    /// </summary>
    public class TransactionsWorker(ILogger<TransactionsWorker> logger, IServiceProvider serviceProvider,IConsumer<string,string> consumer, TimeProvider timeProvider) : BackgroundService
    {
        private readonly string _topicName = "transactions";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = serviceProvider.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                try
                {
                    var transactionDbWriter = scope.ServiceProvider.GetRequiredService<ITransactionService>();
                    consumer.Subscribe(_topicName);
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(5));
                        if (result != null)
                        {
                            var transaction = JsonConvert.DeserializeObject<TransactionRequest>(result.Message.Value, JsonSerializationSettings.Settings);

                            if (transaction == null)
                            {
                                logger.LogError("Transaction payload is null");
                                continue;
                            }
                            var r = await transactionDbWriter.AddAsync(new Data.Entities.Transaction
                            {
                                CreatedAt = timeProvider.GetUtcNow(),
                                Description = transaction.Description,
                                Value = transaction.Amount,
                            },stoppingToken).ConfigureAwait(false);

                            if (r > 0)
                            {
                                consumer.Commit(result);
                            } else
                            {
                                logger.LogWarning("Transaction not saved");
                            }
                        }
                    }
                }
                catch (ConsumeException ex) // when (ex.Error.IsFatal)
                {
                    Console.WriteLine($"Erro fatal ao tentar subscrever ao tópico: {ex.Error.Reason}");

                    if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                    {
                        Console.WriteLine($"Tópico {_topicName} não existe. Aguardando...");

                        // Aqui poderiamos ter a lógica de criar o tópico
                    }
                    else
                    {
                        logger.LogError(ex, "Erro fatal ao tentar subscrever ao tópico: {reason}", ex.Error.Reason);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Erro ao processar a transação");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                    await ExecuteAsync(stoppingToken).ConfigureAwait(false);

                }
            }
        }
    }
}