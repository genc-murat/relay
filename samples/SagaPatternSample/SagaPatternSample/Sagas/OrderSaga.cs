using Relay.MessageBroker.Saga;
using SagaPatternSample.Sagas.Steps;

namespace SagaPatternSample.Sagas;

public class OrderSaga : Saga<OrderSagaData>
{
    public OrderSaga(
        ReserveInventoryStep reserveInventoryStep,
        ProcessPaymentStep processPaymentStep,
        CreateShipmentStep createShipmentStep)
    {
        // Add saga steps in order
        AddStep(reserveInventoryStep);
        AddStep(processPaymentStep);
        AddStep(createShipmentStep);
    }
}
