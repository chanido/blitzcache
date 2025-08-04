using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace BlitzCacheCore.Logging
{
    public static class BlitzCacheLoggingExtensions
    {
        /// <summary>
        /// Adds automatic periodic logging of BlitzCache statistics.
        /// Statistics will be logged at the specified interval using the provided logger.
        /// Note: BlitzCache must be configured with statistics enabled for this to work.
        /// </summary>
        /// <param name="services">The service collection to add the logging service to.</param>
        /// <param name="logInterval">How often to log statistics. Defaults to 1 hour.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCacheLogging(this IServiceCollection services, TimeSpan? logInterval = null) 
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddHostedService<BlitzCacheLoggingService>(provider => 
                new BlitzCacheLoggingService(
                    provider.GetRequiredService<IBlitzCache>(),
                    provider.GetRequiredService<ILogger<BlitzCacheLoggingService>>(),
                    logInterval ?? TimeSpan.FromHours(1)
                ));

            return services;
        }
    }
}
