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
    public void ExplainQueryPlan_BaselineWithoutIndexes_ReflectsTableScan()
    {
        _db.DropIndexes();
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
        // Under unindexed SQLite, baseline plan performs table scan on Orders
        bool containsScan = planNodes.Any(node => node.Contains("SCAN o") || node.Contains("SCAN Orders"));
        Assert.That(containsScan, Is.True, "Baseline plan must reflect unindexed table scan");
    }

    [Test]
    public void ExplainQueryPlan_OptimizedQueryWithCoveringIndexes_UsesCoveringIndexes()
    {
        _db.CreateIndexes();
        string query = @"
            SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
            FROM Products p
            JOIN Orders o ON p.ProductID = o.ProductID
            WHERE p.Category = 'Electronics'
            GROUP BY p.ProductID, p.ProductName
            ORDER BY TotalSold DESC;
        ";

        var planNodes = _db.ExplainQueryPlan(query);

        Assert.That(planNodes, Is.Not.Empty);
        bool usesProductIndex = planNodes.Any(node => node.Contains("idx_products_category_covering"));
        bool usesOrderIndex = planNodes.Any(node => node.Contains("idx_orders_product_qty"));
        bool hasTableScan = planNodes.Any(node => node.StartsWith("SCAN "));

        Assert.That(usesProductIndex, Is.True, "Optimized plan must seek via idx_products_category_covering");
        Assert.That(usesOrderIndex, Is.True, "Optimized plan must seek via idx_orders_product_qty");
        Assert.That(hasTableScan, Is.False, "Optimized plan must eliminate unindexed table scans");
    }

    [Test]
    public void ExecuteOptimizedSalesSummaryQuery_ReturnsIdenticalResultsToBaseline()
    {
        _db.CreateIndexes();
        var baselineResults = OrderService.ExecuteSalesSummaryQuery(_db);
        var optimizedResults = OrderService.ExecuteOptimizedSalesSummaryQuery(_db);

        Assert.That(optimizedResults, Is.Not.Null);
        Assert.That(optimizedResults.Count, Is.EqualTo(baselineResults.Count), "Row counts must match exactly");
        Assert.That(optimizedResults.Count, Is.GreaterThan(0));

        // Verify dictionary / multi-set equivalence across all products
        var baseDict = baselineResults.ToDictionary(r => r.ProductName, r => r.TotalSold);
        var optDict = optimizedResults.ToDictionary(r => r.ProductName, r => r.TotalSold);
        Assert.That(optDict, Is.EqualTo(baseDict), "Aggregated sales quantities must match exactly per product");

        // Verify monotonic descending sort on TotalSold in optimized results
        for (int i = 0; i < optimizedResults.Count - 1; i++)
        {
            Assert.That(optimizedResults[i].TotalSold, Is.GreaterThanOrEqualTo(optimizedResults[i + 1].TotalSold),
                $"Optimized output must remain sorted descending at index {i}");
        }
    }

    [Test]
    public void BenchmarkQueries_AchievesSignificantLatencyReduction()
    {
        using var largeDb = new WarehouseDbContext("Data Source=:memory:");
        largeDb.SeedData(productCount: 500, orderCount: 5000, randomSeed: 777);
        largeDb.CreateIndexes();

        var (baselineMs, optMs, reduction) = OrderService.BenchmarkQueries(largeDb, iterations: 25);

        Assert.That(baselineMs, Is.GreaterThan(0));
        Assert.That(optMs, Is.GreaterThan(0));
        Assert.That(optMs, Is.LessThan(baselineMs), "Optimized query latency must be lower than baseline");
        Assert.That(reduction, Is.GreaterThanOrEqualTo(15.0), "Latency reduction must show substantial improvement");
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
