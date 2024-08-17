using Contoso.CacheService;
using Microsoft.Extensions.DependencyInjection;

namespace Contoso
{
    public static partial class Extensions
    {
        public static IServiceCollection AddContosoCacheServices(this IServiceCollection services)
        {
            services.AddTransient<IContosoCache, ContosoCache>();
            return services;
        }
    }
}
