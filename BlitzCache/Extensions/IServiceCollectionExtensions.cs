using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlitzCache.Extensions
{
    public static class IServiceCollectionExtensions
    {
        //
        // Summary:
        //     /// Adds a non distributed in memory implementation of BlitzCache
        //     to the /// Microsoft.Extensions.DependencyInjection.IServiceCollection. ///
        //
        // Parameters:
        //   services:
        //     The Microsoft.Extensions.DependencyInjection.IServiceCollection to add services
        //     to.
        //
        // Returns:
        //     The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional
        //     calls can be chained.
        public static IServiceCollection AddBlitzCache(this IServiceCollection services, long defaultMilliseconds = 60000)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }
            OptionsServiceCollectionExtensions.AddOptions(services);
            ServiceCollectionDescriptorExtensions.TryAdd(services, ServiceDescriptor.Singleton(new BlitzCache(defaultMilliseconds)));
            return services;
        }
    }
}
