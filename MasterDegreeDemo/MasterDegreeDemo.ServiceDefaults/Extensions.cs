using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IServiceCollection AddMasstransitService(this IServiceCollection services, Type assemblyMarker)
    {
        return services.AddMassTransit(conf =>
        {
            conf.AddSagaStateMachinesFromNamespaceContaining(assemblyMarker);
            conf.AddConsumersFromNamespaceContaining(assemblyMarker);

            conf.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host("eu-west-1", _ => { });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
