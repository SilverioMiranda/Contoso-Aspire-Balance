namespace Contoso.Transactions.Worker
{
    using Contoso.ServiceDefaults;
    using Contoso.Transactions.Services;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();
            builder.AddContosoDbContext();
            builder.AddTransactionWorkerServices();
            builder.Services.AddHostedService<TransactionsWorker>();

            var host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
