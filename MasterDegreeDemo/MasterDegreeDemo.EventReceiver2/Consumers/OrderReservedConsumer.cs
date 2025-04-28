using MassTransit;
using MassTransit.Transports;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver2.Consumers
{
    public class OrderReservedConsumer(ILogger<OrderReservedConsumer> logger) : IConsumer<OrderReserved>
    {
        private readonly ILogger<OrderReservedConsumer> logger = logger;

        public static event Action<Order> OnReceived = null!;

        public static TaskCompletionSource<bool> Resume { get; private set; } = CreateTaskCompletionSource();

        public async Task Consume(ConsumeContext<OrderReserved> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(OrderReservedConsumer), context.Message.Order.Id);

            if (OnReceived is null)
            {
                logger.LogWarning("No listeners");
                return;
            }

            OnReceived.Invoke(context.Message.Order);

            var success = await Resume.Task;
            if (success)
            {
                logger.LogInformation("Payment successful with id {Id}", context.Message.Order.Id);

                await context.Publish(new OrderPaymentSucceded(context.Message.Order));
            }
            else
            {
                logger.LogError("Payment failed with id {Id}", context.Message.Order.Id);

                await context.Publish(new OrderPaymentFailed(context.Message.Order));
            }

            Resume = CreateTaskCompletionSource();
        }

        private static TaskCompletionSource<bool> CreateTaskCompletionSource()
        {
            return new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
