# Activity 1 Completion Report · WarehouseX

| Document Metadata | Value |
|---|---|
| **Activity ID** | Course 9 Capstone · Activity 1 (C9-ACT1) |
| **Title** | Developing a Strategic Plan for Performance Optimization |
| **Author** | Juan Diego S. (`88201583+jdsaire@users.noreply.github.com`) |
| **Specialization** | Microsoft Full Stack Developer Specialization |
| **Course** | Course 9: Performance Optimization and Scalability |
| **Status** | Completed on branch `a1-strategy` |
| **Target Branch** | `a1-strategy` |
| **Date** | November 2026 |

---

## 1. Executive Summary

Activity 1 of the WarehouseX Capstone establishes the foundational architecture, performance optimization strategy, quantifiable metric baselines, and runnable test harness for Warehouse X's logistics and order management platform. 

The primary objective of Activity 1 was to analyze the operational bottlenecks in order processing, catalog lookup, and stock deduction, formulate a structured multi-phase strategic plan (`P1–P4`) addressing 15 course requirements across SQL queries, application loops, and runtime exceptions, and scaffold a minimal runnable C# .NET 8 solution backed by SQLite and automated NUnit test fixtures.

### Milestone Deliverables Achieved
- **Strategic Optimization Plan (`docs/STRATEGIC-PLAN.md`)**: Comprehensive engineering plan addressing issues I1–I4, defining quantifiable metric targets (`M-SQL-01`, `M-SQL-02`, `M-APP-01`, `M-APP-02`, `M-DBG-01`, `M-DBG-02`), selecting AI environment assistance roles, and structuring work across Analysis, Optimization, and Testing phases.
- **Minimal Runnable .NET 8 Solution (`src/WarehouseX/`)**: Built domain models (`Order`, `Product`), an SQLite database context (`WarehouseDbContext`) with schema creation, explicit property-to-column mapping, and a deterministic synthetic seed generator (`SIMULATED-WORKLOAD`) supporting Small (100 products / 1,000 orders) and Medium (500 products / 10,000 orders) datasets.
- **Automated NUnit Test Suite (`tests/WarehouseX.Tests/`)**: Implemented 12 automated test cases verifying schema creation, seed row counts, property bounds, SQL column mapping, baseline `EXPLAIN QUERY PLAN` extraction, loop execution, and negative boundary exception handling (`12/12` passing).
- **Interactive Multipage Demo Shell (`index.html`, `activity1-strategy.html`)**: Interactive web interface published via GitHub Pages featuring milestone progress tracking, an interactive strategy matrix, requirements filtering, and idle arrival state diagnostics.
- **Performance Evidence Ledger & AI Assistance Record (`docs/evidence/`)**: Initialized `LEDGER.json` and `AI-ASSISTANCE-RECORD.md` adhering strictly to evidence badge standards (`[M]`, `[SIM]`, `[GAP]`).

---

## 2. Course 9 Capstone Rubric Alignment

The table below quotes the Course 9 Capstone rubric criteria from the project brief and records how Activity 1 satisfies the requirements:

| Criterion | Points | Question | Activity 1 Scope & Repository Evidence |
|---|---|---|---|
| **C1** | 5 | Did you develop a strategic plan for performance optimization? | Completed. Fully developed in [`docs/STRATEGIC-PLAN.md`](../STRATEGIC-PLAN.md) (Item S1 / Prompt P2). Covers scope boundaries, quantifiable metrics, tool selection, three-phase methodology (analysis, optimization, testing), and all 15 course requirements (`R-SQL-1..4`, `R-APP-1..4`, `R-DBG-1..4`, `R-LT-1..3`), with interactive visualization in [`activity1-strategy.html`](../../activity1-strategy.html) (`[M]`). |
| **C2** | 5 | Did you submit a single revised SQL query reflecting optimization? | Out of scope for Activity 1. Scheduled for Activity 2; baseline query execution harness and `EXPLAIN QUERY PLAN` capture implemented in [`src/WarehouseX/Services/OrderService.cs`](../../src/WarehouseX/Services/OrderService.cs). |
| **C3** | 5 | Did you optimize the application code to enhance performance? | Out of scope for Activity 1. Scheduled for Activity 3; baseline loop harness reproducing the $N+1$ query pattern verified in [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs). |
| **C4** | 5 | Did you debug the application and resolve runtime errors? | Out of scope for Activity 1. Scheduled for Activity 4; baseline failure reproduction on missing product IDs verified in [`tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs`](../../tests/WarehouseX.Tests/TypeMappingAndQueryTests.cs). |
| **C5** | 5 | Did you include a reflective summary explaining how the AI assistant assisted in each step? | Initial assistance record for Activity 1 logged in [`docs/evidence/AI-ASSISTANCE-RECORD.md`](../evidence/AI-ASSISTANCE-RECORD.md); full capstone summary (Item S5 / Prompt P6) will be compiled at Activity 4. |

---

## 3. Chronological Evolution & Iteration History

### Activity 1 Implementation Milestones (Branch `a1-strategy`)
1. **Repository & Governance Setup**: Initialized root `.gitignore` on `main` and branched `a1-strategy`. Authored `README.md` defining system architecture, quickstart steps, and the AI Assistance Disclosure section.
2. **Strategic Plan Authoring**: Drafted `docs/STRATEGIC-PLAN.md` structuring the four problem areas (I1–I4), planning framework (P1–P4), 15 requirement specifications, and synthesis answers (Z1–Z3).
3. **Backend Solution & SQLite Engine Scaffolding**: Built `WarehouseX.sln` and `src/WarehouseX/` with `Product` and `Order` models, `WarehouseDbContext` managing SQLite in-memory and local database instances, and synthetic seed generators badged `SIMULATED-WORKLOAD`.
4. **NUnit Test Harness Verification**: Implemented 12 automated unit tests across `SchemaAndSeedDataTests.cs` and `TypeMappingAndQueryTests.cs`, validating 100% test passage in < 1 second.
5. **Interactive Demonstration Workspaces**: Built `index.html` navigation hub and `activity1-strategy.html` interactive plan viewer adhering to idle arrival states and WCAG accessibility guidelines.
6. **Telemetry & Ledger Initialization**: Established `docs/evidence/LEDGER.json` and `docs/evidence/AI-ASSISTANCE-RECORD.md` logging planned versus measured metrics.

---

## 4. Component Architecture

### Backend Services & Models (`src/WarehouseX/`)
- `Models/Product.cs`: Represents catalog items (`Id`, `Name`, `Category`, `Stock`).
- `Models/Order.cs`: Represents order lines (`Id`, `ProductId`, `Quantity`).
- `Data/WarehouseDbContext.cs`: Manages SQLite connection lifecycle, schema initialization, table indexes, and synthetic data generation for Small and Medium workloads.
- `Services/OrderService.cs`: Implements baseline execution harnesses for SQL query aggregation, order loop processing, and order deduction.

### Test Fixtures (`tests/WarehouseX.Tests/`)
- `SchemaAndSeedDataTests.cs`: Validates DDL table creation and synthetic dataset generation for Small (1K) and Medium (10K) workloads (`[M]`).
- `TypeMappingAndQueryTests.cs`: Validates entity mapping, query execution plans, $N+1$ query iteration counts, and null-reference exception behavior (`[M]`).

### Web Demonstration (`index.html`, `activity1-strategy.html`)
- Multipage static presentation published via GitHub Pages.
- Fully accessible, zero external CDN dependencies, dual-state diagnostics with initial idle status.

---

## 5. Automated Test Suite Audit

Measured NUnit test execution output (`[M]`):
```text
Test run for /home/antigravity/jdsaire/fullstack_c9_warehousex/tests/WarehouseX.Tests/bin/Debug/net8.0/WarehouseX.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.2 (x64)

NUnit Adapter 4.6.0.0: Test execution started
Running all tests in WarehouseX.Tests.dll
  Passed InitializeSchema_CreatesRequiredTables [26 ms]
  Passed SeedData_MediumWorkload_GeneratesExpectedRowCounts [40 ms]
  Passed SeedData_ProductProperties_ConformToBounds [2 ms]
  Passed SeedData_SmallWorkload_GeneratesExpectedRowCounts [3 ms]
  Passed ExecuteSalesSummaryQuery_ReturnsAggregatedElectronicsSales [4 ms]
  Passed ExplainQueryPlan_OutputsPlannerExecutionNodes [3 ms]
  Passed OrderSet_ToList_ReturnsMappedOrderEntities [2 ms]
  Passed ProcessOrdersLoopUnoptimized_ExecutesVerbatimActivity3Logic [3 ms]
  Passed ProcessOrderUnoptimized_ExecutesVerbatimActivity4Logic [2 ms]
  Passed ProcessOrderUnoptimized_ThrowsNullReferenceException_OnMissingProduct [3 ms]
  Passed ProductSet_Find_MapsSqlColumnsToEntityMembers [1 ms]
  Passed ProductSet_FirstOrDefault_ResolvesViaPredicate [1 ms]

Test Run Successful.
Total tests: 12
     Passed: 12
 Total time: 0.7554 Seconds
```

---

## 6. Scope Boundaries & Next Activity

Activity 1 scope is strictly constrained to strategic planning, architecture scaffolding, synthetic data modeling, and test harness implementation. Optimization of the SQL query, loop refactoring, and code debugging are scheduled for subsequent milestone activities:
- **Activity 2 (Query Optimization)**: Single revised SQL query, composite indexing, and `EXPLAIN QUERY PLAN` comparative evaluation.
- **Activity 3 (Application Code Optimization)**: In-memory dictionary batching and $N+1$ query elimination.
- **Activity 4 (Debugging & Error Resolution)**: Guard clause implementation, custom exception hierarchies, and S5 reflective summary assembly.

---

## 7. Author Attestation

All code, tests, documentation, and reports in this repository were authored by Juan Diego S. (`jdsaire`). Technical assistance was provided by an AI environment in accordance with the course academic integrity standards and repository disclosure policies.
