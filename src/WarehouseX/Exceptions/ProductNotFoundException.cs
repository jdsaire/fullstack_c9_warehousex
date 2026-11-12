namespace WarehouseX.Exceptions;

/// <summary>
/// Thrown when an order references a ProductId that does not exist in the warehouse catalog.
/// Resolves null reference vulnerabilities (Criterion C4 / Issue E1 / Guard F1).
/// </summary>
public class ProductNotFoundException : WarehouseException
{
    public int ProductId { get; }

    public ProductNotFoundException(int productId)
        : base($"Product with ID {productId} was not found in the warehouse catalog.")
    {
        ProductId = productId;
    }
}
