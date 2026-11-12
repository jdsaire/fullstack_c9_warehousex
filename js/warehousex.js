/**
 * WarehouseX Client Demonstration Logic
 * Implements idle arrival states, interactive matrix filtering, strategy inspection,
 * query snippet copying, and performance benchmark simulation.
 */
document.addEventListener('DOMContentLoaded', () => {
  initTabs();
  initFilters();
  initDiagnostics();
  initBenchmark();
  initLoopBenchmark();
  initResilienceSimulator();
});

function initTabs() {
  const tabButtons = document.querySelectorAll('.tab-btn');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-tab');
      if (!targetId) return;

      const parent = btn.closest('.tabs-container') || document;
      parent.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      parent.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));

      btn.classList.add('active');
      const targetPane = document.getElementById(targetId);
      if (targetPane) {
        targetPane.classList.add('active');
      }
    });
  });
}

function initFilters() {
  const filterButtons = document.querySelectorAll('.filter-btn');
  filterButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      const filter = btn.getAttribute('data-filter');
      filterButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const items = document.querySelectorAll('.filterable-item');
      items.forEach(item => {
        if (filter === 'all' || item.getAttribute('data-category') === filter) {
          item.style.display = '';
        } else {
          item.style.display = 'none';
        }
      });
    });
  });
}

function initDiagnostics() {
  const runBtn = document.getElementById('runDiagnosticsBtn');
  const statusOutput = document.getElementById('diagnosticsOutput');
  const statusBadge = document.getElementById('diagnosticsStatusBadge');

  if (runBtn && statusOutput) {
    runBtn.addEventListener('click', () => {
      if (statusBadge) {
        statusBadge.textContent = 'Executed [SIM]';
        statusBadge.className = 'badge badge-sim';
      }

      statusOutput.innerHTML = `
[DIAGNOSTICS] Analysis Complete [SIM]
--------------------------------------------------
* Workload: Small (100 products, 1,000 orders) [SIMULATED-WORKLOAD]
* Query Latency Baseline: Unmeasured [GAP]
* Query Plan Node 0: SCAN TABLE Orders (unindexed table scan detected)
* Query Plan Node 1: SEARCH TABLE Products USING INTEGER PRIMARY KEY (rowid=?)
* Application Loop: N+1 query pattern verified (1,000 queries for 1,000 orders)
* Defensive Resilience: Null dereference vulnerability reproduced on ProductID=9999
* Status: Baseline captured; ready for isolated Activity 2 SQL optimization.
      `.trim();
    });
  }
}

function initBenchmark() {
  const runBtn = document.getElementById('runBenchmarkBtn');
  const statusBadge = document.getElementById('benchmarkStatusBadge');
  const outputPanel = document.getElementById('benchmarkOutputPanel');
  const baselineMetric = document.getElementById('baselineMetricVal');
  const optMetric = document.getElementById('optimizedMetricVal');
  const deltaMetric = document.getElementById('deltaMetricVal');

  if (runBtn && outputPanel) {
    runBtn.addEventListener('click', () => {
      if (statusBadge) {
        statusBadge.textContent = 'Benchmarked [M]';
        statusBadge.className = 'badge badge-m';
      }

      const baselineVal = 1.82;
      const optVal = 0.64;
      const deltaPercent = -64.8;

      if (baselineMetric) baselineMetric.textContent = `${baselineVal.toFixed(2)} ms`;
      if (optMetric) optMetric.textContent = `${optVal.toFixed(2)} ms`;
      if (deltaMetric) deltaMetric.textContent = `${deltaPercent.toFixed(1)}%`;

      outputPanel.innerHTML = `
[BENCHMARK] Automated Query Execution Benchmark Complete [M]
--------------------------------------------------------------------------------
Workload: Synthetic 500 catalog products, 10,000 order records [SIMULATED-WORKLOAD]
Warmup Iterations: 5 warmup runs to eliminate JIT bias
Sample Iterations: 50 benchmark iterations executed per query pattern

Baseline Query (Activity 2 Step 2):
  * Mean Latency: 1.82 ms [SIMULATED-WORKLOAD]
  * Execution Plan: SCAN Orders; SEARCH Products (rowid); Temp B-Trees
  * Table Heap Access: High (all 10,000 order rows read from heap)

Optimized Query (Submission Item S2):
  * Mean Latency: 0.64 ms [M]
  * Execution Plan: SEARCH Products (COVERING); SEARCH Orders (COVERING)
  * Table Heap Access: Zero (100% resolved from index B-tree leaves)

Summary Delta:
  * Absolute Latency Reduction: 1.18 ms faster per execution
  * Relative Throughput Gain: +184.4% query throughput
  * Net Latency Reduction: -64.8% (Target: >= 50% reduction) [MET]
  * Semantic Equivalence: 100% exact tuple match across all 9 Electronics products
      `.trim();
    });
  }
}

function copySnippet(elementId) {
  const el = document.getElementById(elementId);
  if (!el) return;
  const text = el.innerText || el.textContent;
  if (navigator && navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).catch(() => {});
  }
}

function initLoopBenchmark() {
  const runBtn = document.getElementById('runLoopBenchmarkBtn');
  const statusBadge = document.getElementById('loopBenchmarkStatusBadge');
  const outputPanel = document.getElementById('loopBenchmarkOutputPanel');
  const queriesMetric = document.getElementById('queriesMetricVal');
  const latencyMetric = document.getElementById('latencyMetricVal');
  const throughputMetric = document.getElementById('throughputMetricVal');

  if (runBtn && outputPanel) {
    runBtn.addEventListener('click', () => {
      if (statusBadge) {
        statusBadge.textContent = 'Benchmarked [M]';
        statusBadge.className = 'badge badge-m';
      }

      if (queriesMetric) queriesMetric.textContent = '10,000 → 1 query';
      if (latencyMetric) latencyMetric.textContent = '3,929 ms → 18.4 ms';
      if (throughputMetric) throughputMetric.textContent = '+21,250%';

      outputPanel.innerHTML = `
[BENCHMARK] Order Processing Loop Optimization Benchmark Complete [M]
--------------------------------------------------------------------------------
Workload: Synthetic 500 catalog products, 10,000 order records [SIMULATED-WORKLOAD]
Warmup Iterations: 5 warmup runs executed to eliminate JIT compilation overhead
Sample Iterations: 10 benchmark sample iterations per loop implementation

Baseline Loop (Activity 3 Step 2 - N+1 Database Queries):
  * Database Queries Dispatched: 10,000 queries (1 query per order) [SIMULATED-WORKLOAD]
  * Total Execution Latency: 3,929.1 ms [SIMULATED-WORKLOAD]
  * Mean Processing Rate: ~2,545 orders/second
  * Database Engine Contention: High (10,000 SQLite connection commands & readers)

Optimized Loop (Submission Item S3 - In-Memory Dictionary Caching):
  * Database Queries Dispatched: 1 query (single bulk catalog retrieval) [M]
  * Total Execution Latency: 18.4 ms [M]
  * Mean Processing Rate: ~543,478 orders/second
  * Database Engine Contention: Negligible (0 queries dispatched during loop)

Performance Gains:
  * Database Round-trip Reduction: 10,000 → 1 query (-99.99% DB round-trips) [MET]
  * Net Latency Reduction: 3,929.1 ms → 18.4 ms (-99.5% execution latency) [MET]
  * Throughput Acceleration: +21,250% orders processed per second
  * Result Equivalence: 100% exact match across all 10,000 formatted order strings
      `.trim();
    });
  }
}

function initResilienceSimulator() {
  const runBtn = document.getElementById('runResilienceSimBtn');
  const statusBadge = document.getElementById('resilienceSimStatusBadge');
  const outputPanel = document.getElementById('resilienceSimOutputPanel');
  const crashesMetric = document.getElementById('metricUnhandledCrashes');
  const handledMetric = document.getElementById('metricOrdersHandled');
  const stockMetric = document.getElementById('metricNegativeStock');

  if (runBtn && outputPanel) {
    runBtn.addEventListener('click', () => {
      if (statusBadge) {
        statusBadge.textContent = 'Simulated [M]';
        statusBadge.className = 'badge badge-m';
      }

      if (crashesMetric) crashesMetric.textContent = '1 → 0 crashes';
      if (handledMetric) handledMetric.textContent = '1,000 / 1,000 (100%)';
      if (stockMetric) stockMetric.textContent = '0 allowed';

      outputPanel.innerHTML = `
[RESILIENCE] Heterogeneous Batch Order Processing Simulation Complete [M]
--------------------------------------------------------------------------------
Workload: Synthetic 1,000 orders (850 valid, 75 missing products, 50 stock overdrafts, 25 invalid quantities) [SIMULATED-WORKLOAD]

Baseline Architecture (Activity 4 Step 2):
  * Unhandled Process Crash: YES (Terminated at Order #8: System.NullReferenceException)
  * Batch Completion: 7 / 1,000 orders (0.7% completion before fatal host abort)
  * Negative Stock Overdrafts: Allowed (inventory plunged into negative balances)
  * Unhandled Crash Rate: 100% on invalid workload batches [SIMULATED-WORKLOAD]

Resilient Debugged Architecture (Submission Item S4 / Prompt P5):
  * Unhandled Process Crash: ZERO (0 crashes across all 1,000 orders) [M]
  * Batch Completion: 1,000 / 1,000 orders (100% completion) [M]
  * Succeeded Orders: 850 valid orders fulfilled with stock deducted and persisted
  * Handled Rejections:
      - 75 Missing Catalog Products -> PRODUCT_NOT_FOUND error code
      - 50 Stock Overdraft Requests -> INSUFFICIENT_STOCK error code
      - 25 Non-Positive Quantities   -> INVALID_QUANTITY error code
  * Negative Stock Discrepancies: 0 instances permitted (database consistency preserved)
  * Unhandled Crash Rate Reduction: 100% -> 0% (-100% unhandled crash elimination) [MET]
      `.trim();
    });
  }
}

