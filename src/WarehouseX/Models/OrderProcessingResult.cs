namespace WarehouseX.Models;

/// <summary>
/// Structured result model for order processing operations.
/// Encapsulates execution status, diagnostic messages, and error codes without crashing the host process.
/// </summary>
public class OrderProcessingResult
{
    public bool Success { get; }
    public int OrderId { get; }
    public int ProductId { get; }
    public int Quantity { get; }
    public int RemainingStock { get; }
    public string Message { get; }
    public string? ErrorCode { get; }

    private OrderProcessingResult(bool success, int orderId, int productId, int quantity, int remainingStock, string message, string? errorCode = null)
    {
        Success = success;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        RemainingStock = remainingStock;
        Message = message;
        ErrorCode = errorCode;
    }

    public static OrderProcessingResult Completed(int orderId, int productId, int quantity, int remainingStock)
    {
        return new OrderProcessingResult(true, orderId, productId, quantity, remainingStock, $"Order {orderId} processed successfully. Remaining stock: {remainingStock}.");
    }

    public static OrderProcessingResult Failed(int orderId, int productId, int quantity, string errorCode, string message)
    {
        return new OrderProcessingResult(false, orderId, productId, quantity, 0, message, errorCode);
    }
}
