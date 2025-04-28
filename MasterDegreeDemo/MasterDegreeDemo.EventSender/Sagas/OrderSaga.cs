using MassTransit;

namespace MasterDegreeDemo.EventSender.Sagas
{
    public class OrderSaga : SagaStateMachineInstance
    {
        public required Guid Id { get; set; }
        public required string ProductName { get; set; }
        public required Guid CorrelationId { get; set; }
        public required string State { get; set; }
    }
}
