namespace Contoso.DailyBalance.API
{
    public class TraceIdMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // Gera ou recupera um Trace-Id existente no header
            var traceId = context.TraceIdentifier;

            // Adiciona o Trace-Id ao header da resposta
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Trace-Id"] = traceId;
                return Task.CompletedTask;
            });

            await next(context).ConfigureAwait(false);
        }
    }
}