using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

namespace MasterDegreeDemo.EventSender.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {
        public static event Action OnReceive = null!;
        public static Dictionary<Guid, OrderSaga> Sagas = [];

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
            Initially(When(OrderCreatedEvent)
                .Then(context =>
                {
                    context.Saga.Id = context.Message.Order.Id;
                    context.Saga.ProductName = context.Message.Order.ProductName;
                })
                .TransitionTo(OrderCreated));

            During(OrderCreated,
                When(OrderReservedEvent).TransitionTo(OrderReserved),
                When(OrderReservationFailedEvent).TransitionTo(OrderReservationFailed));

            During(OrderReserved,
                When(OrderPaymentSuccededEvent).TransitionTo(OrderPaymentSucceded),
                When(OrderPaymentFailedEvent).TransitionTo(OrderPaymentFailed));

            During(OrderPaymentFailed,
                When(OrderReservationRevertedEvent).TransitionTo(OrderReservationReverted));

            WhenEnterAny(x =>
                x.Then(binder =>
                {
                    if (binder.Saga.State == nameof(Initial))
                    {
                        return;
                    }

                    if (!Sagas.ContainsKey(binder.Saga.Id))
                    {
                        Sagas.Add(binder.Saga.Id, binder.Saga);
                    }
                    else
                    {
                        Sagas[binder.Saga.Id] = binder.Saga;
                    }

                    OnReceive?.Invoke();
                }));
        }
    }
}
