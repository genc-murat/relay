using Relay.MessageBroker.Saga;
using SagaPatternSample.Services;

namespace SagaPatternSample.Sagas.Steps;

public class CreateShipmentStep : SagaStep<OrderSagaData>
{
    private readonly ShippingService _shippingService;
    private readonly ILogger<CreateShipmentStep> _logger;

    public CreateShipmentStep(
        ShippingService shippingService,
        ILogger<CreateShipmentStep> logger)
    {
        _shippingService = shippingService;
        _logger = logger;
    }

    public override string Name => "CreateShipment";

    public override async ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shipment for order {OrderId}", data.OrderId);

        var shipmentId = await _shippingService.CreateShipmentAsync(
            data.OrderId.ToString(),
            data.CustomerId,
            cancellationToken);

        data.ShipmentId = shipmentId;

        _logger.LogInformation("Shipment created with ID {ShipmentId}", shipmentId);
    }

    public override async ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(data.ShipmentId))
        {
            _logger.LogWarning("Cancelling shipment {ShipmentId}", data.ShipmentId);
            await _shippingService.CancelShipmentAsync(data.ShipmentId, cancellationToken);
            _logger.LogInformation("Shipment cancelled");
        }
    }
}
