using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
    {
        public static event Action<Order> OnReceived = null!;

        private readonly ILogger<OrderCreatedConsumer> logger = logger;

        public static TaskCompletionSource<bool> Resume { get; private set; } = CreateTaskCompletionSource();

        public async Task Consume(ConsumeContext<OrderCreated> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(OrderCreatedConsumer), context.Message.Order.Id);

            if (OnReceived is null)
            {
                logger.LogWarning("No listeners");
                return;
            }

            OnReceived.Invoke(context.Message.Order);

            var success = await Resume.Task;
            if (success)
            {
                logger.LogInformation("Order reserved with id {Id}", context.Message.Order.Id);
                await context.Publish(new OrderReserved(context.Message.Order));
            }
            else
            {
                logger.LogInformation("Order reservation failed with id {Id}", context.Message.Order.Id);
                await context.Publish(new OrderReservationFailed(context.Message.Order));
            }

            Resume = CreateTaskCompletionSource();
        }

        private static TaskCompletionSource<bool> CreateTaskCompletionSource()
        {
            return new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
