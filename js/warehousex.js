/**
 * WarehouseX Client Demonstration Logic
 * Implements idle arrival states, interactive matrix filtering, and strategy inspection.
 */
document.addEventListener('DOMContentLoaded', () => {
  initTabs();
  initFilters();
  initDiagnostics();
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
