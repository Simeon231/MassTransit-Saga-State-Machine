using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class CompensateOrderReservationConsumer(ILogger<CompensateOrderReservationConsumer> logger) : IConsumer<CompensateOrderReservation>
    {
        public static event Action<Order> OnReceived = null!;

        public static TaskCompletionSource<bool> Resume { get; private set; } = CreateTaskCompletionSource();

        public async Task Consume(ConsumeContext<CompensateOrderReservation> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(CompensateOrderReservationConsumer), context.Message.Order.Id);

            if (OnReceived is null)
            {
                logger.LogWarning("No listeners found");
                return;
            }

            bool isSuccessful = await CompensateOrder(context.Message.Order);
            if (isSuccessful)
            {
                logger.LogInformation("Order reservation with id {Id} compensated", context.Message.Order.Id);
                await context.Publish(new OrderReservationCompensated(context.Message.Order));
            }
            else
            {
                logger.LogError("Could not compensate order resarvation with id {Id}", context.Message.Order.Id);
                throw new Exception($"Could not compensate order resarvation with id {context.Message.Order.Id}");
            }
        }

        private static async Task<bool> CompensateOrder(Order order)
        {
            Resume = CreateTaskCompletionSource();

            OnReceived.Invoke(order);

            return await Resume.Task;
        }

        private static TaskCompletionSource<bool> CreateTaskCompletionSource()
        {
            return new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
