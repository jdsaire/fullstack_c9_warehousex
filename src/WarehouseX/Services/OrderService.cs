using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using WarehouseX.Data;
using WarehouseX.Exceptions;
using WarehouseX.Models;

namespace WarehouseX.Services;

/// <summary>
/// Service harness implementing the base course snippets and benchmark utilities.
/// </summary>
public class OrderService
{
    /// <summary>
    /// Executes the unoptimized SQL query from Activity 2 Step 2.
    /// </summary>
    public static List<(string ProductName, int TotalSold)> ExecuteSalesSummaryQuery(WarehouseDbContext db)
    {
        var results = new List<(string ProductName, int TotalSold)>();
        var conn = db.GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
            FROM Orders o
            JOIN Products p ON o.ProductID = p.ProductID
            WHERE p.Category = 'Electronics'
            GROUP BY p.ProductName
            ORDER BY TotalSold DESC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(0);
            int total = reader.GetInt32(1);
            results.Add((name, total));
        }
        return results;
    }

    /// <summary>
    /// Executes the single revised optimized SQL query (Submission Item S2 / Criterion C2).
    /// Optimization: Inverted driving table (Products -> Orders) and composite covering indexes.
    /// </summary>
    public static List<(string ProductName, int TotalSold)> ExecuteOptimizedSalesSummaryQuery(WarehouseDbContext db)
    {
        var results = new List<(string ProductName, int TotalSold)>();
        var conn = db.GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
            FROM Products p
            JOIN Orders o ON p.ProductID = o.ProductID
            WHERE p.Category = 'Electronics'
            GROUP BY p.ProductID, p.ProductName
            ORDER BY TotalSold DESC;
        ";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(0);
            int total = reader.GetInt32(1);
            results.Add((name, total));
        }
        return results;
    }

    /// <summary>
    /// Executes a benchmark comparison between the unindexed baseline and the covering-index optimized query.
    /// Includes warmup iterations to eliminate JIT compilation overhead.
    /// </summary>
    public static (double BaselineMs, double OptimizedMs, double ReductionPercent) BenchmarkQueries(WarehouseDbContext db, int iterations = 50)
    {
        // 1. Measure unindexed baseline
        db.DropIndexes();
        for (int w = 0; w < 5; w++)
        {
            ExecuteSalesSummaryQuery(db);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            ExecuteSalesSummaryQuery(db);
        }
        sw.Stop();
        double baselineMs = sw.Elapsed.TotalMilliseconds / iterations;

        // 2. Measure optimized query with composite covering indexes
        db.CreateIndexes();
        for (int w = 0; w < 5; w++)
        {
            ExecuteOptimizedSalesSummaryQuery(db);
        }

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            ExecuteOptimizedSalesSummaryQuery(db);
        }
        sw.Stop();
        double optMs = sw.Elapsed.TotalMilliseconds / iterations;

        double reduction = baselineMs > 0 ? ((baselineMs - optMs) / baselineMs) * 100.0 : 0.0;
        return (baselineMs, optMs, reduction);
    }

    /// <summary>
    /// Baseline loop processing reproducing the course snippet from Activity 3 Step 2.
    /// Exhibits the N+1 database query pattern.
    /// </summary>
    public static void ProcessOrdersLoopUnoptimized(WarehouseDbContext db, List<Order> orders, Action<string>? logAction = null)
    {
        foreach (var order in orders)
        {
            var product = db.Products.FirstOrDefault(p => p.Id == order.ProductId);
            logAction?.Invoke($"Order {order.Id}: {product?.Name} - {order.Quantity}");
        }
    }

    /// <summary>
    /// Optimized order processing loop (Submission Item S3 / Criterion C3).
    /// Pre-fetches the product catalog into an in-memory dictionary prior to iteration,
    /// eliminating N+1 database query round-trips (O(N) -> O(1) query complexity).
    /// </summary>
    public static void ProcessOrdersLoopOptimized(WarehouseDbContext db, List<Order> orders, Action<string>? logAction = null)
    {
        var productCatalog = db.Products.AsDictionary();
        foreach (var order in orders)
        {
            productCatalog.TryGetValue(order.ProductId, out var product);
            logAction?.Invoke($"Order {order.Id}: {product?.Name} - {order.Quantity}");
        }
    }

    /// <summary>
    /// Benchmarks the unoptimized N+1 query loop against the dictionary-cached optimized loop.
    /// Includes warmup runs to prevent JIT bias.
    /// </summary>
    public static (double BaselineMs, double OptimizedMs, double ReductionPercent, int BaselineQueries, int OptimizedQueries) BenchmarkLoopProcessing(WarehouseDbContext db, List<Order> orders, int iterations = 5)
    {
        // Warmup runs
        var warmupOrders = orders.Take(Math.Min(orders.Count, 50)).ToList();
        ProcessOrdersLoopUnoptimized(db, warmupOrders, _ => { });
        ProcessOrdersLoopOptimized(db, warmupOrders, _ => { });

        // 1. Measure Baseline
        db.QueryCounter = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            ProcessOrdersLoopUnoptimized(db, orders, _ => { });
        }
        sw.Stop();
        double baselineMs = sw.Elapsed.TotalMilliseconds / iterations;
        int baselineQueries = db.QueryCounter / iterations;

        // 2. Measure Optimized
        db.QueryCounter = 0;
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            ProcessOrdersLoopOptimized(db, orders, _ => { });
        }
        sw.Stop();
        double optMs = sw.Elapsed.TotalMilliseconds / iterations;
        int optQueries = db.QueryCounter / iterations;

        double reduction = baselineMs > 0 ? ((baselineMs - optMs) / baselineMs) * 100.0 : 0.0;
        return (baselineMs, optMs, reduction, baselineQueries, optQueries);
    }

    /// <summary>
    /// Baseline order processing reproducing the non-resilient course snippet from Activity 4 Step 2.
    /// Prone to NullReferenceException and negative stock.
    /// </summary>
    public static void ProcessOrderUnoptimized(WarehouseDbContext db, Order order, Action<string>? logAction = null)
    {
        var product = db.Products.Find(order.ProductId);
        // Direct dereference and deduction without guards (course baseline)
        if (product != null)
        {
            product.Stock -= order.Quantity;
        }
        else
        {
            throw new NullReferenceException("Product was null in unoptimized flow.");
        }
        logAction?.Invoke($"Order {order.Id} processed.");
    }

    /// <summary>
    /// Debugged and resilient order processing function (Submission Item S4 / Capstone Prompt P5 / Rubric Criterion C4).
    /// Implements defensive guard clauses for null checks, stock availability validation, and edge-case handling.
    /// Synchronizes inventory state with persistent storage.
    /// </summary>
    public static void ProcessOrderDebugged(WarehouseDbContext db, Order order, Action<string>? logAction = null)
    {
        // Guard Clause 1: Validate non-null order entity payload
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order), "Order payload cannot be null.");
        }

        // Guard Clause 2: Validate positive order quantity (Edge-case validation R-DBG-2 / ST3)
        if (order.Quantity <= 0)
        {
            throw new InvalidOrderQuantityException(order.Id, order.Quantity);
        }

        // Guard Clause 3: Retrieve and validate product existence (Null check R-DBG-1 / ST1 / F1)
        var product = db.Products.Find(order.ProductId);
        if (product == null)
        {
            throw new ProductNotFoundException(order.ProductId);
        }

        // Guard Clause 4: Validate stock availability against order demand (Stock validation R-DBG-1 / ST1 / F2)
        if (product.Stock < order.Quantity)
        {
            throw new InsufficientStockException(order.ProductId, order.Quantity, product.Stock);
        }

        // Apply inventory deduction and synchronize with database storage
        product.Stock -= order.Quantity;
        db.Products.UpdateStock(product.Id, product.Stock);

        logAction?.Invoke($"Order {order.Id} processed.");
    }

    /// <summary>
    /// Resilient order execution wrapper providing comprehensive exception handling (Criterion C4 / Strategy ST2 / F3).
    /// Intercepts domain and system exceptions, transforming them into structured OrderProcessingResult records
    /// to guarantee host process stability and prevent application crashes.
    /// </summary>
    public static OrderProcessingResult TryProcessOrder(WarehouseDbContext db, Order order, Action<string>? logAction = null)
    {
        if (order == null)
        {
            return OrderProcessingResult.Failed(0, 0, 0, "NULL_ORDER", "Order payload cannot be null.");
        }

        try
        {
            ProcessOrderDebugged(db, order, logAction);
            var product = db.Products.Find(order.ProductId);
            int remainingStock = product?.Stock ?? 0;
            return OrderProcessingResult.Completed(order.Id, order.ProductId, order.Quantity, remainingStock);
        }
        catch (InvalidOrderQuantityException ex)
        {
            return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "INVALID_QUANTITY", ex.Message);
        }
        catch (ProductNotFoundException ex)
        {
            return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "PRODUCT_NOT_FOUND", ex.Message);
        }
        catch (InsufficientStockException ex)
        {
            return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "INSUFFICIENT_STOCK", ex.Message);
        }
        catch (WarehouseException ex)
        {
            return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "DOMAIN_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "UNEXPECTED_ERROR", ex.Message);
        }
    }

    /// <summary>
    /// Processes a batch of orders through the resilient handler, measuring throughput, success, and handled failure counts.
    /// Demonstrates 0 unhandled process crashes across arbitrary workloads.
    /// </summary>
    public static (int Total, int Succeeded, int Failed, List<OrderProcessingResult> Results) ProcessOrdersBatchResilient(
        WarehouseDbContext db,
        List<Order> orders,
        Action<string>? logAction = null)
    {
        var results = new List<OrderProcessingResult>();
        int succeeded = 0;
        int failed = 0;

        foreach (var order in orders)
        {
            var res = TryProcessOrder(db, order, logAction);
            results.Add(res);
            if (res.Success)
            {
                succeeded++;
            }
            else
            {
                failed++;
            }
        }

        return (orders.Count, succeeded, failed, results);
    }
}
