// Shared Scan interactions
(function () {
  'use strict';
  window.SCAN = window.SCAN || {};
  // Basic helpers used by scan views
  function showAlert(message, type) {
    const container = document.querySelector('.container');
    const el = document.createElement('div');
    el.className = `alert alert-${type} alert-dismissible fade show`;
    el.setAttribute('role', 'alert');
    el.innerHTML = `
      <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'info' ? 'info-circle' : 'exclamation-circle'} me-2"></i>${message}
      <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    container.insertBefore(el, container.children[1]);
    setTimeout(() => el.remove(), 4000);
  }

  // Public bootstrap for StartScan page
  window.SCAN.initStartScan = function initStartScan({ sessionId, qrIdentity }) {
    // simple keyboard Enter handling for manual input if present
    const manual = document.getElementById('manual-barcode');
    if (manual) {
      manual.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
          e.preventDefault();
          const btn = manual.parentElement?.querySelector('button');
          btn?.click();
        }
      });
    }
    // expose to window so inline page script can reuse
    window.SCAN.showAlert = showAlert;
  };
})();
