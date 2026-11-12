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
