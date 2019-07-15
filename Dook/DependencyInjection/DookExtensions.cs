using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dook
{
    public static class DookExtensions
    {
        public static IServiceCollection AddDookContext<T>(this IServiceCollection services, Action<DookConfigurationOptions<T>> options) where T : Context
        {
            //Adding configuration
            services.AddScoped<DookConfigurationOptions<T>>(sp => 
            {
                DookConfigurationOptions<T> configuration = new DookConfigurationOptions<T>();
                options.Invoke(configuration);
                return configuration;
            });
            //Adding context instance
            services.AddScoped<T>();
            return services;
        }
    }
}