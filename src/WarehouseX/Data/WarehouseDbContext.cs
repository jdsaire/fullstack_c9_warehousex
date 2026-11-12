using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using WarehouseX.Models;

namespace WarehouseX.Data;

/// <summary>
/// Lightweight ORM-style data context for WarehouseX using SQLite.
/// Exposes Products and Orders datasets with explicit column-to-member mapping.
/// Supports native execution plan inspection (EXPLAIN QUERY PLAN).
/// </summary>
public class WarehouseDbContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed;

    public ProductSet Products { get; }
    public OrderSet Orders { get; }

    public int QueryCounter { get; set; }

    public WarehouseDbContext(string connectionString = "Data Source=:memory:")
    {
        _connectionString = connectionString;
        Products = new ProductSet(this);
        Orders = new OrderSet(this);
    }

    public SqliteConnection GetOpenConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        else if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
        return _connection;
    }

    public void InitializeSchema()
    {
        var conn = GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Products (
                ProductID INTEGER PRIMARY KEY,
                ProductName TEXT NOT NULL,
                Category TEXT NOT NULL,
                Stock INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductID INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void CreateIndexes()
    {
        var conn = GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_products_category_covering ON Products(Category, ProductID, ProductName);
            CREATE INDEX IF NOT EXISTS idx_orders_product_qty ON Orders(ProductID, Quantity);
        ";
        cmd.ExecuteNonQuery();
    }

    public void DropIndexes()
    {
        var conn = GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DROP INDEX IF EXISTS idx_products_category;
            DROP INDEX IF EXISTS idx_products_category_covering;
            DROP INDEX IF EXISTS idx_orders_product_qty;
        ";
        cmd.ExecuteNonQuery();
    }

    public void SeedData(int productCount = 100, int orderCount = 1000, int randomSeed = 42)
    {
        InitializeSchema();
        var conn = GetOpenConnection();
        using var transaction = conn.BeginTransaction();

        // Clear existing data
        using (var clearCmd = conn.CreateCommand())
        {
            clearCmd.Transaction = transaction;
            clearCmd.CommandText = "DELETE FROM Orders; DELETE FROM Products;";
            clearCmd.ExecuteNonQuery();
        }

        var rng = new Random(randomSeed);
        string[] categories = { "Electronics", "Apparel", "HomeGoods", "Industrial", "Books" };

        // Seed Products
        using (var prodCmd = conn.CreateCommand())
        {
            prodCmd.Transaction = transaction;
            prodCmd.CommandText = @"
                INSERT INTO Products (ProductID, ProductName, Category, Stock)
                VALUES (@id, @name, @cat, @stock);
            ";
            var pId = prodCmd.Parameters.Add("@id", SqliteType.Integer);
            var pName = prodCmd.Parameters.Add("@name", SqliteType.Text);
            var pCat = prodCmd.Parameters.Add("@cat", SqliteType.Text);
            var pStock = prodCmd.Parameters.Add("@stock", SqliteType.Integer);

            for (int i = 1; i <= productCount; i++)
            {
                pId.Value = i;
                pName.Value = $"Product-{i:D4}";
                pCat.Value = categories[i % categories.Length];
                pStock.Value = rng.Next(20, 500);
                prodCmd.ExecuteNonQuery();
            }
        }

        // Seed Orders
        using (var orderCmd = conn.CreateCommand())
        {
            orderCmd.Transaction = transaction;
            orderCmd.CommandText = @"
                INSERT INTO Orders (ProductID, Quantity)
                VALUES (@prodId, @qty);
            ";
            var pProdId = orderCmd.Parameters.Add("@prodId", SqliteType.Integer);
            var pQty = orderCmd.Parameters.Add("@qty", SqliteType.Integer);

            for (int i = 1; i <= orderCount; i++)
            {
                pProdId.Value = rng.Next(1, productCount + 1);
                pQty.Value = rng.Next(1, 10);
                orderCmd.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    public List<string> ExplainQueryPlan(string sqlQuery)
    {
        var results = new List<string>();
        var conn = GetOpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"EXPLAIN QUERY PLAN {sqlQuery}";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // Column 3 is the explanation detail text
            string detail = reader.GetString(reader.GetOrdinal("detail"));
            results.Add(detail);
        }
        return results;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Product dataset accessor allowing both direct Find(id) queries and in-memory LINQ queries.
    /// </summary>
    public class ProductSet : IEnumerable<Product>
    {
        private readonly WarehouseDbContext _context;

        public ProductSet(WarehouseDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Finds a product by its primary key ID via direct SQL query.
        /// Corresponds to course Activity 4 snippet: db.Products.Find(order.ProductId).
        /// </summary>
        public Product? Find(int productId)
        {
            _context.QueryCounter++;
            var conn = _context.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT ProductID, ProductName, Category, Stock
                FROM Products
                WHERE ProductID = @id;
            ";
            cmd.Parameters.AddWithValue("@id", productId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Product(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)
                );
            }
            return null;
        }

        /// <summary>
        /// Enumerates all products from the database for LINQ compatibility.
        /// </summary>
        public IEnumerator<Product> GetEnumerator()
        {
            _context.QueryCounter++;
            var conn = _context.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ProductID, ProductName, Category, Stock FROM Products;";

            using var reader = cmd.ExecuteReader();
            var list = new List<Product>();
            while (reader.Read())
            {
                list.Add(new Product(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)
                ));
            }
            return list.GetEnumerator();
        }

        /// <summary>
        /// Materializes all products into an in-memory dictionary keyed by ProductID in a single query.
        /// Enables O(1) in-memory lookups to eliminate N+1 query loops.
        /// </summary>
        public Dictionary<int, Product> AsDictionary()
        {
            _context.QueryCounter++;
            var conn = _context.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ProductID, ProductName, Category, Stock FROM Products;";

            using var reader = cmd.ExecuteReader();
            var dict = new Dictionary<int, Product>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                dict[id] = new Product(
                    id,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)
                );
            }
            return dict;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Order dataset accessor.
    /// </summary>
    public class OrderSet : IEnumerable<Order>
    {
        private readonly WarehouseDbContext _context;

        public OrderSet(WarehouseDbContext context)
        {
            _context = context;
        }

        public List<Order> ToList()
        {
            _context.QueryCounter++;
            var conn = _context.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ProductID, Quantity FROM Orders ORDER BY Id;";

            using var reader = cmd.ExecuteReader();
            var list = new List<Order>();
            while (reader.Read())
            {
                list.Add(new Order(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2)
                ));
            }
            return list;
        }

        public IEnumerator<Order> GetEnumerator() => ToList().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
