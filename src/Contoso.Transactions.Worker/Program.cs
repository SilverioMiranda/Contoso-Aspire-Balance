namespace Contoso.Transactions.Worker
{
    using Contoso.Data;
    using Contoso.ServiceDefaults;
    using Contoso.Transactions.Services;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add service defaults & Aspire components.
            builder.AddServiceDefaults();

            builder.Services.AddContosoCacheServices();
            // Add services to the container.
            builder.Services.AddProblemDetails();
            builder.AddContosoDbContext();
            
            builder.AddTransactionServices();
            builder.Services.AddHostedService<TransactionsWorker>();
            var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
    }
}