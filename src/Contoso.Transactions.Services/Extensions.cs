using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contoso.Transactions.Services
{
    public static partial class Extensions
    {
        public static void AddTransactionServices(this IHostApplicationBuilder builder)
        {
            builder.AddContosoKafkaProducer();
            builder.AddKafkaConsumer<string, string>("kafka", opt =>
            {
                opt.Config.GroupId = "transactions";
                opt.Config.EnableAutoCommit = false;
            });
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddTransient<ITransactionQueueService, TransactionQueueService>();
        }
    }
}
