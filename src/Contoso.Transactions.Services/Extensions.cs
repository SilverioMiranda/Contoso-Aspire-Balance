using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contoso.Transactions.Services
{
    public static partial class Extensions
    {
        public static void AddTransactionApiServices(this IHostApplicationBuilder builder)
        {
            builder.AddContosoKafkaProducer();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddTransient<ITransactionQueueService, TransactionQueueService>();
        }

        public static void AddTransactionWorkerServices(this IHostApplicationBuilder builder)
        {
            builder.AddKafkaConsumer<string, string>("kafka", opt =>
            {
                opt.Config.GroupId = "transactions";
                opt.Config.EnableAutoCommit = false;
            });

            builder.Services.AddScoped<ITransactionService, TransactionService>();
        }
    }
}
