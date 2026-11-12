using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using WarehouseX.Data;
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
    /// Executes a benchmark comparison between the baseline and optimized queries.
    /// Includes warmup iterations to eliminate JIT compilation overhead.
    /// </summary>
    public static (double BaselineMs, double OptimizedMs, double ReductionPercent) BenchmarkQueries(WarehouseDbContext db, int iterations = 50)
    {
        // Warmup JIT and database context
        for (int w = 0; w < 5; w++)
        {
            ExecuteSalesSummaryQuery(db);
            ExecuteOptimizedSalesSummaryQuery(db);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            ExecuteSalesSummaryQuery(db);
        }
        sw.Stop();
        double baselineMs = sw.Elapsed.TotalMilliseconds / iterations;

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
}
