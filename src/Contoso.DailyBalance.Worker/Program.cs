namespace Contoso.DailyBalance.Worker
{
    using Contoso.Data;
    using Contoso.ServiceDefaults;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add service defaults & Aspire components.
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddProblemDetails();
            builder.AddContosoDbContext();

            //builder.Services.AddHostedService<BalanceWorker>();
            var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
    }
}