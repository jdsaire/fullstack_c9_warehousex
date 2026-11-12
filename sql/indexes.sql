-- Performance Optimization Covering Indexes for WarehouseX Relational Database
-- Target Engine: SQLite (also portable to PostgreSQL and SQL Server)

-- 1. Composite Covering Index on Products
-- Covers WHERE Category = 'Electronics', JOIN ON ProductID, and SELECT/GROUP BY ProductName
CREATE INDEX IF NOT EXISTS idx_products_category_covering 
ON Products(Category, ProductID, ProductName);

-- 2. Covering Index on Orders
-- Covers JOIN ON ProductID and SUM(Quantity) aggregation entirely within index leaves
CREATE INDEX IF NOT EXISTS idx_orders_product_qty 
ON Orders(ProductID, Quantity);
