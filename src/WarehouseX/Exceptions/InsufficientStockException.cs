namespace WarehouseX.Exceptions;

/// <summary>
/// Thrown when an order requests a quantity exceeding available warehouse inventory.
/// Prevents negative stock balances and inventory discrepancies (Criterion C4 / Issue E3 / Guard F2).
/// </summary>
public class InsufficientStockException : WarehouseException
{
    public int ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }

    public InsufficientStockException(int productId, int requestedQuantity, int availableStock)
        : base($"Insufficient stock for product ID {productId}. Requested: {requestedQuantity}, Available: {availableStock}.")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
    }
}
