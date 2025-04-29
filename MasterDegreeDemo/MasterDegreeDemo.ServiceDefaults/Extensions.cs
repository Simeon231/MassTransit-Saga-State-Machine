using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IServiceCollection AddMasstransitService(this IServiceCollection services, Type assemblyMarker, string environment)
    {
        return services.AddMassTransit(conf =>
        {
            conf.AddSagaStateMachinesFromNamespaceContaining(assemblyMarker);
            conf.AddConsumersFromNamespaceContaining(assemblyMarker);

            conf.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host("eu-west-1", _ => { });

                var prefixEntityFormatter = new PrefixEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter, environment);
                cfg.MessageTopology.SetEntityNameFormatter(prefixEntityFormatter);

                var kebabEndpointFormatter = new KebabCaseEndpointNameFormatter(environment);
                cfg.ConfigureEndpoints(context, kebabEndpointFormatter);
            });
        });
    }
}