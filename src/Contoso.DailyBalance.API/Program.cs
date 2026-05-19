namespace Contoso.DailyBalance.API
{
    using Contoso.DailyBalance.Services;
    using Contoso.ServiceDefaults;
    using Microsoft.AspNetCore.Mvc;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();
            builder.Services.AddProblemDetails();
            builder.AddContosoDbContext();
            builder.AddApiKeyAuthentication();
            builder.Services.AddContosoCacheServices();
            builder.Services.AddDailyBalanceServices();

            var app = builder.Build();
            app.UseMiddleware<TraceIdMiddleware>();
            app.UseExceptionHandler();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapGet("/consolidado/{data}", async (
                [FromRoute] DateTime data,
                [FromServices] IDailyBalanceService dailyBalanceService,
                [FromServices] TimeProvider timeProvider,
                CancellationToken cancellationToken) =>
            {
                var requestedDate = DateOnly.FromDateTime(data.Date);
                var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
                if (requestedDate > currentDate)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["data"] = ["A data informada não pode estar no futuro."],
                    });
                }

                var consolidatedBalance = await dailyBalanceService.GetBalanceAsync(data, cancellationToken).ConfigureAwait(false);
                return Results.Ok(consolidatedBalance);
            });

            app.MapDefaultEndpoints();

            await app.RunAsync().ConfigureAwait(false);
        }
    }
}
