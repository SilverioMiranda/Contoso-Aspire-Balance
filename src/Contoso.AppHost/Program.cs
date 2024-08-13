
namespace Contoso.AppHost
{

    public class Program
    {
        public static async Task Main(string[] args)
        {

            var builder = DistributedApplication.CreateBuilder(args);

            var messaging = builder.AddKafka("kafka").WithHealthCheck();
            var kafkaUi = messaging.WithKafkaUI().WaitForCompletion(messaging).WithHealthCheck();
            //var messaging = builder.AddKafka("kafka").WithKafkaUI().WithHealthCheck();
            var db1 = builder.AddSqlServer("sql").AddDatabase("db1");

            var cache = builder.AddRedis("cache");

            var db1_migrator = builder.AddProject<Projects.Contoso_DatabaseMigrationService>("db1migrator")
                .WithReference(db1)
                .WithReplicas(1)
                .WaitFor(db1)
                ;

            var dailybalance_api = builder.AddProject<Projects.Contoso_DailyBalance_API>("dailybalance-api")
                .WithReference(cache)
                .WithReference(db1)
                .WithReference(messaging)
                .WithReplicas(2)
                .WaitFor(db1)
                .WaitFor(messaging)
                .WaitForCompletion(db1_migrator)
                .WaitFor(kafkaUi)
                //.WaitFor(cache)
                ;

            var dailybalance_worker = builder.AddProject<Projects.Contoso_DailyBalance_Worker>("dailybalance-worker")
               .WithReference(cache)
               .WithReference(messaging)
               .WithReference(db1)
               .WaitFor(db1)
               .WaitFor(messaging)
               .WaitForCompletion(db1_migrator)
               .WaitFor(kafkaUi)
               // .WaitFor(cache)
               ;

            var transactions_api = builder.AddProject<Projects.Contoso_Transactions_API>("transactions-api")
                .WithReference(cache)
                .WithReference(db1)
                .WithReference(messaging)
                .WithReplicas(2)
                .WaitFor(db1)
                .WaitFor(messaging)
                .WaitForCompletion(db1_migrator)
                //.WaitFor(kafkaUi)
                //.WaitFor(cache)
                ;
            var transactions_worker = builder.AddProject<Projects.Contoso_Transactions_Worker>("transactions-worker")
                .WithReference(cache)
                .WithReference(messaging)
                .WithReference(db1)
                .WaitFor(db1)
                .WaitFor(messaging)
                .WaitForCompletion(db1_migrator)
                .WaitFor(kafkaUi)
                // .WaitFor(cache)
                ;

            builder.AddProject<Projects.Contoso_Web>("webfrontend")
                .WithExternalHttpEndpoints()
                .WithReference(cache)
                .WithReference(db1)
                .WithReference(dailybalance_api)
                .WithReference(transactions_api)
                ;

            await builder.Build().RunAsync();
        }
    }
}