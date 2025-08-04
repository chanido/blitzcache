using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace BlitzCacheCore.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds BlitzCache as a singleton service to the dependency injection container.
        /// Uses the global BlitzCache singleton to maintain backward compatibility with the original behavior.
        /// For dedicated cache instances, use AddBlitzCacheInstance() instead.
        /// </summary>
        /// <param name="services">The service collection to add BlitzCache to.</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds. Defaults to 60000 (1 minute).</param>
        /// <param name="enableStatistics">Whether to enable statistics tracking (default: false for better performance).</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCache(this IServiceCollection services, long defaultMilliseconds = 60000, bool enableStatistics = false)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            OptionsServiceCollectionExtensions.AddOptions(services);
            ServiceCollectionDescriptorExtensions.TryAdd(services, ServiceDescriptor.Singleton<IBlitzCache>(_ => BlitzCache.Global));

            return services;
        }

        /// <summary>
        /// Adds a new BlitzCache instance as a singleton to the dependency injection container.
        /// This creates a dedicated cache instance that doesn't share data with other instances.
        /// </summary>
        /// <param name="services">The service collection to add BlitzCache to.</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds. Defaults to 60000 (1 minute).</param>
        /// <param name="enableStatistics">Whether to enable statistics tracking (default: false for better performance).</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCacheInstance(this IServiceCollection services, long defaultMilliseconds = 60000, bool enableStatistics = false)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            OptionsServiceCollectionExtensions.AddOptions(services);
            ServiceCollectionDescriptorExtensions.TryAdd(services, ServiceDescriptor.Singleton<IBlitzCache>(new BlitzCache(defaultMilliseconds, enableStatistics)));

            return services;
        }
    }
}
