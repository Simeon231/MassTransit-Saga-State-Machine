using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver2.Consumers
{
    public class ProcessOrderPaymentConsumer(ILogger<ProcessOrderPaymentConsumer> logger) : IConsumer<ProcessOrderPayment>
    {
        public static event Action<Order> OnReceived = null!;

        public static TaskCompletionSource<bool> Resume { get; private set; } = CreateTaskCompletionSource();

        public async Task Consume(ConsumeContext<ProcessOrderPayment> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(ProcessOrderPaymentConsumer), context.Message.Order.Id);

            bool isSuccessful = await ProcessPayment(context.Message.Order);
            if (isSuccessful)
            {
                logger.LogInformation("Payment successful with id {Id}", context.Message.Order.Id);

                await context.Publish(new OrderPaymentProcessed(context.Message.Order));
            }
            else
            {
                logger.LogError("Payment failed with id {Id}", context.Message.Order.Id);
                throw new Exception($"Payment failed with id {context.Message.Order.Id}");
            }
        }

        private async Task<bool> ProcessPayment(Order order)
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
