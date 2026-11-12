using System;
using System.Collections.Generic;
using NUnit.Framework;
using WarehouseX.Data;
using WarehouseX.Exceptions;
using WarehouseX.Models;
using WarehouseX.Services;

namespace WarehouseX.Tests;

[TestFixture]
public class ResilienceAndDebuggingTests
{
    private WarehouseDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new WarehouseDbContext("Data Source=:memory:");
        _db.SeedData(productCount: 50, orderCount: 200, randomSeed: 12345);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    #region Baseline Defect Reproduction Tests (Criterion C4 / Activity 4 Step 2)

    [Test]
    public void Baseline_ProcessOrderUnoptimized_ThrowsNullReferenceException_OnMissingProduct()
    {
        // Product 9999 is absent from catalog (Issue E1 / Bullet F1)
        var missingProductOrder = new Order(101, 9999, 2);

        var ex = Assert.Throws<NullReferenceException>(() =>
            OrderService.ProcessOrderUnoptimized(_db, missingProductOrder));

        Assert.That(ex.Message, Does.Contain("Product was null"));
    }

    [Test]
    public void Baseline_ProcessOrderUnoptimized_AllowsNegativeStock_OnOverdraft()
    {
        // Product 1 has stock >= 20. Order requests 10,000 items (Issue E3 / Bullet F2)
        var product = _db.Products.Find(1);
        Assert.That(product, Is.Not.Null);
        int initialStock = product!.Stock;

        var overdraftOrder = new Order(102, 1, initialStock + 500);

        // Baseline execution does not validate stock and permits negative inventory
        OrderService.ProcessOrderUnoptimized(_db, overdraftOrder);

        var persisted = _db.Products.Find(1);
        Assert.That(persisted!.Stock, Is.LessThan(0), "Baseline permits inventory stock to plunge into negative balances");
        Assert.That(persisted.Stock, Is.EqualTo(initialStock - (initialStock + 500)));
    }

    #endregion

    #region Debugged Guard Clause Verification (Criterion C4 / Activity 4 Step 3)

    [Test]
    public void ProcessOrderDebugged_ThrowsArgumentNullException_WhenOrderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrderService.ProcessOrderDebugged(_db, null!));
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-50)]
    public void ProcessOrderDebugged_ThrowsInvalidOrderQuantityException_WhenQuantityIsZeroOrNegative(int invalidQuantity)
    {
        var invalidOrder = new Order(201, 1, invalidQuantity);

        var ex = Assert.Throws<InvalidOrderQuantityException>(() =>
            OrderService.ProcessOrderDebugged(_db, invalidOrder));

        Assert.That(ex.OrderId, Is.EqualTo(201));
        Assert.That(ex.Quantity, Is.EqualTo(invalidQuantity));
        Assert.That(ex.Message, Does.Contain("must be greater than zero"));
    }

    [Test]
    public void ProcessOrderDebugged_ThrowsProductNotFoundException_WhenProductDoesNotExist()
    {
        var missingProductOrder = new Order(202, 8888, 5);

        var ex = Assert.Throws<ProductNotFoundException>(() =>
            OrderService.ProcessOrderDebugged(_db, missingProductOrder));

        Assert.That(ex.ProductId, Is.EqualTo(8888));
        Assert.That(ex.Message, Does.Contain("was not found in the warehouse catalog"));
    }

    [Test]
    public void ProcessOrderDebugged_ThrowsInsufficientStockException_WhenRequestedExceedsAvailable()
    {
        var product = _db.Products.Find(2);
        Assert.That(product, Is.Not.Null);
        int currentStock = product!.Stock;

        var excessiveOrder = new Order(203, 2, currentStock + 25);

        var ex = Assert.Throws<InsufficientStockException>(() =>
            OrderService.ProcessOrderDebugged(_db, excessiveOrder));

        Assert.That(ex.ProductId, Is.EqualTo(2));
        Assert.That(ex.RequestedQuantity, Is.EqualTo(currentStock + 25));
        Assert.That(ex.AvailableStock, Is.EqualTo(currentStock));

        // Confirm database stock remains unmodified
        var reloaded = _db.Products.Find(2);
        Assert.That(reloaded!.Stock, Is.EqualTo(currentStock), "Stock must remain unchanged after rejected overdraft");
    }

    [Test]
    public void ProcessOrderDebugged_DeductsStockAndPersists_WhenOrderIsValid()
    {
        var product = _db.Products.Find(3);
        Assert.That(product, Is.Not.Null);
        int initialStock = product!.Stock;
        int orderQty = 5;
        Assert.That(initialStock, Is.GreaterThan(orderQty));

        var validOrder = new Order(204, 3, orderQty);
        var logs = new List<string>();

        OrderService.ProcessOrderDebugged(_db, validOrder, line => logs.Add(line));

        Assert.That(logs.Count, Is.EqualTo(1));
        Assert.That(logs[0], Is.EqualTo("Order 204 processed."));

        // Verify database persistence via fresh query
        var persisted = _db.Products.Find(3);
        Assert.That(persisted, Is.Not.Null);
        Assert.That(persisted!.Stock, Is.EqualTo(initialStock - orderQty));
    }

    #endregion

    #region Resilient Wrapper & Exception Handling (Criterion C4 / Strategy ST2)

    [Test]
    public void TryProcessOrder_HandlesNullOrderGracefully_WithoutCrashing()
    {
        var result = OrderService.TryProcessOrder(_db, null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("NULL_ORDER"));
        Assert.That(result.Message, Does.Contain("Order payload cannot be null"));
    }

    [Test]
    public void TryProcessOrder_HandlesInvalidQuantityGracefully_WithoutCrashing()
    {
        var invalidOrder = new Order(301, 1, -5);
        var result = OrderService.TryProcessOrder(_db, invalidOrder);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_QUANTITY"));
        Assert.That(result.OrderId, Is.EqualTo(301));
        Assert.That(result.Message, Does.Contain("must be greater than zero"));
    }

    [Test]
    public void TryProcessOrder_HandlesMissingProductGracefully_WithoutCrashing()
    {
        var missingProductOrder = new Order(302, 7777, 4);
        var result = OrderService.TryProcessOrder(_db, missingProductOrder);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("PRODUCT_NOT_FOUND"));
        Assert.That(result.ProductId, Is.EqualTo(7777));
        Assert.That(result.Message, Does.Contain("not found"));
    }

    [Test]
    public void TryProcessOrder_HandlesInsufficientStockGracefully_WithoutCrashing()
    {
        var product = _db.Products.Find(4);
        Assert.That(product, Is.Not.Null);

        var excessiveOrder = new Order(303, 4, product!.Stock + 100);
        var result = OrderService.TryProcessOrder(_db, excessiveOrder);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INSUFFICIENT_STOCK"));
        Assert.That(result.ProductId, Is.EqualTo(4));
        Assert.That(result.Message, Does.Contain("Insufficient stock"));
    }

    [Test]
    public void TryProcessOrder_ReturnsCompletedResult_OnValidOrder()
    {
        var product = _db.Products.Find(5);
        Assert.That(product, Is.Not.Null);
        int initialStock = product!.Stock;

        var validOrder = new Order(304, 5, 2);
        var result = OrderService.TryProcessOrder(_db, validOrder);

        Assert.That(result.Success, Is.True);
        Assert.That(result.OrderId, Is.EqualTo(304));
        Assert.That(result.RemainingStock, Is.EqualTo(initialStock - 2));
        Assert.That(result.Message, Does.Contain("processed successfully"));
    }

    [Test]
    public void ProcessOrdersBatchResilient_EliminatesProcessCrashes_AcrossHighVolumeHeterogeneousWorkload()
    {
        // Construct 1,000 heterogeneous order workload:
        // - 850 valid orders
        // - 75 non-existent product IDs
        // - 50 excessive quantity requests (overdrafts)
        // - 25 non-positive quantities
        var workload = new List<Order>();
        int orderIdCounter = 1000;

        for (int i = 0; i < 850; i++)
        {
            workload.Add(new Order(orderIdCounter++, (i % 50) + 1, 1)); // Valid order
        }
        for (int i = 0; i < 75; i++)
        {
            workload.Add(new Order(orderIdCounter++, 9000 + i, 2)); // Missing product
        }
        for (int i = 0; i < 50; i++)
        {
            workload.Add(new Order(orderIdCounter++, (i % 50) + 1, 99999)); // Overdraft
        }
        for (int i = 0; i < 25; i++)
        {
            workload.Add(new Order(orderIdCounter++, (i % 50) + 1, -(i % 5))); // Non-positive quantity
        }

        // Shuffle workload deterministically
        var rng = new Random(42);
        for (int i = workload.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (workload[i], workload[j]) = (workload[j], workload[i]);
        }

        // Under baseline unoptimized implementation, processing would abort at first invalid item
        // Under resilient implementation, all 1,000 orders execute without unhandled crash
        var (total, succeeded, failed, results) = OrderService.ProcessOrdersBatchResilient(_db, workload);

        Assert.That(total, Is.EqualTo(1000));
        Assert.That(succeeded, Is.GreaterThanOrEqualTo(800));
        Assert.That(failed, Is.EqualTo(total - succeeded));
        Assert.That(results.Count, Is.EqualTo(1000));

        // Verify zero unhandled exceptions (M-DBG-01 Target: 0 crashes)
        foreach (var res in results)
        {
            Assert.That(res.Message, Is.Not.Null.And.Not.Empty);
            if (!res.Success)
            {
                Assert.That(res.ErrorCode, Is.AnyOf("NULL_ORDER", "INVALID_QUANTITY", "PRODUCT_NOT_FOUND", "INSUFFICIENT_STOCK", "DOMAIN_ERROR", "UNEXPECTED_ERROR"));
            }
        }
    }

    #endregion
}
