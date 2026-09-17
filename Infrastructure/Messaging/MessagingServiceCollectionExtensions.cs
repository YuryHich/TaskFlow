using Application.Messaging;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTaskFlowMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<NotificationConsumer>();
            x.AddConsumer<AuditConsumer>();

            if (environment.IsEnvironment("Testing"))
            {
                x.UsingInMemory((context, cfg) => ConfigureReceiveEndpoints(context, cfg));
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbit.Host, (ushort)rabbit.Port, rabbit.VirtualHost, h =>
                    {
                        h.Username(rabbit.UserName);
                        h.Password(rabbit.Password);
                    });
                    ConfigureReceiveEndpoints(context, cfg);
                });
            }
        });

        return services;
    }

    private static void ConfigureReceiveEndpoints(
        IBusRegistrationContext context,
        IBusFactoryConfigurator cfg)
    {
        cfg.ReceiveEndpoint("taskflow-notifications", e =>
        {
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(8),
                intervalDelta: TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<NotificationConsumer>(context);
        });

        cfg.ReceiveEndpoint("taskflow-audit", e =>
        {
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Exponential(
                3,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(2)));
            e.ConfigureConsumer<AuditConsumer>(context);
        });
    }
}