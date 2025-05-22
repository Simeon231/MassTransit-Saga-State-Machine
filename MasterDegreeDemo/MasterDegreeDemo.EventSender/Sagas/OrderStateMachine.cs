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

        public State Submitted { get; }
        public State Reserved { get; }
        public State ReservationFailed { get; }
        public State ReservationReverted { get; }
        public State PaymentCaptured { get; }
        public State PaymentFailed { get; }

        public Event<SubmitOrder> OrderSubmittedEvent { get; }
        public Event<OrderReserved> OrderReservedEvent { get; }
        public Event<OrderReservationFailed> OrderReservationFailedEvent { get; }
        public Event<OrderReservationReverted> OrderReservationRevertedEvent { get; }
        public Event<OrderPaymentProcessed> OrderPaymentSucceededEvent { get; }
        public Event<OrderPaymentFailed> OrderPaymentFailedEvent { get; }

        private void ConfigureEvents()
        {
            Event(() => OrderSubmittedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationFailedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservationRevertedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderPaymentSucceededEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderPaymentFailedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
        }

        private void ConfigureStates()
        {
            Initially(When(OrderSubmittedEvent)
                .Then(context =>
                {
                    context.Saga.Id = context.Message.Order.Id;
                    context.Saga.ProductName = context.Message.Order.ProductName;
                })
                .Publish(x => new ReserveOrder(x.Message.Order))
                .TransitionTo(Submitted));

            During(Submitted,
                When(OrderReservedEvent)
                    .Publish(x => new ProcessOrderPayment(x.Message.Order))
                    .TransitionTo(Reserved),
                When(OrderReservationFailedEvent)
                    .TransitionTo(ReservationFailed));

            During(Reserved,
                When(OrderPaymentSucceededEvent)
                    .TransitionTo(PaymentCaptured),
                When(OrderPaymentFailedEvent)
                    .Publish(x => new RevertOrderReservation(x.Message.Order))
                    .TransitionTo(PaymentFailed));

            During(PaymentFailed,
                When(OrderReservationRevertedEvent)
                    .TransitionTo(ReservationReverted));

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
