using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventSender.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {
        public OrderStateMachine()
        {
            InstanceState(x => x.State);

            ConfigureEvents();

            ConfigureStates();
        }

        public State OrderCreated { get; set; }
        public State OrderReserved { get; set; }
        public State OrderReservationFailed { get; set; }
        public State OrderReservationReverted { get; set; }
        public State OrderPaymentSucceded { get; set; }
        public State OrderPaymentFailed { get; set; }

        public Event<OrderCreated> OrderCreatedEvent { get; set; }
        public Event<OrderReserved> OrderReservedEvent { get; set; }
        public Event<OrderReservationFailed> OrderReservationFailedEvent { get; set; }
        public Event<OrderReservationReverted> OrderReservationRevertedEvent { get; set; }
        public Event<OrderPaymentSucceded> OrderPaymentSuccededEvent { get; set; }
        public Event<OrderPaymentFailed> OrderPaymentFailedEvent { get; set; }

        private void ConfigureEvents()
        {
            Event(() => OrderCreatedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationFailedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationRevertedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderPaymentSuccededEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderPaymentFailedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
        }

        private void ConfigureStates()
        {
            Initially(When(OrderCreatedEvent).TransitionTo(OrderCreated));

            During(OrderCreated,
                When(OrderReservedEvent).TransitionTo(OrderReserved),
                When(OrderReservationFailedEvent).TransitionTo(OrderReservationFailed));

            During(OrderReserved,
                When(OrderPaymentSuccededEvent).TransitionTo(OrderPaymentSucceded),
                When(OrderPaymentFailedEvent).TransitionTo(OrderPaymentFailed));

            During(OrderPaymentFailed,
                When(OrderReservationRevertedEvent).TransitionTo(OrderReservationReverted));
        }
    }
}
