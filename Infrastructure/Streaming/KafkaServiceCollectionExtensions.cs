using Application.Streaming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Streaming;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddTaskFlowKafka(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        if (environment.IsEnvironment("Testing"))
            services.AddSingleton<IEventStreamPublisher, NoOpEventStreamPublisher>();
        else
            services.AddSingleton<IEventStreamPublisher, KafkaEventStreamPublisher>();

        return services;
    }
}
