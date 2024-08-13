using Microsoft.Extensions.DependencyInjection;

namespace Contoso.DailyBalance.Services
{
    public static partial class Extensions
    {
        public static IServiceCollection AddDailyBalanceServices(this IServiceCollection services)
        {
            services.AddScoped<IDailyBalanceService, DailyBalanceService>();
            return services;
        }
    }
}
