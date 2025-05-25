using MassTransit;
using MasterDegreeDemo.ServiceDefaults;

#pragma warning disable CS8618

namespace MasterDegreeDemo.EventSender.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {
        public static event Action OnReceive = null!;
        public static Dictionary<Guid, OrderSaga> Sagas = [];

        public OrderStateMachine(ILogger<OrderStateMachine> logger)
        {
            InstanceState(x => x.State);

            ConfigureEvents();

            ConfigureStates(logger);
        }

        public State Submitted { get; }
        public State Reserved { get; }
        public State ReservationFaulted { get; }
        public State ReservationCompensated { get; }
        public State PaymentCaptured { get; }
        public State PaymentFailed { get; }
        public State CompensationFailed { get; set; }

        public Event<SubmitOrder> SubmittedEvent { get; }
        public Event<OrderReserved> ReservedEvent { get; }
        public Event<Fault<ReserveOrder>> ReservationFaultedEvent { get; }
        public Event<OrderReservationCompensated> ReservationCompensatedEvent { get; }
        public Event<Fault<CompensateOrderReservation>> CompensateReservationFaultedEvent { get; }
        public Event<OrderPaymentProcessed> PaymentSucceededEvent { get; }
        public Event<Fault<ProcessOrderPayment>> PaymentFaultedEvent { get; }

        private void ConfigureEvents()
        {
            Event(() => SubmittedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => ReservedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => ReservationFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
            Event(() => ReservationCompensatedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => CompensateReservationFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
            Event(() => PaymentSucceededEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => PaymentFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
        }

        private void ConfigureStates(ILogger<OrderStateMachine> logger)
        {
            Initially(When(SubmittedEvent)
                .Then(context =>
                {
                    context.Saga.Id = context.Message.Order.Id;
                    context.Saga.ProductName = context.Message.Order.ProductName;
                })
                .Publish(x => new ReserveOrder(x.Message.Order))
                .TransitionTo(Submitted));

            During(Submitted,
                When(ReservedEvent)
                    .Publish(x => new ProcessOrderPayment(x.Message.Order))
                    .TransitionTo(Reserved),
                When(ReservationFaultedEvent)
                    .TransitionTo(ReservationFaulted));

            During(Reserved,
                When(PaymentSucceededEvent)
                    .TransitionTo(PaymentCaptured),
                When(PaymentFaultedEvent)
                    .Publish(x => new CompensateOrderReservation(x.Message.Message.Order))
                    .TransitionTo(PaymentFailed));

            During(PaymentFailed,
                When(ReservationCompensatedEvent)
                    .TransitionTo(ReservationCompensated),
                When(CompensateReservationFaultedEvent)
                    .Then(context =>
                    {
                        logger.LogCritical(
                            "Order with an id {Id} could not be compensated. Attention required",
                            context.Message.Message.Order.Id);
                    })
                    .TransitionTo(CompensationFailed));

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
