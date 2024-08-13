namespace Contoso
{
    using Contoso.Data;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class Extensions
    {
        public static IServiceCollection AddContosoDbContext(this IHostApplicationBuilder builder)
        {
            builder.AddSqlServerDbContext<ContosoDbContext>("db1");
            //builder.EnrichSqlServerDbContext<ContosoDbContext>();
            return builder.Services;
        }
       
    }
}
