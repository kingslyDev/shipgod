// PO shared interactions (Preview/Create/Index)
(function () {
  'use strict';

  function qs(sel, root) {
    return (root || document).querySelector(sel);
  }
  function qsa(sel, root) {
    return Array.from((root || document).querySelectorAll(sel));
  }

  // Radio option helper
  function updateRadioSelection() {
    qsa('.radio-option').forEach((o) => o.classList.remove('selected'));
    const checked = qs('input[name="ShipmentType"]:checked');
    if (checked) {
      const opt = checked.closest('.radio-option');
      if (opt) opt.classList.add('selected');
    }
  }

  function selectRadio(id) {
    const el = qs('#' + id);
    if (el) {
      el.checked = true;
      updateRadioSelection();
    }
  }

  // Preview page dynamic calculation
  async function updateCalculation(sessionId) {
    const checked = qs('input[name="ShipmentType"]:checked');
    if (!checked || !sessionId) return;
    const shipmentType = checked.value;

    const totalBoxesEl = qs('#totalBoxes');
    if (totalBoxesEl) totalBoxesEl.textContent = '...';

    try {
      const res = await fetch('/PO/UpdateMode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `sessionId=${sessionId}&shipmentType=${shipmentType}`,
      });
      const data = await res.json();
      if (data.success) {
        updatePreviewTable(data.data);
        updatePreviewStats(data.data);
      }
    } catch (e) {
      console.error(e);
      alert('Gagal memperbarui perhitungan, coba lagi.');
    }
  }

  function updatePreviewTable(rows) {
    const tbody = qs('#previewTableBody');
    if (!tbody) return;
    tbody.innerHTML = '';
    rows.forEach((item, index) => {
      const tr = document.createElement('tr');
      tr.dataset.index = index;
      tr.innerHTML = `
        <td>
          <input type="hidden" name="ProcessedData[${index}].NoPO" value="${item.noPO}" />
          <strong>${item.noPO}</strong>
        </td>
        <td>
          <input type="hidden" name="ProcessedData[${index}].Model" value="${item.model}" />
          <span class="badge-enhanced" style="background: var(--primary-color); color: #fff;">${item.model}</span>
        </td>
        <td>
          <input type="hidden" name="ProcessedData[${index}].TotalQty" value="${item.totalQty}" />
          <strong style="color: var(--primary-color);">${Number(item.totalQty).toLocaleString()}</strong>
        </td>
        <td>
          <input type="hidden" name="ProcessedData[${index}].QtyPallet" value="${item.qtyPallet}" />
          <span class="badge-enhanced" style="background: var(--success-color); color: #fff;">${item.qtyPallet}</span>
        </td>
        <td>
          <input type="hidden" name="ProcessedData[${index}].QtyBox" value="${item.qtyBox}" />
          <span class="badge-enhanced" style="background: var(--warning-color); color: #fff;">${item.qtyBox}</span>
        </td>
        <td>
          <input type="hidden" name="ProcessedData[${index}].QtyPcs" value="${item.qtyPcs}" />
          <span class="badge-enhanced" style="background: var(--primary-light); color: #fff;">${item.qtyPcs}</span>
        </td>
        <td><input type="text" name="ProcessedData[${index}].NoInvoice" class="form-control-enhanced" placeholder="Invoice..." /></td>
        <td><input type="text" name="ProcessedData[${index}].Container" class="form-control-enhanced" placeholder="Container..." /></td>
        <td><input type="text" name="ProcessedData[${index}].ShipmentDetail" class="form-control-enhanced" placeholder="Detail..." /></td>
        <td>
          <button type="button" class="btn btn-sm btn-outline-primary" data-action="edit-row" data-index="${index}"><i class="fas fa-edit"></i></button>
        </td>`;
      tbody.appendChild(tr);
    });
  }

  function updatePreviewStats(rows) {
    const processed = qs('#processedRows');
    const totalBoxes = qs('#totalBoxes');
    if (processed) processed.textContent = rows.length.toLocaleString();
    if (totalBoxes) totalBoxes.textContent = rows.reduce((s, r) => s + Number(r.qtyBox || 0), 0).toLocaleString();
  }

  function handleEditRow(index) {
    const row = qs(`tr[data-index="${index}"]`);
    if (!row) return;
    const qtyInput = qs(`input[name="ProcessedData[${index}].TotalQty"]`, row);
    const curr = qtyInput?.value || '';
    const next = prompt('Masukkan quantity baru:', curr);
    if (next && !isNaN(next) && Number(next) > 0) {
      qtyInput.value = Number(next);
      const strong = qs('td:nth-child(3) strong', row);
      if (strong) strong.textContent = Number(next).toLocaleString();
      alert('Quantity diperbarui. Perhitungan akan dihitung saat submit.');
    }
  }

  // Public bootstrap per-page
  window.PO = {
    initPreview: function (sessionId) {
      qsa('input[name="ShipmentType"]').forEach((r) => r.addEventListener('change', () => updateCalculation(sessionId)));
      updateRadioSelection();
      const tbody = qs('#previewTableBody');
      if (tbody) {
        tbody.addEventListener('click', (e) => {
          const btn = e.target.closest('[data-action="edit-row"]');
          if (btn) handleEditRow(btn.dataset.index);
        });
      }
      updateCalculation(sessionId);
    },
    selectRadio,
  };
})();
