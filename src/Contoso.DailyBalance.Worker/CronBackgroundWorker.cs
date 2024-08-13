using Cronos;

namespace Contoso.DailyBalance.Worker
{
    public abstract class CronBackgroundWorker : BackgroundService
    {
        protected ILogger Logger { get; }
        private string _cronExpression { get; }
        protected CronBackgroundWorker(ILogger logger, string cronExpression)
        {
            Logger = logger;
            _cronExpression = cronExpression;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await WaitForNextScheduleAsync(stoppingToken).ConfigureAwait(false);
            await ExecuteCronAsync(stoppingToken).ConfigureAwait(false);
        }
        protected abstract Task ExecuteCronAsync(CancellationToken stoppingToken);

        private async Task WaitForNextScheduleAsync( CancellationToken cancellationToken)
        {
            var parsedExp = CronExpression.Parse(_cronExpression);
            var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
            var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime);

            var delay = occurenceTime.GetValueOrDefault().Subtract(currentUtcTime);
            Logger.LogInformation("The run is delayed for {delay}. Current time: {time}", delay, DateTimeOffset.Now);

            await Task.Delay(Convert.ToInt32(delay.TotalMilliseconds), cancellationToken).ConfigureAwait(false);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}