# AI Assistance Chronological Record · Warehouse X

**System**: Warehouse X Logistics & Order Management Platform  
**Author**: Juan Diego S. (`jdsaire`)  
**Scope**: Course 9 Capstone Activities 1–4  
**Feeds**: Submission Item S5 (Prompt P6 · Rubric Criterion C5)  

---

## Record Schema
Each entry documents an interaction with the AI environment during development, capturing what was requested, what was suggested, what was accepted or modified, and the resulting artifact.

---

## Chronological Development Log

### Entry AI-01 · Activity 1: Strategic Optimization Plan
- **Date**: 2026-11-11
- **Activity**: Activity 1 · Strategic Optimization Plan
- **Inquiry / Prompt**: Requested assistance in structuring a multi-phase optimization framework (P1–P4) and mapping out strategies across the 15 rubric requirements (SQL query speed, loop bottlenecks, runtime crash resilience, and long-term telemetry).
- **Assistance Provided**: The AI environment proposed organizing the plan around the four course problem areas (I1–I4), recommended establishing quantifiable performance metric baselines with unmeasured values badged `[GAP]`, and suggested scaffolding a minimal C# SQLite harness.
- **Evaluation & Action**: Accepted the four-phase structure and 15-requirement breakdown. Modified the framework to strictly badge all synthetic data workloads as `SIMULATED-WORKLOAD` and ensure all baseline metrics remain `[GAP]` until measured.
- **Resulting Artifacts**: `docs/STRATEGIC-PLAN.md`, `docs/reports/ACTIVITY-1-COMPLETION-REPORT.md`, `activity1-strategy.html`.

### Entry AI-02 · Activity 1: Minimal Runnable Scaffold Architecture
- **Date**: 2026-11-11
- **Activity**: Activity 1 · Runnable Scaffold Architecture
- **Inquiry / Prompt**: Requested architectural review for the minimal C# .NET 8 test harness to run the course snippets verbatim without modifying the course code.
- **Assistance Provided**: The AI environment recommended creating domain models `Order` and `Product` with explicit property-to-column mappings to bridge the naming discrepancy between SQL (`ProductID`, `ProductName`) and C# (`Id`, `Name`, `Stock`), and implementing `WarehouseDbContext.Products` with `Find(id)` and `FirstOrDefault(predicate)` accessors.
- **Evaluation & Action**: Accepted the recommendation in full. Implemented `WarehouseDbContext.cs` and verified that both baseline snippets compile and execute verbatim against in-memory SQLite.
- **Resulting Artifacts**: `src/WarehouseX/`, `tests/WarehouseX.Tests/`.

### Entry AI-03 · Activity 2: SQL Query Optimization & Covering Indexes
- **Date**: 2026-11-11
- **Activity**: Activity 2 · Query Optimization with SQL Execution Plans
- **Inquiry / Prompt**: Requested execution plan analysis of the unoptimized sales summary query and recommendations for covering indexes and query restructuring techniques to eliminate full table scans while guaranteeing 100% result equivalence.
- **Assistance Provided**: The AI environment analyzed the SQLite query plan, identifying bottlenecks in the full table scan of `Orders` and post-join `Category` evaluation. It proposed a composite covering index `idx_products_category_covering` on `Products(Category, ProductID, ProductName)`, a covering index `idx_orders_product_qty` on `Orders(ProductID, Quantity)`, and inverting the driving table to filter `Products` prior to joining `Orders`.
- **Evaluation & Action**: Evaluated the proposal against the capstone requirement for a single revised query (Submission Item S2). Implemented the revised query with driving-table restructuring and composite covering indexes, verified that result sets match the baseline 100% across all catalog items, and confirmed execution plans eliminate all table heap scans.
- **Resulting Artifacts**: `sql/baseline_query.sql`, `sql/optimized_query.sql`, `sql/indexes.sql`, `sql/explain_query_plans.sql`, `activity2-sql.html`, `tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`.

### Entry AI-04 · Activity 3: Application Code Optimization & Loop Refactoring
- **Date**: 2026-11-11
- **Activity**: Activity 3 · Application Code Optimization
- **Inquiry / Prompt**: Requested profiling and refactoring recommendations for the sequential order processing loop exhibiting repeated database queries ($N+1$ query pattern) and evaluation of in-memory caching strategies to achieve $O(1)$ query complexity while preserving 100% output equivalence.
- **Assistance Provided**: The AI environment identified the bottleneck where `db.Products.FirstOrDefault()` executed a separate SQL query and connection acquisition on every iteration. It recommended pre-loading the product catalog into an in-memory hash dictionary (`Dictionary<int, Product>`) prior to loop iteration, resolving product metadata via `TryGetValue`, and verifying deterministic string output equivalence.
- **Evaluation & Action**: Accepted the dictionary caching proposal. Implemented `ProductSet.AsDictionary()` in `WarehouseDbContext.cs` and `OrderService.ProcessOrdersLoopOptimized` in `OrderService.cs`. Verified in NUnit that database calls dropped from 10,000 to 1 (-99.99%), batch latency decreased from 3,929 ms to 18.4 ms (-99.5%), and formatted log lines matched the baseline verbatim across all orders.
- **Resulting Artifacts**: `src/WarehouseX/Data/WarehouseDbContext.cs`, `src/WarehouseX/Services/OrderService.cs`, `tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`, `activity3-code.html`, `docs/evidence/LEDGER.json`.

### Entry AI-05 · Activity 4: Debugging Application Code & Resilience Architecture
- **Date**: 2026-11-11
- **Activity**: Activity 4 · Debugging Application Code & Resilience Architecture
- **Inquiry / Prompt**: Requested vulnerability analysis of the non-resilient order processing function (`ProcessOrder`), recommendations for custom domain exception models, boundary guard validation for null pointers and negative stock overdrafts, and structured wrapper design to eliminate unhandled host crashes during batch order execution.
- **Assistance Provided**: The AI environment analyzed the function, identifying null reference dereferences on missing product catalog queries and inventory corruption caused by unvalidated stock deductions. It proposed an exception hierarchy rooted at `WarehouseException` (`ProductNotFoundException`, `InsufficientStockException`, `InvalidOrderQuantityException`), formulated defensive guard clauses with atomic SQLite persistence (`db.Products.UpdateStock`), and designed the `TryProcessOrder` wrapper returning immutable `OrderProcessingResult` status codes.
- **Evaluation & Action**: Evaluated and accepted the recommendations. Implemented the custom domain exceptions, `OrderService.ProcessOrderDebugged`, `TryProcessOrder`, and `ProcessOrdersBatchResilient`. Verified in NUnit that 15/15 tests passed cleanly, completely eliminating unhandled process crashes across a 1,000-order heterogeneous batch (-100% crash rate) and preserving database stock integrity against overdrafts.
- **Resulting Artifacts**: `src/WarehouseX/Exceptions/`, `src/WarehouseX/Models/OrderProcessingResult.cs`, `src/WarehouseX/Services/OrderService.cs`, `src/WarehouseX/Data/WarehouseDbContext.cs`, `tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs`, `activity4-debug.html`, `docs/reports/ACTIVITY-4-COMPLETION-REPORT.md`, `docs/evidence/LEDGER.json`.


