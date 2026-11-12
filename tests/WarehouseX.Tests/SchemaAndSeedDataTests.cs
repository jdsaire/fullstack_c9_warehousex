using NUnit.Framework;
using WarehouseX.Data;

namespace WarehouseX.Tests;

[TestFixture]
public class SchemaAndSeedDataTests
{
    private WarehouseDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new WarehouseDbContext("Data Source=:memory:");
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public void InitializeSchema_CreatesRequiredTables()
    {
        _db.InitializeSchema();

        var conn = _db.GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";

        var tables = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.That(tables, Does.Contain("Products"));
        Assert.That(tables, Does.Contain("Orders"));
    }

    [Test]
    public void SeedData_SmallWorkload_GeneratesExpectedRowCounts()
    {
        const int productCount = 100;
        const int orderCount = 1000;

        _db.SeedData(productCount, orderCount, randomSeed: 12345);

        var conn = _db.GetOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM Products;";
        long actualProducts = (long)cmd.ExecuteScalar()!;

        cmd.CommandText = "SELECT COUNT(*) FROM Orders;";
        long actualOrders = (long)cmd.ExecuteScalar()!;

        Assert.That(actualProducts, Is.EqualTo(productCount));
        Assert.That(actualOrders, Is.EqualTo(orderCount));
    }

    [Test]
    public void SeedData_MediumWorkload_GeneratesExpectedRowCounts()
    {
        const int productCount = 500;
        const int orderCount = 10000;

        _db.SeedData(productCount, orderCount, randomSeed: 54321);

        var conn = _db.GetOpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM Products;";
        long actualProducts = (long)cmd.ExecuteScalar()!;

        cmd.CommandText = "SELECT COUNT(*) FROM Orders;";
        long actualOrders = (long)cmd.ExecuteScalar()!;

        Assert.That(actualProducts, Is.EqualTo(productCount));
        Assert.That(actualOrders, Is.EqualTo(orderCount));
    }

    [Test]
    public void SeedData_ProductProperties_ConformToBounds()
    {
        _db.SeedData(productCount: 50, orderCount: 100);

        var conn = _db.GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MIN(Stock), MAX(Stock), COUNT(DISTINCT Category) FROM Products;";

        using var reader = cmd.ExecuteReader();
        Assert.That(reader.Read(), Is.True);
        int minStock = reader.GetInt32(0);
        int maxStock = reader.GetInt32(1);
        int categoryCount = reader.GetInt32(2);

        Assert.That(minStock, Is.GreaterThanOrEqualTo(20));
        Assert.That(maxStock, Is.LessThanOrEqualTo(500));
        Assert.That(categoryCount, Is.EqualTo(5));
    }
}
