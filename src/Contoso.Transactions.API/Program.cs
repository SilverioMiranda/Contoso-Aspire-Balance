using Contoso.ServiceDefaults;
using Contoso.Transactions.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Contoso.Transactions.API
{
    public class Program
    {
        private const string IdempotencyKeyHeaderName = "X-Idempotency-Key";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder);

            var app = builder.Build();
            ConfigurePipeline(app);
            MapEndpoints(app);
            app.MapDefaultEndpoints();

            await app.RunAsync().ConfigureAwait(false);
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.AddServiceDefaults();
            builder.AddContosoDbContext();
            builder.AddTransactionApiServices();
            builder.Services.AddProblemDetails();
            builder.AddApiKeyAuthentication();
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            app.UseExceptionHandler();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void MapEndpoints(WebApplication app)
        {
            app.MapPost("/lancamentos", async (
                [FromServices] ITransactionQueueService transactionQueueService,
                [FromServices] TimeProvider timeProvider,
                [FromBody] TransactionRequest payload,
                HttpContext http) =>
            {
                var validationErrors = ValidatePayload(payload);
                if (validationErrors is not null)
                {
                    return Results.ValidationProblem(validationErrors);
                }

                if (!TryResolveRequestId(http.Request.Headers, out var requestId, out var requestIdErrors))
                {
                    return Results.ValidationProblem(requestIdErrors!);
                }

                var traceId = Activity.Current?.TraceId.ToString() ?? "traceid-nao-disponivel";
                http.Response.Headers.Append("X-Trace-Id", traceId);
                http.Response.Headers.Append(IdempotencyKeyHeaderName, requestId.ToString());

                var queuedTransaction = new QueuedTransaction(
                    requestId,
                    payload.Amount,
                    payload.Description,
                    timeProvider.GetUtcNow());

                var result = await transactionQueueService.EnqueueAsync(queuedTransaction, http.RequestAborted).ConfigureAwait(false);
                if (!result.Item1)
                {
                    return Results.Problem(result.Item2, statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                return Results.Accepted(value: new { RequestId = requestId });
            });

            app.MapGet("/lancamentos/{date:datetime}", async (
                [FromRoute] DateTime date,
                [FromServices] ITransactionService transactionService,
                CancellationToken cancellationToken,
                HttpContext http,
                [FromQuery] int page = 0,
                [FromQuery] int? limit = 30) =>
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? "traceid-nao-disponivel";
                http.Response.Headers.Append("X-Trace-Id", traceId);
                return Results.Ok(await transactionService.ListAsync(date, page, limit, cancellationToken).ConfigureAwait(false));
            });
        }

        private static Dictionary<string, string[]>? ValidatePayload(TransactionRequest payload)
        {
            var validationContext = new ValidationContext(payload);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(payload, validationContext, validationResults, validateAllProperties: true);
            if (isValid)
            {
                return null;
            }

            return validationResults
                .GroupBy(result => result.MemberNames.FirstOrDefault() ?? "payload", StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(result => result.ErrorMessage ?? "Payload inválido.").ToArray(),
                    StringComparer.Ordinal);
        }

        private static bool TryResolveRequestId(IHeaderDictionary headers, out Guid requestId, out Dictionary<string, string[]>? errors)
        {
            if (!headers.TryGetValue(IdempotencyKeyHeaderName, out var headerValues) || string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
            {
                requestId = Guid.NewGuid();
                errors = null;
                return true;
            }

            if (Guid.TryParse(headerValues.First(), out requestId))
            {
                errors = null;
                return true;
            }

            errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [IdempotencyKeyHeaderName] =
                [
                    $"O header {IdempotencyKeyHeaderName} deve ser um GUID válido quando informado.",
                ],
            };

            return false;
        }
    }
}
