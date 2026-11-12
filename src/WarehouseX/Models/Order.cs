namespace WarehouseX.Models;

/// <summary>
/// Customer order entity representing an order line item.
/// Maps to SQL table Orders with columns (Id, ProductID, Quantity).
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Order() { }

    public Order(int id, int productId, int quantity)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
    }
}
