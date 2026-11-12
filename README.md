# WarehouseX · Performance Optimization and Scalability

A comprehensive performance optimization and reliability engineering implementation for Warehouse X's logistics and order management platform, built as the capstone project for the Microsoft Full Stack Developer Specialization.

## System Overview

Warehouse X operates an order fulfillment platform managing high-throughput order intake, product catalog resolution, and stock management. As catalog size and transaction volume scale, the system addresses four primary operational dimensions:

1. **Strategic Planning & Baselining**: Establishing quantified metrics, workload models, and isolated optimization phases.
2. **SQL Query Optimization**: Resolving relational join bottlenecks, index scanning overhead, and query execution plans.
3. **Application Logic Refactoring**: Eliminating redundant database queries ($N+1$ query patterns), batching memory operations, and optimizing iteration logic.
4. **Resilient Debugging & Error Handling**: Guarding against null references, stock discrepancies, and concurrency edge cases.

## Technology Architecture

- **Backend Platform**: .NET 8 (C#)
- **Data Engine**: SQLite 3 (embedded local SQL database) exposing execution plans via native `EXPLAIN QUERY PLAN`
- **Data Access**: Structured Object-Relational Data Context (`WarehouseDbContext`) with explicit entity-to-column mappings
- **Automated Test Suite**: NUnit 3 automated test runner executing schema validation, seed verification, and optimization benchmarks
- **Interactive Demonstration**: Multipage static web application published via GitHub Pages (`index.html`, `activity1-strategy.html`)

## Project Structure

```text
├── docs/
│   ├── STRATEGIC-PLAN.md               # S1 Strategic Plan (Framework P1–P4, Requirements R1–R15)
│   ├── evidence/
│   │   ├── LEDGER.json                 # Quantified performance metrics ledger
│   │   └── AI-ASSISTANCE-RECORD.md     # Chronological development assistance log
│   └── reports/
│       ├── README.md                   # Index of milestone completion reports
│       ├── ACTIVITY-1-COMPLETION-REPORT.md # Activity 1 report (Rubric C1)
│       ├── ACTIVITY-2-COMPLETION-REPORT.md # Activity 2 report (Rubric C2)
│       ├── ACTIVITY-3-COMPLETION-REPORT.md # Activity 3 report (Rubric C3)
│       └── ACTIVITY-4-COMPLETION-REPORT.md # Activity 4 report (Rubric C4)
├── sql/
│   ├── baseline_query.sql              # Activity 2 unoptimized query
│   ├── optimized_query.sql             # Submission Item S2 optimized SQL query
│   ├── indexes.sql                     # Composite covering index definitions
│   └── explain_query_plans.sql         # Native SQLite EXPLAIN QUERY PLAN analysis
├── src/
│   └── WarehouseX/
│       ├── Data/                       # WarehouseDbContext, SQLite schema, synthetic seed generator
│       ├── Exceptions/                 # Domain exception hierarchy (WarehouseException, etc.)
│       ├── Models/                     # Domain entities (Order, Product, OrderProcessingResult)
│       └── Services/                   # Query execution, loop processing, and order pipelines
├── tests/
│   └── WarehouseX.Tests/               # NUnit unit and integration test fixtures (34 tests)
├── css/
│   └── styles.css                      # Accessible presentation styles
├── js/
│   └── warehousex.js                   # Interactive demonstration client logic
├── index.html                          # Interactive demonstration navigation hub
├── activity1-strategy.html             # Activity 1 strategic optimization viewer
├── activity2-sql.html                  # Activity 2 SQL query optimization viewer
├── activity3-code.html                 # Activity 3 application code refactoring viewer
├── activity4-debug.html                # Activity 4 debugging & resilience viewer
├── WarehouseX.sln                      # .NET 8 solution root
└── .gitignore                          # Build, test, and runtime artifact exclusions
```

## Quickstart

### Prerequisites
- .NET 8 SDK (`dotnet --version` >= 8.0.0)

### Build Solution
```bash
dotnet build WarehouseX.sln
```

### Run Automated Tests
```bash
dotnet test WarehouseX.sln --logger "console;verbosity=normal"
```

### Launch Interactive Demo
Open `index.html` in any modern web browser or serve locally using any standard static file server.

## AI Assistance Disclosure

This project was developed with AI assistance using Google Antigravity as the AI environment. Google Antigravity assisted throughout the engineering process by analyzing query execution plans, evaluating application loop refactoring strategies, suggesting defensive programming guard clauses, and helping formulate the structured performance optimization plan across the four course activities. All generated outputs were reviewed, refined, verified against the course rubric, and tested locally by the author.
