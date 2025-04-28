using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
    {
        private readonly ILogger<OrderCreatedConsumer> logger = logger;

        public static event Action<Order> OrderCreated = null!;

        public Task Consume(ConsumeContext<OrderCreated> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(OrderCreatedConsumer), context.Message.Order.Id);

            OrderCreated?.Invoke(context.Message.Order);

            return Task.CompletedTask;
        }
    }
}
