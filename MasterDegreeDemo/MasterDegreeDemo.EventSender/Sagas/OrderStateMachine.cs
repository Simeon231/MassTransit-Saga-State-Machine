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
        public State PaymentProcessed { get; }
        public State PaymentFaulted { get; }
        public State CompensationFaulted { get; set; }

        public Event<SubmitOrder> SubmitOrderEvent { get; }
        public Event<OrderReserved> OrderReservedEvent { get; }
        public Event<Fault<ReserveOrder>> ReserveOrderFaultedEvent { get; }
        public Event<OrderReservationCompensated> OrderReservationCompensatedEvent { get; }
        public Event<Fault<CompensateOrderReservation>> CompensateOrderReservationFaultedEvent { get; }
        public Event<OrderPaymentProcessed> OrderPaymentProcessedEvent { get; }
        public Event<Fault<ProcessOrderPayment>> ProcessOrderPaymentFaultedEvent { get; }

        private void ConfigureEvents()
        {
            Event(() => SubmitOrderEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => OrderReservedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => ReserveOrderFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
            Event(() => OrderReservationCompensatedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => CompensateOrderReservationFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
            Event(() => OrderPaymentProcessedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Order.Id));
            Event(() => ProcessOrderPaymentFaultedEvent, configutor => configutor.CorrelateById(saga => saga.Message.Message.Order.Id));
        }

        private void ConfigureStates(ILogger<OrderStateMachine> logger)
        {
            Initially(When(SubmitOrderEvent)
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
                When(ReserveOrderFaultedEvent)
                    .TransitionTo(ReservationFaulted));

            During(Reserved,
                When(OrderPaymentProcessedEvent)
                    .TransitionTo(PaymentProcessed),
                When(ProcessOrderPaymentFaultedEvent)
                    .Publish(x => new CompensateOrderReservation(x.Message.Message.Order))
                    .TransitionTo(PaymentFaulted));

            During(PaymentFaulted,
                When(OrderReservationCompensatedEvent)
                    .TransitionTo(ReservationCompensated),
                When(CompensateOrderReservationFaultedEvent)
                    .Then(context =>
                    {
                        logger.LogCritical(
                            "Order with an id {Id} could not be compensated!",
                            context.Message.Message.Order.Id);
                    })
                    .TransitionTo(CompensationFaulted));

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
