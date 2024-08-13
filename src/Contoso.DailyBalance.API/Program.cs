namespace Contoso.DailyBalance.API
{
    using Contoso.DailyBalance.Services;
    using Contoso.Data;
    using Contoso.ServiceDefaults;
    using Microsoft.AspNetCore.Mvc;

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
            builder.AddApiKeyAuthentication();
            builder.Services.AddOutputCache();

            builder.Services.AddDailyBalanceServices();

            var app = builder.Build();
            // Adiciona o middleware de Trace-Id ao pipeline
            app.UseMiddleware<TraceIdMiddleware>();
            app.UseAuthorization();
            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            app.MapGet("/consolidado/{data}", async ([FromRoute] DateTime data, [FromServices] IDailyBalanceService dailyBalanceService, HttpContext http, CancellationToken cancellationToken) =>
            {
                var consolidado = await dailyBalanceService.GetBalanceAsync(data, cancellationToken).ConfigureAwait(false);
                return Results.Ok(consolidado);
            });

            app.MapDefaultEndpoints();

            await app.RunAsync().ConfigureAwait(false);

        }
    }
}