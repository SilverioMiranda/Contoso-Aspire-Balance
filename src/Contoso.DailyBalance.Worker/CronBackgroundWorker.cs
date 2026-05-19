using Cronos;

namespace Contoso.DailyBalance.Worker
{
    public abstract class CronBackgroundWorker(
        ILogger logger,
        TimeProvider timeProvider,
        string cronExpression) : BackgroundService
    {
        protected ILogger Logger { get; } = logger;
        protected TimeProvider TimeProvider { get; } = timeProvider;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var parsedExpression = CronExpression.Parse(cronExpression);

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = TimeProvider.GetUtcNow().UtcDateTime;
                var nextOccurrence = parsedExpression.GetNextOccurrence(utcNow, TimeZoneInfo.Utc);
                if (!nextOccurrence.HasValue)
                {
                    Logger.LogWarning("Nenhuma próxima execução foi encontrada para a expressão cron {CronExpression}.", cronExpression);
                    return;
                }

                var delay = nextOccurrence.Value - utcNow;
                if (delay > TimeSpan.Zero)
                {
                    Logger.LogInformation("Próxima execução agendada para {NextOccurrenceUtc}.", nextOccurrence.Value);
                    await Task.Delay(delay, TimeProvider, stoppingToken).ConfigureAwait(false);
                }

                await ExecuteCronAsync(stoppingToken).ConfigureAwait(false);
            }
        }

        protected abstract Task ExecuteCronAsync(CancellationToken stoppingToken);
    }
}
