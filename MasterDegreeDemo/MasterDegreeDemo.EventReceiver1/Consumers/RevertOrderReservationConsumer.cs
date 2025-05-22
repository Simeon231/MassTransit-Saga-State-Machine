using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventReceiver1.Consumers
{
    public class RevertOrderReservationConsumer(ILogger<RevertOrderReservationConsumer> logger) : IConsumer<RevertOrderReservation>
    {
        public static event Action<Order> OnReceived = null!;

        public async Task Consume(ConsumeContext<RevertOrderReservation> context)
        {
            logger.LogInformation("Received on {Consumer} an event with id {Id}", nameof(RevertOrderReservationConsumer), context.Message.Order.Id);

            OnReceived?.Invoke(context.Message.Order);

            await context.Publish(new OrderReservationReverted(context.Message.Order));
        }
    }
}
