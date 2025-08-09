using BlitzCacheCore.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace BlitzCacheCore.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds BlitzCache as a singleton service to the dependency injection container.
        /// Uses the global BlitzCache singleton to maintain backward compatibility with the original behavior.
        /// This singleton has statistics enabled by default.
        /// For dedicated cache instances, use AddBlitzCacheInstance() instead.
        /// </summary>
        /// <param name="services">The service collection to add BlitzCache to.</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds. Defaults to 60000 (1 minute).</param>
        /// <param name="maxTopSlowest">Max number of top slowest queries to store (0 for improved performance) (default: 5 queries)</param>
        /// <param name="maxTopHeaviest">Max number of heaviest entries to track (0 disables). Default: 5.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCache(this IServiceCollection services, long defaultMilliseconds = 60000, int maxTopSlowest = 5, int maxTopHeaviest = 5)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            OptionsServiceCollectionExtensions.AddOptions(services);
            ServiceCollectionDescriptorExtensions.TryAdd(services, ServiceDescriptor.Singleton<IBlitzCache>(_ => new BlitzCache(defaultMilliseconds, maxTopSlowest: maxTopSlowest, maxTopHeaviest: maxTopHeaviest)));

            return services;
        }

        /// <summary>
        /// Adds automatic periodic logging of BlitzCache statistics.
        /// Statistics will be logged at the specified interval using the provided logger.
        /// Note: BlitzCache must be configured with statistics enabled for this to work.
        /// </summary>
        /// <param name="services">The service collection to add the logging service to.</param>
        /// <param name="logInterval">How often to log statistics. Defaults to 1 hour.</param>
        /// <param name="globalCacheIdentifier">Custom identifier for the globalCache in logs. If null or empty, auto-detects from the running application.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCacheLogging(this IServiceCollection services, TimeSpan? logInterval = null, string? globalCacheIdentifier = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddHostedService(provider => new BlitzCacheLoggingService(
                provider.GetRequiredService<ILogger<BlitzCacheLoggingService>>(),
                provider.GetService<IBlitzCache>(),
                globalCacheIdentifier,
                logInterval - TimeSpan.FromMilliseconds(1)));

            return services;
        }
    }
}
