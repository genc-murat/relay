using Relay.MessageBroker.Saga;
using SagaPatternSample.Services;

namespace SagaPatternSample.Sagas.Steps;

public class ReserveInventoryStep : SagaStep<OrderSagaData>
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<ReserveInventoryStep> _logger;

    public ReserveInventoryStep(
        InventoryService inventoryService,
        ILogger<ReserveInventoryStep> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public override string Name => "ReserveInventory";

    public override async ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reserving inventory for order {OrderId}", data.OrderId);

        var reservationId = await _inventoryService.ReserveAsync(
            data.Items.Select(i => (i.ProductId, i.Quantity)).ToList(),
            cancellationToken);

        data.ReservationId = reservationId;

        _logger.LogInformation("Inventory reserved with ID {ReservationId}", reservationId);
    }

    public override async ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.ReservationId.HasValue)
        {
            _logger.LogWarning("Cancelling inventory reservation {ReservationId}", data.ReservationId);
            await _inventoryService.CancelReservationAsync(data.ReservationId.Value, cancellationToken);
            _logger.LogInformation("Inventory reservation cancelled");
        }
    }
}
