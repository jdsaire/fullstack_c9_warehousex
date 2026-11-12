# Activity 4 Completion Report · WarehouseX

| Document Metadata | Value |
|---|---|
| **Activity ID** | Course 9 Capstone · Activity 4 (C9-ACT4) |
| **Title** | Debugging Application Code & System Resilience |
| **Author** | Juan Diego S. (`88201583+jdsaire@users.noreply.github.com`) |
| **Specialization** | Microsoft Full Stack Developer Specialization |
| **Course** | Course 9: Performance Optimization and Scalability |
| **Status** | Completed on branch `a4-debug` |
| **Target Branch** | `a4-debug` |
| **Date** | November 2026 |

---

## 1. Executive Summary

Activity 4 of the WarehouseX Capstone addresses application crash vulnerabilities, uncaught exceptions, null pointer dereferences, and inventory boundary discrepancies (Course 9 Focus Area F4).

The primary objective was to analyze the course-supplied non-resilient order processing function (Activity 4 Step 2), isolate the architectural causes of system instability and runtime faults, formulate defensive guard clauses and a typed domain exception hierarchy, implement the final debugged application code (Submission Item S4 / Capstone Prompt P5 / Rubric Criterion C4), and verify 100% crash elimination and inventory integrity through automated NUnit test suites and an interactive demonstration workspace.

### Milestone Deliverables Achieved
- **Debugged Application Code (`src/WarehouseX/Services/OrderService.cs`)**: Implemented `ProcessOrderDebugged` (Submission Item S4 / Prompt P5), introducing sequential defensive guard clauses validating null order payloads, positive order quantities, product catalog presence, and available inventory stock prior to state deduction.
- **SQLite State Persistence (`src/WarehouseX/Data/WarehouseDbContext.cs`)**: Extended `ProductSet` with `UpdateStock`, synchronizing inventory deductions back to the underlying SQLite relational store and eliminating memory/database divergence.
- **Custom Domain Exception Hierarchy (`src/WarehouseX/Exceptions/`)**: Engineered typed domain exceptions rooted at `WarehouseException`: `ProductNotFoundException`, `InsufficientStockException`, `InvalidOrderQuantityException`, and `InvalidOrderException`, supplying contextual error metadata without leaking raw internal stack traces.
- **Host Stability & Error Handling Wrapper (`OrderService.TryProcessOrder`)**: Implemented a comprehensive exception management wrapper returning immutable `OrderProcessingResult` payloads, ensuring zero unhandled host process crashes across heterogeneous order batches.
- **Automated Verification Suite (`tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs`)**: Implemented 15 new NUnit tests (expanding the solution test suite to 34/34 passing tests), verifying defect reproduction (null dereference, negative stock overdraft), guard clause enforcement, custom exception assertions, and 100% crash elimination under a 1,000-order mixed workload.
- **Interactive Demonstration Workspace (`activity4-debug.html`)**: Built an accessible, WCAG 2.1 AAA dark-themed demonstration interface featuring interactive code comparisons, an exception hierarchy explorer, a defect reproduction catalog, and a live client-side batch resilience simulator.
- **Multipage Navigation Integration**: Enabled full active navigation linking across `index.html`, `activity1-strategy.html`, `activity2-sql.html`, `activity3-code.html`, and `activity4-debug.html`, updating the milestone status map and ledger on the hub overview.
- **Evidence Ledger & AI Assistance Record (`docs/evidence/`)**: Recorded measured outcomes for `M-DBG-01` (0 unhandled crashes across 1,000 orders, a -100% crash rate) and `M-DBG-02` (100% boundary guard coverage) in `docs/evidence/LEDGER.json`, and appended Entry `AI-05` to `docs/evidence/AI-ASSISTANCE-RECORD.md`.

---

## 2. Course 9 Capstone Rubric Alignment

The table below quotes the Course 9 Capstone rubric criteria from the project brief and records how Activity 4 satisfies the requirements:

| Criterion | Points | Question | Activity 4 Scope & Repository Evidence |
|---|---|---|---|
| **C1** | 5 | Did you develop a strategic plan for performance optimization? | Completed in Activity 1 ([`docs/STRATEGIC-PLAN.md`](../STRATEGIC-PLAN.md)). Activity 4 fulfills Phase P4 debugging strategies addressing requirements `R-DBG-1..4` and problem area `I3`. |
| **C2** | 5 | Did you submit a single revised SQL query reflecting optimization? | Completed in Activity 2 ([`sql/optimized_query.sql`](../../sql/optimized_query.sql), PR #2 merged into `main`). |
| **C3** | 5 | Did you optimize the application code to enhance performance? | Completed in Activity 3 ([`src/WarehouseX/Services/OrderService.cs`](../../src/WarehouseX/Services/OrderService.cs), PR #3 merged into `main`). |
| **C4** | 5 | Did you debug the application and resolve runtime errors? | Completed. Implemented `ProcessOrderDebugged`, custom exception hierarchy, and `TryProcessOrder` in [`src/WarehouseX/Services/OrderService.cs`](../../src/WarehouseX/Services/OrderService.cs). Verified in [`tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs`](../../tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs) (15/15 passed) and interactive demo [`activity4-debug.html`](../../activity4-debug.html) (`[M]`). |
| **C5** | 5 | Did you include a reflective summary explaining how the AI assistant assisted in each step? | Assistance for Activity 4 logged in [`docs/evidence/AI-ASSISTANCE-RECORD.md`](../evidence/AI-ASSISTANCE-RECORD.md) (Entry AI-05); full capstone reflective summary preview (Item S5 / Prompt P6) drafted in [`SUBMISSION-PREVIEW-S5.md`](../../SUBMISSION-PREVIEW-S5.md). |

---

## 3. Non-Resilient Baseline Analysis & Defect Reproduction

### Baseline Non-Resilient Function (Activity 4 Step 2)
The starting C# code provided in the course instructions:

```csharp
public void ProcessOrder(Order order)
{
    var product = db.Products.Find(order.ProductId);
    product.Stock -= order.Quantity;
    Console.WriteLine($"Order {order.Id} processed.");
}
```

#### Identified Defects & Vulnerabilities:
1. **Uncaught NullReferenceException (Issue E1 / Bullet F1)**: If `db.Products.Find(order.ProductId)` returns `null` because the product ID does not exist in the catalog, evaluating `product.Stock` throws an immediate, unhandled `NullReferenceException`, aborting the fulfillment process.
2. **Negative Inventory Stock Overdrafts (Issue E3 / Bullet F2)**: The function performs no comparison between `product.Stock` and `order.Quantity`. If an order requests more inventory than available, the balance drops into negative numbers, corrupting warehouse inventory counts.
3. **Unvalidated Input Boundaries (Issue E3 / Bullet F3)**: Zero or negative quantities (`order.Quantity <= 0`) and `null` order references bypass checks entirely, introducing business anomalies.
4. **Missing Relational Persistence**: Memory changes to `product.Stock` are never written back to the database, creating permanent divergence between running application state and SQLite storage.

---

## 4. Debugged Application Code (Submission Item S4 / Prompt P5)

```csharp
public void ProcessOrder(Order order)
{
    // 1. Guard against null order payload
    if (order == null)
        throw new ArgumentNullException(nameof(order), "Order payload cannot be null.");

    // 2. Guard against non-positive order quantity (Edge-case boundary validation)
    if (order.Quantity <= 0)
        throw new InvalidOrderQuantityException(order.Id, order.Quantity);

    // 3. Guard against non-existent product in catalog (Null reference prevention)
    var product = db.Products.Find(order.ProductId);
    if (product == null)
        throw new ProductNotFoundException(order.ProductId);

    // 4. Guard against insufficient stock overdraft (Inventory integrity validation)
    if (product.Stock < order.Quantity)
        throw new InsufficientStockException(order.ProductId, order.Quantity, product.Stock);

    // 5. Deduct stock and persist to database
    product.Stock -= order.Quantity;
    db.Products.UpdateStock(product.Id, product.Stock);

    Console.WriteLine($"Order {order.Id} processed.");
}
```

#### Key Engineering Improvements:
1. **Defensive Guard Clauses**: Validates preconditions sequentially, rejecting invalid states at the earliest possible boundary before side effects occur.
2. **Typed Domain Exceptions**: Replaces generic exceptions with domain-specific types carrying structured diagnostics (`ProductId`, `RequestedQuantity`, `AvailableStock`, `OrderId`).
3. **Database State Synchronization**: Calls `db.Products.UpdateStock` after deduction, guaranteeing relational persistence.
4. **Zero Unhandled Process Aborts**: Integrates cleanly with `TryProcessOrder` for non-blocking batch execution.

---

## 5. Domain Exception Hierarchy & Host Stability Wrapper

To prevent application crashes when errors occur during high-volume batch processing, WarehouseX introduces a clean domain exception hierarchy:

- `WarehouseException` (abstract base domain exception)
  - `ProductNotFoundException`: Raised when a catalog item is missing (`PRODUCT_NOT_FOUND`).
  - `InsufficientStockException`: Raised when order quantity exceeds available inventory (`INSUFFICIENT_STOCK`).
  - `InvalidOrderQuantityException`: Raised when requested quantity is $\le 0$ (`INVALID_QUANTITY`).
  - `InvalidOrderException`: Raised on corrupted order payloads (`INVALID_ORDER`).

### Host Stability Wrapper: `OrderService.TryProcessOrder`
```csharp
public static OrderProcessingResult TryProcessOrder(WarehouseDbContext db, Order order, Action<string>? logAction = null)
{
    if (order == null)
        return OrderProcessingResult.Failed(0, 0, 0, "NULL_ORDER", "Order payload cannot be null.");

    try
    {
        ProcessOrderDebugged(db, order, logAction);
        var product = db.Products.Find(order.ProductId);
        return OrderProcessingResult.Completed(order.Id, order.ProductId, order.Quantity, product?.Stock ?? 0);
    }
    catch (ProductNotFoundException ex)
    {
        return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "PRODUCT_NOT_FOUND", ex.Message);
    }
    catch (InsufficientStockException ex)
    {
        return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "INSUFFICIENT_STOCK", ex.Message);
    }
    catch (InvalidOrderQuantityException ex)
    {
        return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "INVALID_QUANTITY", ex.Message);
    }
    catch (Exception ex)
    {
        return OrderProcessingResult.Failed(order.Id, order.ProductId, order.Quantity, "UNEXPECTED_ERROR", ex.Message);
    }
}
```

---

## 6. Automated NUnit Verification Results

Automated test fixture `tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs` executes 15 dedicated verification tests (expanding the solution test suite to 34 total tests):

| Test Method | Category | Verified Behavior | Status |
|---|---|---|---|
| `Baseline_ProcessOrderUnoptimized_ThrowsNullReferenceException` | Defect Reproduction | Confirms unhandled NullReferenceException on missing catalog item | `[M]` Passed |
| `Baseline_ProcessOrderUnoptimized_AllowsNegativeStock_OnOverdraft` | Defect Reproduction | Confirms negative stock overdraft permitted under baseline | `[M]` Passed |
| `ProcessOrderDebugged_ThrowsArgumentNullException_WhenOrderIsNull` | Guard Clause | Validates null order payload rejection | `[M]` Passed |
| `ProcessOrderDebugged_ThrowsInvalidOrderQuantityException` | Guard Clause | Validates non-positive quantity rejection (0, -1, -50) | `[M]` Passed |
| `ProcessOrderDebugged_ThrowsProductNotFoundException` | Guard Clause | Validates missing product exception and error message | `[M]` Passed |
| `ProcessOrderDebugged_ThrowsInsufficientStockException` | Guard Clause | Validates overdraft rejection and stock balance preservation | `[M]` Passed |
| `ProcessOrderDebugged_DeductsStockAndPersists_WhenOrderIsValid` | Fulfillment | Validates deduction and SQLite database state persistence | `[M]` Passed |
| `TryProcessOrder_HandlesNullOrderGracefully` | Resilience | Returns NULL_ORDER result without throwing | `[M]` Passed |
| `TryProcessOrder_HandlesInvalidQuantityGracefully` | Resilience | Returns INVALID_QUANTITY result without throwing | `[M]` Passed |
| `TryProcessOrder_HandlesMissingProductGracefully` | Resilience | Returns PRODUCT_NOT_FOUND result without throwing | `[M]` Passed |
| `TryProcessOrder_HandlesInsufficientStockGracefully` | Resilience | Returns INSUFFICIENT_STOCK result without throwing | `[M]` Passed |
| `TryProcessOrder_ReturnsCompletedResult_OnValidOrder` | Resilience | Returns success result with updated remaining stock | `[M]` Passed |
| `ProcessOrdersBatchResilient_EliminatesProcessCrashes` | High Volume | Processes 1,000 mixed orders with zero unhandled crashes | `[M]` Passed |

### Measured Resilience Metrics

| Metric ID | Description | Baseline | Debugged | Delta | Target | Status |
|---|---|---|---|---|---|---|
| `M-DBG-01` | Unhandled Process Crashes | Crash on invalid order `[SIM]` | 0 crashes across 1,000 orders `[M]` | -100% | 0 Unhandled Crashes | Target Exceeded `[M]` |
| `M-DBG-02` | Edge-Case Validation Coverage | 0 boundary checks `[SIM]` | 100% guard coverage (4/4 modes) `[M]` | Complete Coverage | 100% Coverage | Target Exceeded `[M]` |
| Inventory Integrity | Negative Stock Permitted | Yes (corrupted balances) `[SIM]` | 0 negative balances allowed `[M]` | Discrepancy Eliminated | 0 Discrepancies | Target Exceeded `[M]` |

---

## 7. Repository Artifacts & Evidence Traceability

| Artifact Path | Role / Content | Evidence Badge |
|---|---|---|
| [`src/WarehouseX/Services/OrderService.cs`](../../src/WarehouseX/Services/OrderService.cs) | `ProcessOrderDebugged`, `TryProcessOrder`, and `ProcessOrdersBatchResilient` | `[M]` Verified |
| [`src/WarehouseX/Exceptions/`](../../src/WarehouseX/Exceptions/) | Typed domain exception hierarchy classes | `[M]` Verified |
| [`src/WarehouseX/Models/OrderProcessingResult.cs`](../../src/WarehouseX/Models/OrderProcessingResult.cs) | Immutable result payload for non-crashing order operations | `[M]` Verified |
| [`src/WarehouseX/Data/WarehouseDbContext.cs`](../../src/WarehouseX/Data/WarehouseDbContext.cs) | SQLite inventory persistence (`ProductSet.UpdateStock`) | `[M]` Verified |
| [`tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs`](../../tests/WarehouseX.Tests/ResilienceAndDebuggingTests.cs) | NUnit tests for defect reproduction, guard clauses, and resilience | `[M]` 15/15 Passed (34/34 Suite) |
| [`activity4-debug.html`](../../activity4-debug.html) | Interactive debugging and resilience demonstration workspace | `[M]` Verified Demo |
| [`js/warehousex.js`](../../js/warehousex.js) | Interactive batch resilience simulator handler | `[M]` Verified Logic |
| [`index.html`](../../index.html) | Hub overview with enabled Activity 4 delivered milestone link | `[M]` Verified Hub |
| [`docs/evidence/LEDGER.json`](../evidence/LEDGER.json) | Metric ledger entries for `M-DBG-01` and `M-DBG-02` | `[M]` Ledger Traceable |
| [`docs/evidence/AI-ASSISTANCE-RECORD.md`](../evidence/AI-ASSISTANCE-RECORD.md) | Chronological log of assistance for Activity 4 (Entry AI-05) | `[M]` AI Record |
| [`SUBMISSION-PREVIEW-S5.md`](../../SUBMISSION-PREVIEW-S5.md) | Capstone reflective summary preview (Prompt P6 / Criterion C5) | `[M]` S5 Draft |
