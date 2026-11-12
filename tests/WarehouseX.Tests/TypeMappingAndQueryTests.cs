using System.Linq;
using NUnit.Framework;
using WarehouseX.Data;
using WarehouseX.Models;
using WarehouseX.Services;

namespace WarehouseX.Tests;

[TestFixture]
public class TypeMappingAndQueryTests
{
    private WarehouseDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new WarehouseDbContext("Data Source=:memory:");
        _db.SeedData(productCount: 50, orderCount: 200, randomSeed: 999);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public void ProductSet_Find_MapsSqlColumnsToEntityMembers()
    {
        var product = _db.Products.Find(1);

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.Id, Is.EqualTo(1));
        Assert.That(product.Name, Does.StartWith("Product-"));
        Assert.That(product.Category, Is.Not.Empty);
        Assert.That(product.Stock, Is.GreaterThan(0));
    }

    [Test]
    public void ProductSet_FirstOrDefault_ResolvesViaPredicate()
    {
        var product = _db.Products.FirstOrDefault(p => p.Id == 5);

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.Id, Is.EqualTo(5));
    }

    [Test]
    public void OrderSet_ToList_ReturnsMappedOrderEntities()
    {
        var orders = _db.Orders.ToList();

        Assert.That(orders.Count, Is.EqualTo(200));
        var first = orders[0];
        Assert.That(first.Id, Is.GreaterThan(0));
        Assert.That(first.ProductId, Is.InRange(1, 50));
        Assert.That(first.Quantity, Is.InRange(1, 10));
    }

    [Test]
    public void ExecuteSalesSummaryQuery_ReturnsAggregatedElectronicsSales()
    {
        var sales = OrderService.ExecuteSalesSummaryQuery(_db);

        Assert.That(sales, Is.Not.Null);
        Assert.That(sales.Count, Is.GreaterThan(0));

        foreach (var item in sales)
        {
            Assert.That(item.ProductName, Does.StartWith("Product-"));
            Assert.That(item.TotalSold, Is.GreaterThan(0));
        }

        // Verify ordering: descending by TotalSold
        for (int i = 0; i < sales.Count - 1; i++)
        {
            Assert.That(sales[i].TotalSold, Is.GreaterThanOrEqualTo(sales[i + 1].TotalSold));
        }
    }

    [Test]
    public void ExplainQueryPlan_OutputsPlannerExecutionNodes()
    {
        string query = @"
            SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
            FROM Orders o
            JOIN Products p ON o.ProductID = p.ProductID
            WHERE p.Category = 'Electronics'
            GROUP BY p.ProductName
            ORDER BY TotalSold DESC;
        ";

        var planNodes = _db.ExplainQueryPlan(query);

        Assert.That(planNodes, Is.Not.Empty);
        // Under unindexed SQLite, baseline plan performs table scans
        bool containsScan = planNodes.Any(node => node.Contains("SCAN"));
        Assert.That(containsScan, Is.True, "Baseline plan should reflect unindexed table scan");
    }

    [Test]
    public void ProcessOrdersLoopUnoptimized_ExecutesVerbatimActivity3Logic()
    {
        var orders = _db.Orders.Take(10).ToList();
        var loggedLines = new List<string>();

        _db.QueryCounter = 0;
        OrderService.ProcessOrdersLoopUnoptimized(_db, orders, line => loggedLines.Add(line));

        Assert.That(loggedLines.Count, Is.EqualTo(10));
        Assert.That(loggedLines[0], Does.StartWith("Order 1: Product-"));
        // Each loop iteration queried Products (demonstrates N+1 queries)
        Assert.That(_db.QueryCounter, Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    public void ProcessOrderUnoptimized_ExecutesVerbatimActivity4Logic()
    {
        var validOrder = new Order(1, 1, 5);
        Assert.DoesNotThrow(() => OrderService.ProcessOrderUnoptimized(_db, validOrder));
    }

    [Test]
    public void ProcessOrderUnoptimized_ThrowsNullReferenceException_OnMissingProduct()
    {
        // Product 9999 does not exist in catalog; reproduces course baseline defect
        var missingProductOrder = new Order(2, 9999, 1);
        Assert.Throws<NullReferenceException>(() => OrderService.ProcessOrderUnoptimized(_db, missingProductOrder));
    }
}
