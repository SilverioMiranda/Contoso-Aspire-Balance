using Contoso.Data;
using Contoso.Infrastructure.Messaging;
using Contoso.ServiceDefaults;
using Contoso.Transactions.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using static System.Net.WebRequestMethods;

namespace Contoso.Transactions.API
{

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add service defaults & Aspire components.
            builder.AddServiceDefaults();

            builder.AddContosoDbContext();
            var envs = builder.Configuration;

            builder.AddTransactionServices();

            // Add services to the container.
            builder.Services.AddProblemDetails();
            builder.AddApiKeyAuthentication();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            app.MapPost("/lancamentos", async ([FromServices] ITransactionQueueService transactionQueueService, [FromServices] TimeProvider timeProvider, [FromServices] ILogger<Program> logger, [FromBody] TransactionRequest payload, HttpContext http) =>
            {
                var success = await transactionQueueService.EnqueueAsync(payload, http.RequestAborted).ConfigureAwait(false);
                var traceId = Activity.Current?.TraceId.ToString() ?? "traceid não disponível";
                http.Response.Headers.Append("X-Trace-Id", traceId);

                if (!success.Item1)
                {
                    return Results.Problem(success.Item2,statusCode: 503);
                }
                return Results.Accepted();
            });
            app.MapGet("/lancamentos/{date:datetime}", async ([FromRoute] DateTime date, [FromServices] ITransactionService transactionService, CancellationToken cancellationToken, HttpContext http,[FromQuery] int page = 0, [FromQuery] int? limit = 30) =>
            {
                try
                {
                    var traceId = Activity.Current?.TraceId.ToString() ?? "traceid não disponível";
                    http.Response.Headers.Append("X-Trace-Id", traceId);
                    return Results.Ok(await transactionService.ListAsync(date, page, limit, cancellationToken).ConfigureAwait(false));
                } catch(Exception e)
                {
                    app.Logger.LogError(e, "Erro ao listar transações");
                    return Results.Problem("Erro ao listar transações", statusCode: 500);
                }
            });


            app.MapDefaultEndpoints();

            await app.RunAsync().ConfigureAwait(false);
        }
       
    }
}
