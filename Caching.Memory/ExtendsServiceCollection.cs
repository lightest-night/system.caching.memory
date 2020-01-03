using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightestNight.System.Caching.Memory
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddMemoryCache(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(ICache), typeof(Cache));
            return services;
        }
    }
}