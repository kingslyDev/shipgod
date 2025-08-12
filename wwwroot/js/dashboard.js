// Dashboard specific JavaScript
(() => {
  'use strict';

  document.addEventListener('DOMContentLoaded', function () {
    initializeDashboard();
    addRippleCSS();
  });

  document.addEventListener('DOMContentLoaded', function () {
    function updateWelcomeClock() {
      const now = new Date();
      const timeString = now.toLocaleTimeString('id-ID', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      });

      const welcomeClockElement = document.getElementById('welcomeClockDisplay');
      if (welcomeClockElement) {
        welcomeClockElement.textContent = timeString;
      }
    }

    // Update immediately and then every second
    updateWelcomeClock();
    setInterval(updateWelcomeClock, 1000);
  });

  function initializeDashboard() {
    initializeAlerts();
    initializeActionCards();
    initializeAnimations();
    initializeAccessibility();
  }

  // Enhanced alert management
  function initializeAlerts() {
    const alerts = document.querySelectorAll('.alert-vibrant');

    alerts.forEach((alert) => {
      // Add close functionality if not present
      if (!alert.querySelector('.btn-close')) {
        const closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'btn-close';
        closeBtn.setAttribute('data-bs-dismiss', 'alert');
        closeBtn.setAttribute('aria-label', 'Close');
        alert.appendChild(closeBtn);
      }

      // Auto-dismiss after 6 seconds
      setTimeout(() => {
        if (alert.classList.contains('show')) {
          const bsAlert = new bootstrap.Alert(alert);
          bsAlert.close();
        }
      }, 6000);

      // Add slide-in animation
      alert.style.opacity = '0';
      alert.style.transform = 'translateX(-100%)';

      setTimeout(() => {
        alert.style.transition = 'all 0.5s ease';
        alert.style.opacity = '1';
        alert.style.transform = 'translateX(0)';
      }, 100);
    });
  }

  // Action card enhancements
  function initializeActionCards() {
    const actionCards = document.querySelectorAll('.action-card');

    actionCards.forEach((card) => {
      // Add keyboard navigation
      card.setAttribute('tabindex', '0');
      card.setAttribute('role', 'button');

      // Click handler
      card.addEventListener('click', function (e) {
        if (!e.target.closest('a')) {
          const link = card.querySelector('a');
          if (link) {
            // Add ripple effect
            createRippleEffect(card, e);

            // Navigate after animation
            setTimeout(() => {
              link.click();
            }, 150);
          }
        }
      });

      // Keyboard navigation
      card.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          const link = card.querySelector('a');
          if (link) {
            createRippleEffect(card, e);
            setTimeout(() => {
              link.click();
            }, 150);
          }
        }
      });
    });
  }

  // Create ripple effect
  function createRippleEffect(element, event) {
    const ripple = document.createElement('span');
    const rect = element.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = (event.clientX || rect.left + rect.width / 2) - rect.left - size / 2;
    const y = (event.clientY || rect.top + rect.height / 2) - rect.top - size / 2;

    ripple.style.width = ripple.style.height = size + 'px';
    ripple.style.left = x + 'px';
    ripple.style.top = y + 'px';
    ripple.classList.add('ripple');

    element.appendChild(ripple);

    setTimeout(() => {
      ripple.remove();
    }, 600);
  }

  // Initialize animations
  function initializeAnimations() {
    // Staggered card animations
    const cards = document.querySelectorAll('.action-card');
    cards.forEach((card, index) => {
      card.style.opacity = '0';
      card.style.transform = 'translateY(30px)';

      setTimeout(() => {
        card.style.transition = 'all 0.6s ease';
        card.style.opacity = '1';
        card.style.transform = 'translateY(0)';
      }, index * 100 + 200);
    });

    // Welcome section animation
    const welcomeSection = document.querySelector('.welcome-section');
    if (welcomeSection) {
      welcomeSection.style.opacity = '0';
      welcomeSection.style.transform = 'translateY(-20px)';

      setTimeout(() => {
        welcomeSection.style.transition = 'all 0.8s ease';
        welcomeSection.style.opacity = '1';
        welcomeSection.style.transform = 'translateY(0)';
      }, 100);
    }
  }

  // Accessibility enhancements
  function initializeAccessibility() {
    // Ensure proper focus management
    const focusableElements = document.querySelectorAll('a, button, [tabindex]');

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

    // Announce page changes for screen readers
    const pageTitle = document.querySelector('h2');
    if (pageTitle) {
      pageTitle.setAttribute('aria-live', 'polite');
    }
  }

  // Add ripple CSS
  function addRippleCSS() {
    const rippleCSS = `
        .ripple {
            position: absolute;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.6);
            transform: scale(0);
            animation: rippleAnimation 0.6s linear;
            pointer-events: none;
        }

        @keyframes rippleAnimation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }
        `;

    const style = document.createElement('style');
    style.textContent = rippleCSS;
    document.head.appendChild(style);
  }

  // Utility functions for dashboard
  window.DashboardUtils = {
    showSuccessMessage: function (message) {
      if (window.ShipmentApp) {
        window.ShipmentApp.showAlert(message, 'success');
      }
    },

    showErrorMessage: function (message) {
      if (window.ShipmentApp) {
        window.ShipmentApp.showAlert(message, 'danger');
      }
    },

    refreshDashboard: function () {
      window.location.reload();
    },

    navigateToCard: function (controller, action) {
      window.location.href = `/${controller}/${action}`;
    },
  };
})();
{
  document.querySelectorAll('.alert-vibrant').forEach((alert) => {
    setTimeout(() => {
      if (alert.classList.contains('show')) alert.classList.remove('show');
    }, 6000);
  });
}
