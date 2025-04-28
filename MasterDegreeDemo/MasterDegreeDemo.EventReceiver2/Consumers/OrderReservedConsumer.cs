using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver2.Consumers
{
    public class OrderReservedConsumer(ILogger<OrderReservedConsumer> logger) : IConsumer<OrderReserved>
    {
        private readonly ILogger<OrderReservedConsumer> logger = logger;

        public static event Action<OrderReserved> OnReceived = null!;

        public Task Consume(ConsumeContext<OrderReserved> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(OrderReservedConsumer), context.Message.Order.Id);

            OnReceived?.Invoke(context.Message);

            return Task.CompletedTask;
        }
    }
}
