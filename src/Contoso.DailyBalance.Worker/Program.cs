namespace Contoso.DailyBalance.Worker
{
    using Contoso.DailyBalance.Services;
    using Contoso.ServiceDefaults;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();
            builder.AddContosoDbContext();
            builder.Services.AddContosoCacheServices();
            builder.Services.AddDailyBalanceServices();
            builder.Services.AddHostedService<BalanceWorker>();

            var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
