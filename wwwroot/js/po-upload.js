// po-upload.js - modular JS for PO upload & preview interactions
(() => {
  const state = { sessionId: null, token: null };
  function qs(sel, root = document) {
    return root.querySelector(sel);
  }
  function qsa(sel, root = document) {
    return Array.from(root.querySelectorAll(sel));
  }
  function toast(msg, type = 'info') {
    console.log(`[${type}]`, msg);
  }
  function init() {
    state.token = qs('input[name="__RequestVerificationToken"]')?.value;
    const previewCard = qs('#previewCard');
    if (previewCard) {
      state.sessionId = previewCard.dataset.sessionId || qs('input[name="SessionId"]')?.value;
    }
    wireEvents();
  }
  function wireEvents() {
    const table = qs('#previewTable');
    if (table) {
      table.addEventListener('click', onTableClick);
    }
    const storeBtn = qs('#btnStoreAll');
    if (storeBtn) {
      storeBtn.addEventListener('click', onStoreAll);
    }
  }
  async function onTableClick(e) {
    const btn = e.target.closest('button');
    if (!btn) return;
    const row = e.target.closest('tr[data-item-id]');
    if (!row) return;
    const itemId = row.dataset.itemId;
    if (btn.classList.contains('method-btn')) {
      await setRowMethod(itemId, btn.dataset.method, row);
    } else if (btn.classList.contains('btn-commit')) {
      await commitRow(itemId, row, btn);
    }
  }
  async function setRowMethod(itemId, method, row) {
    try {
      const fd = new FormData();
      fd.append('itemId', itemId);
      fd.append('method', method);
      if (state.token) fd.append('__RequestVerificationToken', state.token);
      const res = await fetch('/PO/SetRowMethod', { method: 'POST', body: fd });
      const json = await res.json();
      if (!json.success) {
        toast(json.message, 'error');
        return;
      }
      updateRowMethodUI(row, method);
    } catch (err) {
      toast(err.message, 'error');
    }
  }
  function updateRowMethodUI(row, method) {
    row.dataset.method = method;
    qsa('.method-btn', row).forEach((b) => {
      const m = b.dataset.method;
      if (m === 'LOOSE') b.className = 'btn ' + (method === 'LOOSE' ? 'btn-primary' : 'btn-outline-primary') + ' method-btn btn-sm';
      else b.className = 'btn ' + (method === 'PALLET' ? 'btn-warning' : 'btn-outline-warning') + ' method-btn btn-sm';
    });
    const actionCell = row.querySelector('.action-cell');
    if (actionCell) actionCell.innerHTML = '<button type="button" class="btn btn-success btn-sm btn-commit">Commit</button>';
    const statusBadge = row.querySelector('.status-badge');
    if (statusBadge) statusBadge.innerHTML = `<span class="badge bg-info">${method} Selected</span>`;
  }
  async function commitRow(itemId, row, btn) {
    try {
      btn.disabled = true;
      const original = btn.innerHTML;
      btn.textContent = 'Processing...';
      const fd = new FormData();
      fd.append('itemId', itemId);
      if (state.token) fd.append('__RequestVerificationToken', state.token);
      const res = await fetch('/PO/CommitSingleRow', { method: 'POST', body: fd });
      const json = await res.json();
      if (!json.success) {
        toast(json.message, 'error');
        btn.disabled = false;
        btn.innerHTML = original;
        return;
      }
      row.classList.add('table-success');
      row.querySelector('.method-group')?.remove();
      const actionCell = row.querySelector('.action-cell');
      if (actionCell) actionCell.innerHTML = '<span class="text-success small">Done</span>';
      const statusBadge = row.querySelector('.status-badge');
      if (statusBadge) statusBadge.innerHTML = '<span class="badge bg-success">Processed</span>';
      toast(json.message, 'success');
    } catch (err) {
      toast(err.message, 'error');
      btn.disabled = false;
    }
  }
  async function onStoreAll() {
    const btn = this;
    const spinner = btn.querySelector('.spinner-border');
    btn.disabled = true;
    spinner.classList.remove('d-none');
    try {
      const fd = new FormData();
      fd.append('sessionId', state.sessionId || '');
      if (state.token) fd.append('__RequestVerificationToken', state.token);
      const res = await fetch('/PO/StoreAllToDatabase', { method: 'POST', body: fd });
      const json = await res.json();
      if (!json.success) {
        toast(json.message, 'error');
        btn.disabled = false;
        spinner.classList.add('d-none');
        return;
      }
      toast(json.message, 'success');
      setTimeout(() => window.location.reload(), 1500);
    } catch (err) {
      toast(err.message, 'error');
      btn.disabled = false;
      spinner.classList.add('d-none');
    }
  }
  document.addEventListener('DOMContentLoaded', init);
})();
