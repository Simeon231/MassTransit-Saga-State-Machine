using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class OrderPaymentFailedConsumer(ILogger<OrderPaymentFailedConsumer> logger) : IConsumer<OrderPaymentFailed>
    {
        public static event Action<Order> OnReceived = null!;

        public async Task Consume(ConsumeContext<OrderPaymentFailed> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(OrderPaymentFailedConsumer), context.Message.Order.Id);

            OnReceived?.Invoke(context.Message.Order);

            await context.Publish(new OrderReservationReverted(context.Message.Order));
        }
    }
}
