# Activity 2 Completion Report · WarehouseX

| Document Metadata | Value |
|---|---|
| **Activity ID** | Course 9 Capstone · Activity 2 (C9-ACT2) |
| **Title** | Query Optimization with SQL Execution Plans |
| **Author** | Juan Diego S. (`88201583+jdsaire@users.noreply.github.com`) |
| **Specialization** | Microsoft Full Stack Developer Specialization |
| **Course** | Course 9: Performance Optimization and Scalability |
| **Status** | Completed on branch `a2-sql` |
| **Target Branch** | `a2-sql` |
| **Date** | November 2026 |

---

## 1. Executive Summary

Activity 2 of the WarehouseX Capstone addresses database query latency, unindexed table scans, and high lock contention on product and order datasets. 

The primary objective of Activity 2 was to analyze the course-supplied unoptimized sales query (Activity 2 Step 2) using native SQLite execution plans (`EXPLAIN QUERY PLAN`), identify operational bottlenecks, design and implement targeted composite covering indexes, formulate a single revised SQL query (Submission Item S2 / Rubric Criterion C2), and verify 100% mathematical and semantic result equivalence through automated NUnit test fixtures.

### Milestone Deliverables Achieved
- **Single Revised SQL Query (`sql/optimized_query.sql`)**: Formulated the optimized query (Submission Item S2 / Prompt P3) inverting the driving table so that the filtered `Products` table leads the join and grouping on `(ProductID, ProductName)` to guarantee robust aggregation without table reads.
- **Composite Covering Indexes (`sql/indexes.sql`)**: Implemented `idx_products_category_covering` on `Products(Category, ProductID, ProductName)` and `idx_orders_product_qty` on `Orders(ProductID, Quantity)`, eliminating all table heap page access.
- **Execution Plan Analysis (`sql/explain_query_plans.sql`)**: Captured and documented SQLite execution plan transformations: replaced the unindexed `SCAN Orders` full table scan and row-by-row `SEARCH Products (rowid)` with dual covering index seeks (`SEARCH Products USING COVERING INDEX` and `SEARCH Orders USING COVERING INDEX`).
- **Automated Verification Suite (`tests/WarehouseX.Tests/`)**: Expanded NUnit fixtures (`15/15` tests passing) asserting execution plan index utilization, zero table scans, 100% dictionary/multiset equivalence with the baseline query, and latency reduction on simulated workloads.
- **Interactive Demonstration Page (`activity2-sql.html`)**: Built an interactive demonstration interface adhering to WCAG 2.1 AAA accessible dark theme standards, featuring side-by-side SQL diffs, query plan visualizer, result equivalence matrix, and client-side benchmark runner.
- **Evidence Ledger & AI Assistance Record (`docs/evidence/`)**: Updated `LEDGER.json` entries `M-SQL-01` (latency reduced from 1.82 ms to 0.64 ms, a -64.8% reduction) and `M-SQL-02` (table scans eliminated), and appended entry `AI-03` to `AI-ASSISTANCE-RECORD.md`.

---

## 2. Course 9 Capstone Rubric Alignment

The table below quotes the Course 9 Capstone rubric criteria from the project brief and records how Activity 2 satisfies the requirements:

| Criterion | Points | Question | Activity 2 Scope & Repository Evidence |
|---|---|---|---|
| **C1** | 5 | Did you develop a strategic plan for performance optimization? | Completed in Activity 1 ([`docs/STRATEGIC-PLAN.md`](../STRATEGIC-PLAN.md)). Activity 2 executes Phase P2/P4 of the plan specifically addressing requirement `R-SQL-1..4` and problem area `I1`. |
| **C2** | 5 | Did you submit a single revised SQL query reflecting optimization? | Completed. Formulated the single revised SQL query in [`sql/optimized_query.sql`](../../sql/optimized_query.sql) (Submission Item S2 / Prompt P3). Combines driving-table inversion with composite covering indexes, verified in [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs) and interactive demo [`activity2-sql.html`](../../activity2-sql.html) (`[M]`). |
| **C3** | 5 | Did you optimize the application code to enhance performance? | Out of scope for Activity 2. Scheduled for Activity 3; baseline $N+1$ loop verification maintained in [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs). |
| **C4** | 5 | Did you debug the application and resolve runtime errors? | Out of scope for Activity 2. Scheduled for Activity 4; baseline null exception reproduction maintained in [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs). |
| **C5** | 5 | Did you include a reflective summary explaining how the AI assistant assisted in each step? | Assistance for Activity 2 logged in [`docs/evidence/AI-ASSISTANCE-RECORD.md`](../evidence/AI-ASSISTANCE-RECORD.md) (Entry AI-03); full capstone reflective summary (Item S5 / Prompt P6) will be compiled at Activity 4. |

---

## 3. Query Analysis & Optimization Architecture

### Baseline Unoptimized Query (Activity 2 Step 2)
The starting query provided in the course instructions:

```sql
SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
FROM Orders o
JOIN Products p ON o.ProductID = p.ProductID
WHERE p.Category = 'Electronics'
GROUP BY p.ProductName
ORDER BY TotalSold DESC;
```

#### Inefficiencies Identified:
1. **Full Table Scan on Orders (`SCAN o`)**: Without an index on `Orders.ProductID`, the database engine scans every row in the `Orders` table heap, reading all disk/memory pages.
2. **Post-Join Filter Evaluation**: The `Category = 'Electronics'` predicate is evaluated after joining `Orders` and `Products`, processing order rows for irrelevant product categories.
3. **Repeated Primary Key Searches**: For every order row scanned, SQLite performs a point search on `Products` (`SEARCH p USING INTEGER PRIMARY KEY (rowid=?)`).
4. **Temporary B-Tree Overhead**: Grouping by the unindexed text column `p.ProductName` and ordering by computed aggregate `TotalSold DESC` forces the engine to materialize two temporary in-memory B-trees (`USE TEMP B-TREE FOR GROUP BY` and `USE TEMP B-TREE FOR ORDER BY`).

---

### Composite Covering Indexes
To address these bottlenecks, two covering indexes were designed:

```sql
-- 1. Composite Covering Index on Products
CREATE INDEX IF NOT EXISTS idx_products_category_covering 
ON Products(Category, ProductID, ProductName);

-- 2. Covering Index on Orders
CREATE INDEX IF NOT EXISTS idx_orders_product_qty 
ON Orders(ProductID, Quantity);
```

#### Mechanism:
- **Products Index (`Category, ProductID, ProductName`)**: Allows the database engine to seek directly to `Category = 'Electronics'`, extract `ProductID` for the join, and project `ProductName` without visiting the table data pages.
- **Orders Index (`ProductID, Quantity`)**: Replaces the full table scan with an indexed seek on `ProductID` and satisfies the `SUM(Quantity)` aggregation entirely from index leaf nodes.
- **Index-Only Execution**: The query is resolved 100% from index structures with zero heap page I/O.

---

### Single Revised SQL Query (Submission Item S2)
To maximize the efficiency of the covering indexes, the query was restructured:

```sql
SELECT p.ProductName, SUM(o.Quantity) AS TotalSold
FROM Products p
JOIN Orders o ON p.ProductID = o.ProductID
WHERE p.Category = 'Electronics'
GROUP BY p.ProductID, p.ProductName
ORDER BY TotalSold DESC;
```

#### Restructuring Improvements:
1. **Driving Table Inversion**: By placing `Products` first (`FROM Products p JOIN Orders o`), SQLite utilizes `idx_products_category_covering` as the driving scan. Only catalog items matching `'Electronics'` are sought.
2. **Join Cardinality Reduction**: In a warehouse with 500 catalog items and 10,000 orders where 20% of products are Electronics, the join loop executes only for matching product IDs rather than all 10,000 orders.
3. **Grouping Disambiguation**: Grouping by `(p.ProductID, p.ProductName)` ensures proper aggregation semantics while remaining 100% equivalent to the baseline query.

---

## 4. Execution Plan Comparative Verification

### Measured SQLite `EXPLAIN QUERY PLAN` Comparison

```
Baseline Plan (Unindexed):
  |-- SCAN o
  |-- SEARCH p USING INTEGER PRIMARY KEY (rowid=?)
  |-- USE TEMP B-TREE FOR GROUP BY
  `-- USE TEMP B-TREE FOR ORDER BY

Optimized Plan (Covering Indexes + Restructured Query):
  |-- SEARCH p USING COVERING INDEX idx_products_category_covering (Category=?)
  |-- SEARCH o USING COVERING INDEX idx_orders_product_qty (ProductID=?)
  |-- USE TEMP B-TREE FOR GROUP BY
  `-- USE TEMP B-TREE FOR ORDER BY
```

#### Comparative Plan Node Summary:
- **Full Table Scans**: Reduced from 1 (`SCAN o`) to 0 (`[M]`).
- **Table Heap Reads**: Eliminated; both join inputs use `COVERING INDEX` seeks (`[M]`).
- **Join Search Complexity**: O(N) heap scan &rarr; O(M log K) index seeks, where M is category items and K is order records.

---

## 5. Result Equivalence & Performance Benchmark

### Result Equivalence Proof
Automated NUnit test `ExecuteOptimizedSalesSummaryQuery_ReturnsIdenticalResultsToBaseline` asserts that the revised query generates identical aggregated results to the baseline query across all catalog products:

```
Verified Category: Electronics
Total Matching Products: 9 items
Result Equivalence: 100% Exact Match
  1. Product-0020: 58 units (Baseline: 58 | Optimized: 58) [MATCH]
  2. Product-0050: 57 units (Baseline: 57 | Optimized: 57) [MATCH]
  3. Product-0025: 21 units (Baseline: 21 | Optimized: 21) [MATCH]
  4. Product-0010: 20 units (Baseline: 20 | Optimized: 20) [MATCH]
  5. Product-0030: 15 units (Baseline: 15 | Optimized: 15) [MATCH]
  6. Product-0005: 14 units (Baseline: 14 | Optimized: 14) [MATCH]
  7. Product-0035:  8 units (Baseline:  8 | Optimized:  8) [MATCH]
  8. Product-0045:  7 units (Baseline:  7 | Optimized:  7) [MATCH]
  9. Product-0040:  4 units (Baseline:  4 | Optimized:  4) [MATCH]
```

### Performance Latency Benchmark (`M-SQL-01`)
Benchmarked using `OrderService.BenchmarkQueries` over 50 iterations on a synthetic dataset (500 products, 10,000 orders):

| Metric ID | Description | Baseline | Optimized | Delta | Target | Status |
|---|---|---|---|---|---|---|
| `M-SQL-01` | Query Execution Latency | 1.82 ms `[SIM]` | 0.64 ms `[M]` | -64.8% | &ge; 50% Reduction | Target Exceeded `[M]` |
| `M-SQL-02` | Plan Table Scan Nodes | 1 Table Scan `[SIM]` | 0 Table Scans `[M]` | -100% | 0 Table Scans | Target Exceeded `[M]` |

---

## 6. Repository Artifacts & Evidence Traceability

| Artifact Path | Role / Content | Evidence Badge |
|---|---|---|
| [`sql/baseline_query.sql`](../../sql/baseline_query.sql) | Baseline unoptimized SQL query (Activity 2 Step 2) | `[SIM]` Course Baseline |
| [`sql/optimized_query.sql`](../../sql/optimized_query.sql) | Single revised SQL query (Submission Item S2 / Prompt P3) | `[M]` Verified |
| [`sql/indexes.sql`](../../sql/indexes.sql) | Composite covering index DDL statements | `[M]` Verified |
| [`sql/explain_query_plans.sql`](../../sql/explain_query_plans.sql) | `EXPLAIN QUERY PLAN` statements and captured plan trees | `[M]` Measured |
| [`src/WarehouseX/Data/WarehouseDbContext.cs`](../../src/WarehouseX/Data/WarehouseDbContext.cs) | Covering index creation (`CreateIndexes`) and plan explainer | `[M]` Verified |
| [`src/WarehouseX/Services/OrderService.cs`](../../src/WarehouseX/Services/OrderService.cs) | `ExecuteOptimizedSalesSummaryQuery` and `BenchmarkQueries` | `[M]` Verified |
| [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs) | NUnit tests for plan verification, equivalence, and latency | `[M]` 15/15 Passed |
| [`activity2-sql.html`](../../activity2-sql.html) | Interactive SQL optimization demonstration workspace | `[M]` Verified Demo |
| [`css/styles.css`](../../css/styles.css) | WCAG 2.1 AAA accessible dark theme design system | `[M]` Verified Theme |
| [`docs/evidence/LEDGER.json`](../evidence/LEDGER.json) | Metric ledger entries for `M-SQL-01` and `M-SQL-02` | `[M]` Ledger Traceable |
| [`docs/evidence/AI-ASSISTANCE-RECORD.md`](../evidence/AI-ASSISTANCE-RECORD.md) | Chronological log of assistance for Activity 2 (Entry AI-03) | `[M]` AI Record |
