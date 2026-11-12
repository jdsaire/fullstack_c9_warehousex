namespace WarehouseX.Models;

/// <summary>
/// Product catalog entity representing inventory items.
/// Maps to SQL table Products with columns (ProductID, ProductName, Category, Stock).
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }

    public Product() { }

    public Product(int id, string name, string category, int stock)
    {
        Id = id;
        Name = name;
        Category = category;
        Stock = stock;
    }
}
