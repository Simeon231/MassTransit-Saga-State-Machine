using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class ReserveOrderConsumer(ILogger<ReserveOrderConsumer> logger) : IConsumer<ReserveOrder>
    {
        public static event Action<Order> OnReceived = null!;

        public static TaskCompletionSource<bool> Resume { get; private set; } = CreateTaskCompletionSource();

        public async Task Consume(ConsumeContext<ReserveOrder> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(ReserveOrderConsumer), context.Message.Order.Id);

            bool isSuccessful = await ReserveOrder(context.Message.Order);
            if (isSuccessful)
            {
                logger.LogInformation("Order reserved with id {Id}", context.Message.Order.Id);
                await context.Publish(new OrderReserved(context.Message.Order));
            }
            else
            {
                logger.LogError("Order reservation failed with id {Id}", context.Message.Order.Id);
                throw new Exception($"Order reservation failed with id {context.Message.Order.Id}");
            }
        }

        private async Task<bool> ReserveOrder(Order order)
        {
            if (OnReceived is null)
            {
                logger.LogWarning("No listeners found");
                return false;
            }

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
