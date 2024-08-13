using Contoso.Data;
using Contoso.ServiceDefaults;

namespace Contoso.DatabaseMigrationService
{
    public static class Program
    {
        public static void Main(string[] args)
        {

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<ApiDbInitializer>();

            builder.AddServiceDefaults();

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddSource(ApiDbInitializer.ActivitySourceName));

            builder.AddSqlServerDbContext<ContosoDbContext>("db1");

            var app = builder.Build();

            app.Run();
        }
    }
}