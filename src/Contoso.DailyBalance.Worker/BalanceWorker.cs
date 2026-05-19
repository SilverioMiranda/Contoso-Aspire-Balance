using Contoso.DailyBalance.Services;
using Contoso.Data;
using Contoso.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Contoso.DailyBalance.Worker
{
    public class BalanceWorker(
        ILogger<BalanceWorker> logger,
        IServiceScopeFactory serviceScopeFactory,
        TimeProvider timeProvider,
        IConfiguration configuration) : CronBackgroundWorker(
            logger,
            timeProvider,
            configuration.GetValue<string>("DailyBalance:WorkerCronExpression") ?? "15 0 * * *")
    {
        protected override async Task ExecuteCronAsync(CancellationToken stoppingToken)
        {
            var balanceDate = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-1));

            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ContosoDbContext>();
            if (await HasPersistedBalanceAsync(dbContext, balanceDate, stoppingToken).ConfigureAwait(false))
            {
                logger.LogInformation("O saldo diário de {BalanceDate} já estava persistido.", balanceDate);
                return;
            }

            var dailyBalanceService = scope.ServiceProvider.GetRequiredService<IDailyBalanceService>();
            var balanceValue = await dailyBalanceService.CalculateBalanceValueAsync(balanceDate, stoppingToken).ConfigureAwait(false);
            dbContext.Balances.Add(new Balance
            {
                Date = ToBalanceDate(balanceDate),
                Value = balanceValue,
            });

            try
            {
                await dbContext.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                logger.LogInformation("Saldo diário de {BalanceDate} persistido com sucesso.", balanceDate);
            }
            catch (DbUpdateException)
            {
                if (!await HasPersistedBalanceAsync(dbContext, balanceDate, stoppingToken).ConfigureAwait(false))
                {
                    throw;
                }

                logger.LogInformation("O saldo diário de {BalanceDate} já foi persistido por outra instância.", balanceDate);
            }
        }

        private static async Task<bool> HasPersistedBalanceAsync(ContosoDbContext dbContext, DateOnly balanceDate, CancellationToken cancellationToken) =>
            await dbContext.Balances
                .AsNoTracking()
                .AnyAsync(x => x.Date == ToBalanceDate(balanceDate), cancellationToken)
                .ConfigureAwait(false);

        private static DateTimeOffset ToBalanceDate(DateOnly date) =>
            new(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
    }
}
