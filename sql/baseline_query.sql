-- Baseline Unoptimized Query (Course Activity 2 Step 2)
-- Inefficiencies: Unindexed table scan on Orders, post-join Category filter, unindexed grouping on ProductName.
SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
FROM Orders o
JOIN Products p ON o.ProductID = p.ProductID
WHERE p.Category = 'Electronics'
GROUP BY p.ProductName
ORDER BY TotalSold DESC;
