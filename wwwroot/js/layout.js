// Global Layout JavaScript
(() => {
  'use strict';

  // Initialize layout functionality
  document.addEventListener('DOMContentLoaded', function () {
    initializeNavigation();
    initializeAlerts();
    initializeLoadingStates();
    initializeAccessibility();
    initializeClock();
  });

  // Navigation enhancements
  function initializeNavigation() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');

    navLinks.forEach((link) => {
      if (link.getAttribute('href') === currentPath) {
        link.classList.add('active');
      }
    });

    const navbarToggler = document.querySelector('.navbar-toggler');
    if (navbarToggler) {
      navbarToggler.addEventListener('click', function () {
        this.classList.toggle('active');
      });
    }

    document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
      anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
          target.scrollIntoView({ behavior: 'smooth' });
        }
      });
    });
  }

  // Alert system enhancements
  function initializeAlerts() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    alerts.forEach((alert) => {
      if (!alert.querySelector('.btn-close')) {
        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn-close';
        closeBtn.setAttribute('data-bs-dismiss', 'alert');
        closeBtn.setAttribute('aria-label', 'Close');
        alert.appendChild(closeBtn);
      }

      setTimeout(() => {
        if (alert.classList.contains('show')) {
          const bsAlert = new bootstrap.Alert(alert);
          bsAlert.close();
        }
      }, 6000);
    });
  }

  // Loading states for forms and buttons
  function initializeLoadingStates() {
    const forms = document.querySelectorAll('form');

    forms.forEach((form) => {
      form.addEventListener('submit', function () {
        const submitButton = form.querySelector('button[type="submit"]');
        if (submitButton) {
          const originalText = submitButton.innerHTML;
          submitButton.innerHTML = '<span class="loading"></span> Memproses...';
          submitButton.disabled = true;

          setTimeout(() => {
            submitButton.innerHTML = originalText;
            submitButton.disabled = false;
          }, 5000);
        }
      });
    });

    const logoutLinks = document.querySelectorAll('a[href*="Auth/Logout"]');
    logoutLinks.forEach((link) => {
      link.addEventListener('click', function (e) {
        e.preventDefault();
        if (confirm('Apakah Anda yakin ingin logout?')) {
          this.innerHTML = '<span class="loading"></span> Logout...';
          window.location.href = this.href;
        }
      });
    });
  }

  // Accessibility enhancements
  function initializeAccessibility() {
    const actionCards = document.querySelectorAll('.action-card');
    actionCards.forEach((card) => {
      card.setAttribute('tabindex', '0');
      card.setAttribute('role', 'button');

      card.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          const link = card.querySelector('a');
          if (link) link.click();
        }
      });
    });

    const focusableElements = document.querySelectorAll('a, button, input, select, textarea, [tabindex]');
    focusableElements.forEach((element) => {
      element.addEventListener('focus', function () {
        this.style.outline = '2px solid #1976d2';
        this.style.outlineOffset = '2px';
      });

      element.addEventListener('blur', function () {
        this.style.outline = '';
        this.style.outlineOffset = '';
      });
    });
  }

  // Real-time clock
  function initializeClock() {
    const clockEl = document.getElementById('navbarClock');
    if (!clockEl) return;

    function updateClock() {
      const now = new Date();
      const hh = String(now.getHours()).padStart(2, '0');
      const mm = String(now.getMinutes()).padStart(2, '0');
      const ss = String(now.getSeconds()).padStart(2, '0');
      clockEl.textContent = `${hh}:${mm}:${ss}`;
    }

    updateClock();
    setInterval(updateClock, 1000);
  }

  // Utility functions
  window.ShipmentApp = {
    showAlert: function (message, type = 'info') {
      const alertContainer = document.querySelector('.alert-container') || document.body;
      const alertElement = document.createElement('div');

      alertElement.className = `alert alert-${type} alert-dismissible fade show`;
      alertElement.setAttribute('role', 'alert');
      alertElement.innerHTML = `
        <i class="fas fa-info-circle"></i> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
      `;

      alertContainer.insertBefore(alertElement, alertContainer.firstChild);

      setTimeout(() => {
        const bsAlert = new bootstrap.Alert(alertElement);
        bsAlert.close();
      }, 5000);
    },

    confirmAction: function (message, callback) {
      if (confirm(message)) {
        callback();
      }
    },
  };
})();
