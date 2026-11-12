-- Revised Optimized SQL Query (Course 9 Capstone Submission Item S2 / Criterion C2)
-- Optimization Techniques:
-- 1. Driving Table Inversion: Filters Products on Category = 'Electronics' first, drastically reducing join cardinality.
-- 2. Covering Indexes: Leverages idx_products_category_covering and idx_orders_product_qty to eliminate all table heap access.
-- 3. Grouping Disambiguation: Groups on (ProductID, ProductName) ensuring robust aggregation without changing result semantics.
SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
FROM Products p
JOIN Orders o ON p.ProductID = o.ProductID
WHERE p.Category = 'Electronics'
GROUP BY p.ProductID, p.ProductName
ORDER BY TotalSold DESC;
