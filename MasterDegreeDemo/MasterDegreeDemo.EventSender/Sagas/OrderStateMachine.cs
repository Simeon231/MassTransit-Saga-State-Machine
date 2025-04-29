using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

#pragma warning disable CS8618

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

        public State OrderCreated { get; }
        public State OrderReserved { get; }
        public State OrderReservationFailed { get; }
        public State OrderReservationReverted { get; }
        public State OrderPaymentSucceeded { get; }
        public State OrderPaymentFailed { get; }

        public Event<OrderCreated> OrderCreatedEvent { get; }
        public Event<OrderReserved> OrderReservedEvent { get; }
        public Event<OrderReservationFailed> OrderReservationFailedEvent { get; }
        public Event<OrderReservationReverted> OrderReservationRevertedEvent { get; }
        public Event<OrderPaymentSucceeded> OrderPaymentSucceededEvent { get; }
        public Event<OrderPaymentFailed> OrderPaymentFailedEvent { get; }

        private void ConfigureEvents()
        {
            Event(() => OrderCreatedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationFailedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationRevertedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderPaymentSucceededEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
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
                When(OrderPaymentSucceededEvent).TransitionTo(OrderPaymentSucceeded),
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
