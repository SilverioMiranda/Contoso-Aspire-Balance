using Contoso.Data;
using Contoso.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Contoso.DailyBalance.Worker
{
    /// <summary>
    /// Esse BackgroundService é o respons�vel por realizar o calculo do saldo e persistir banco de dados.
    /// </summary>
    public class BalanceWorker: CronBackgroundWorker
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TimeProvider timeProvider;
        private readonly ILogger<BalanceWorker> logger;
        public BalanceWorker(ILogger<BalanceWorker> logger, IServiceProvider serviceProvider, TimeProvider timeProvider,IConfiguration configuration) : base(logger, configuration.GetValue<string>("BALANCE_WORKER_CRON") ?? "15 0 * * *")
        {
            this.serviceProvider = serviceProvider;
            this.timeProvider = timeProvider;
            this.logger = logger;
        }

        protected override async Task ExecuteCronAsync(CancellationToken stoppingToken)
        {
            var scope = serviceProvider.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();

                var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var ultimoSaldo = await dbContext.Balances
                            .OrderByDescending(x => x.Date)
                            .FirstOrDefaultAsync(cancellationToken: stoppingToken)
                            .ConfigureAwait(false);

                        decimal saldo = 0;
                        var now = timeProvider.GetUtcNow();
                        var maxDatePreviousDay = now.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                        if (ultimoSaldo is null)
                        {
                            // Cache miss: Calcula o saldo total pela primeira vez
                            saldo = await dbContext.Transactions
                                .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            // Verifica se a data da última transação é anterior ao dia anterior
                            if (ultimoSaldo.Date < now.AddDays(-1))
                            {
                                // Usa UTCNow - 15 minutos como data máxima
                                var maxDate = now.AddMinutes(-15);

                                saldo = ultimoSaldo.Value + await dbContext.Transactions
                                    .Where(t => t.CreatedAt > ultimoSaldo.Date && t.CreatedAt <= maxDate)
                                    .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                // Usa o último segundo do dia anterior como data máxima
                                saldo = ultimoSaldo.Value + await dbContext.Transactions
                                    .Where(t => t.CreatedAt > ultimoSaldo.Date && t.CreatedAt <= maxDatePreviousDay)
                                    .SumAsync(t => t.Value, cancellationToken: stoppingToken)
                                    .ConfigureAwait(false);
                            }
                        }


                        await dbContext.Balances.AddAsync(new Balance { Value = saldo, Date = maxDatePreviousDay }, stoppingToken)
                            .ConfigureAwait(false);

                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                        

                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Erro ao gerar o saldo");
                    }

                    // Defina um intervalo adequado para a próxima execução do cron
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
                }
            }
        }

    }
}