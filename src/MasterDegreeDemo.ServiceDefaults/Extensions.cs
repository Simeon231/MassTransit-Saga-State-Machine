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

            conf.UsingAmazonSqs((context, configurator) =>
            {
                configurator.Host("eu-west-1", _ => { });

                // To prevent creation of a lot of queues.
                configurator.AutoDelete = true;

                var prefixEntityFormatter = new PrefixEntityNameFormatter(configurator.MessageTopology.EntityNameFormatter, environment);
                configurator.MessageTopology.SetEntityNameFormatter(prefixEntityFormatter);

                var kebabEndpointFormatter = new KebabCaseEndpointNameFormatter(environment);
                configurator.ConfigureEndpoints(context, kebabEndpointFormatter);
            });
        });
    }
}