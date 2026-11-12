namespace WarehouseX.Exceptions;

/// <summary>
/// Thrown when an order specifies a non-positive quantity (zero or negative).
/// Validates business boundaries for order placement (Criterion C4 / Strategy ST3).
/// </summary>
public class InvalidOrderQuantityException : WarehouseException
{
    public int OrderId { get; }
    public int Quantity { get; }

    public InvalidOrderQuantityException(int orderId, int quantity)
        : base($"Invalid quantity {quantity} for order ID {orderId}. Order quantity must be greater than zero.")
    {
        OrderId = orderId;
        Quantity = quantity;
    }
}
