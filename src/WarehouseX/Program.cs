using System;
using WarehouseX.Data;
using WarehouseX.Services;

namespace WarehouseX;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("WarehouseX · Performance Optimization Scaffold");
        Console.WriteLine("==================================================");

        using var db = new WarehouseDbContext();
        Console.WriteLine("Initializing SQLite schema and seeding synthetic data (Small dataset)...");
        db.SeedData(productCount: 100, orderCount: 1000);
        Console.WriteLine("Schema initialized and seeded successfully.");

        Console.WriteLine("\nExecuting baseline sales query...");
        var sales = OrderService.ExecuteSalesSummaryQuery(db);
        Console.WriteLine($"Retrieved {sales.Count} aggregate sales records.");

        Console.WriteLine("\nCapturing EXPLAIN QUERY PLAN for baseline query:");
        string query = @"
            SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
            FROM Orders o
            JOIN Products p ON o.ProductID = p.ProductID
            WHERE p.Category = 'Electronics'
            GROUP BY p.ProductName
            ORDER BY TotalSold DESC;
        ";
        var plan = db.ExplainQueryPlan(query);
        foreach (var step in plan)
        {
            Console.WriteLine($"  - {step}");
        }

        Console.WriteLine("\nScaffold initialization complete.");
    }
}
