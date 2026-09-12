using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Caching;

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddTaskFlowCache(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var redis = configuration.GetConnectionString("Redis");
        if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(redis))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "taskflow:";
            });
        }
        services.AddSingleton<ICacheService, DistributedCacheService>();
        return services;
    }
    

}
