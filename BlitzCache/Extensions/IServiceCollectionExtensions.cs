using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BlitzCacheCore.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds BlitzCache as a singleton service to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to add BlitzCache to.</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds. Defaults to 60000 (1 minute).</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCache(this IServiceCollection services, long defaultMilliseconds = 60000) =>
            AddBlitzCache(services, defaultMilliseconds, useGlobalCache: true);

        /// <summary>
        /// Adds BlitzCache as a singleton service to the dependency injection container with configuration options.
        /// </summary>
        /// <param name="services">The service collection to add BlitzCache to.</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds.</param>
        /// <param name="useGlobalCache">Whether to use a global shared cache (true) or instance-specific cache (false).</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCache(this IServiceCollection services, long defaultMilliseconds, bool useGlobalCache)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            OptionsServiceCollectionExtensions.AddOptions(services);
            ServiceCollectionDescriptorExtensions.TryAdd(services, ServiceDescriptor.Singleton<IBlitzCache>(new BlitzCache(defaultMilliseconds, useGlobalCache)));

            return services;
        }
    }
}
