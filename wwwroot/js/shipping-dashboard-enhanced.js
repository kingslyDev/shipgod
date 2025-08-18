/**
 * Executive Shipping Dashboard JavaScript Module
 * Enhanced for Baby Boomer & Millennial User Experience
 * Features: Smooth animations, intuitive interactions, accessibility
 */

class ShippingDashboard {
    constructor() {
        this.charts = {};
        this.filters = {
            country: '',
            fromDate: '',
            toDate: '',
            status: ''
        };
        this.refreshInterval = null;
        this.isLoading = false;
        
        // Configuration for enhanced UX
        this.config = {
            animationDuration: 600,
            refreshInterval: 300000, // 5 minutes
            chartAnimationDelay: 150,
            countUpDuration: 2000
        };
    }

    init() {
        this.showWelcomeMessage();
        this.initializeFilters();
        this.initializeCharts();
        this.bindEvents();
        this.startAutoRefresh();
        this.addLoadingStates();
        this.enhanceAccessibility();
        this.animateNumberCounters();
    }

    showWelcomeMessage() {
        // Show a brief welcome message for first-time users
        if (!localStorage.getItem('dashboardVisited')) {
            this.showNotification('Welcome to your Executive Shipping Dashboard! 📊', 'info', 4000);
            localStorage.setItem('dashboardVisited', 'true');
        }
    }

    initializeFilters() {
        // Get current filter values from URL or form
        const urlParams = new URLSearchParams(window.location.search);
        this.filters.country = urlParams.get('countryFilter') || '';
        this.filters.fromDate = urlParams.get('dateFromFilter') || '';
        this.filters.toDate = urlParams.get('dateToFilter') || '';
        this.filters.status = urlParams.get('statusFilter') || '';

        // Set form values with smooth transitions
        this.setFilterValue('countryFilter', this.filters.country);
        this.setFilterValue('dateFromFilter', this.filters.fromDate);
        this.setFilterValue('dateToFilter', this.filters.toDate);
        this.setFilterValue('statusFilter', this.filters.status);
    }

    setFilterValue(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.value = value;
            // Add visual feedback
            element.addEventListener('focus', this.handleFilterFocus.bind(this));
            element.addEventListener('blur', this.handleFilterBlur.bind(this));
        }
    }

    handleFilterFocus(event) {
        event.target.style.transform = 'scale(1.02)';
        event.target.style.boxShadow = '0 0 0 3px rgba(25, 118, 210, 0.1)';
    }

    handleFilterBlur(event) {
        event.target.style.transform = 'scale(1)';
        event.target.style.boxShadow = '';
    }

    bindEvents() {
        // Enhanced filter form submission with loading feedback
        const filterForm = document.getElementById('filterForm');
        if (filterForm) {
            filterForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.applyFiltersWithAnimation();
            });
        }

        // Clear filters with confirmation for baby boomers
        const clearBtn = document.getElementById('clearFilters');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                this.clearFiltersWithConfirmation();
            });
        }

        // Manual refresh with visual feedback
        const refreshBtn = document.getElementById('refreshData');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => {
                this.refreshDataWithAnimation();
            });
        }

        // Chart refresh buttons
        document.querySelectorAll('.chart-refresh').forEach(button => {
            button.addEventListener('click', (e) => {
                const chartType = e.target.closest('[data-chart]')?.dataset.chart;
                if (chartType) {
                    this.refreshChartWithAnimation(chartType);
                }
            });
        });

        // Export functionality
        const exportBtn = document.getElementById('exportData');
        if (exportBtn) {
            exportBtn.addEventListener('click', () => {
                this.exportDataWithProgress();
            });
        }

        // Add keyboard shortcuts for power users
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey || e.metaKey) {
                switch(e.key) {
                    case 'r':
                        e.preventDefault();
                        this.refreshDataWithAnimation();
                        break;
                    case 'e':
                        e.preventDefault();
                        this.exportDataWithProgress();
                        break;
                }
            }
        });

        // Enhance stat cards with click animations
        document.querySelectorAll('.stat-card').forEach(card => {
            card.addEventListener('click', () => {
                this.animateStatCard(card);
            });
        });
    }

    applyFiltersWithAnimation() {
        if (this.isLoading) return;
        
        this.isLoading = true;
        this.showLoadingIndicator('Applying filters...');
        
        // Collect filter values
        this.filters.country = document.getElementById('countryFilter')?.value || '';
        this.filters.fromDate = document.getElementById('dateFromFilter')?.value || '';
        this.filters.toDate = document.getElementById('dateToFilter')?.value || '';
        this.filters.status = document.getElementById('statusFilter')?.value || '';

        // Animate filter application
        const filterForm = document.getElementById('filterForm');
        if (filterForm) {
            filterForm.style.opacity = '0.7';
            filterForm.style.pointerEvents = 'none';
        }

        // Build URL with filters
        const params = new URLSearchParams();
        Object.keys(this.filters).forEach(key => {
            if (this.filters[key]) {
                params.append(key, this.filters[key]);
            }
        });

        // Navigate with smooth transition
        setTimeout(() => {
            window.location.href = `${window.location.pathname}?${params.toString()}`;
        }, 300);
    }

    clearFiltersWithConfirmation() {
        const hasFilters = Object.values(this.filters).some(value => value !== '');
        
        if (hasFilters) {
            this.showConfirmDialog(
                'Clear all filters?',
                'This will reset all filter selections and show all data.',
                () => {
                    this.clearAllFilters();
                }
            );
        } else {
            this.showNotification('No filters to clear', 'info', 2000);
        }
    }

    clearAllFilters() {
        // Clear form values with animation
        const inputs = document.querySelectorAll('#filterForm input, #filterForm select');
        inputs.forEach((input, index) => {
            setTimeout(() => {
                input.value = '';
                input.style.transform = 'scale(1.05)';
                setTimeout(() => {
                    input.style.transform = 'scale(1)';
                }, 150);
            }, index * 50);
        });

        // Clear filters object
        Object.keys(this.filters).forEach(key => {
            this.filters[key] = '';
        });

        // Navigate to clean URL
        setTimeout(() => {
            window.location.href = window.location.pathname;
        }, 500);
    }

    refreshDataWithAnimation() {
        if (this.isLoading) return;
        
        this.isLoading = true;
        const refreshBtn = document.getElementById('refreshData');
        
        if (refreshBtn) {
            const originalIcon = refreshBtn.innerHTML;
            
            // Animate refresh button
            refreshBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Refreshing...';
            refreshBtn.disabled = true;
        }
        
        this.showLoadingIndicator('Refreshing dashboard data...');
        
        // Refresh charts and data
        this.refreshAllCharts();
        
        setTimeout(() => {
            window.location.reload();
        }, 1000);
    }

    refreshChartWithAnimation(chartType) {
        const loadingElement = document.getElementById(`${chartType}ChartLoading`);
        if (loadingElement) {
            loadingElement.classList.add('active');
        }

        // Simulate chart refresh
        setTimeout(() => {
            if (loadingElement) {
                loadingElement.classList.remove('active');
            }
            this.showNotification(`${chartType} chart updated!`, 'success', 2000);
        }, 1500);
    }

    animateStatCard(card) {
        // Create ripple effect
        const ripple = document.createElement('span');
        ripple.className = 'ripple-effect';
        ripple.style.cssText = `
            position: absolute;
            border-radius: 50%;
            background: rgba(25, 118, 210, 0.3);
            transform: scale(0);
            animation: ripple-animation 0.6s linear;
            pointer-events: none;
            width: 20px;
            height: 20px;
            left: 50%;
            top: 50%;
            margin-left: -10px;
            margin-top: -10px;
        `;
        
        card.style.position = 'relative';
        card.appendChild(ripple);
        
        // Remove ripple after animation
        setTimeout(() => {
            if (ripple.parentNode) {
                ripple.remove();
            }
        }, 600);
        
        // Add CSS animation keyframes if not exists
        if (!document.querySelector('#ripple-keyframes')) {
            const style = document.createElement('style');
            style.id = 'ripple-keyframes';
            style.textContent = `
                @keyframes ripple-animation {
                    to {
                        transform: scale(4);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
    }

    initializeCharts() {
        this.initStatusChart();
        this.initCountryChart();
        this.initTrendChart();
    }

    initStatusChart() {
        const ctx = document.getElementById('statusPieChart');
        if (!ctx) return;

        this.charts.status = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: ['Completed', 'In Process', 'Pending'],
                datasets: [{
                    data: [40, 35, 25],
                    backgroundColor: [
                        '#4caf50',
                        '#ff9800', 
                        '#f44336'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true,
                            font: {
                                size: 12,
                                weight: '600'
                            }
                        }
                    }
                },
                animation: {
                    animateRotate: true,
                    duration: this.config.animationDuration
                }
            }
        });
    }

    initCountryChart() {
        const ctx = document.getElementById('countryBarChart');
        if (!ctx) return;

        this.charts.country = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: ['USA', 'Canada', 'Mexico', 'Germany', 'Japan'],
                datasets: [{
                    label: 'Shipments',
                    data: [45, 25, 15, 30, 20],
                    backgroundColor: '#1976d2',
                    borderColor: '#1565c0',
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                animation: {
                    duration: this.config.animationDuration,
                    delay: this.config.chartAnimationDelay
                }
            }
        });
    }

    initTrendChart() {
        const ctx = document.getElementById('trendLineChart');
        if (!ctx) return;

        this.charts.trend = new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                datasets: [{
                    label: 'Monthly Shipments',
                    data: [150, 180, 165, 220, 195, 240],
                    borderColor: '#1976d2',
                    backgroundColor: 'rgba(25, 118, 210, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#1976d2',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                },
                animation: {
                    duration: this.config.animationDuration,
                    delay: this.config.chartAnimationDelay * 2
                }
            }
        });
    }

    refreshAllCharts() {
        Object.values(this.charts).forEach(chart => {
            if (chart && typeof chart.update === 'function') {
                chart.update();
            }
        });
    }

    animateNumberCounters() {
        const statNumbers = document.querySelectorAll('.stat-content h3');
        
        statNumbers.forEach((element, index) => {
            const targetValue = parseInt(element.textContent) || 0;
            const duration = this.config.countUpDuration;
            const startTime = performance.now() + (index * 200);
            
            const animateNumber = (currentTime) => {
                if (currentTime < startTime) {
                    requestAnimationFrame(animateNumber);
                    return;
                }
                
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                
                // Easing function for smooth animation
                const easeOut = 1 - Math.pow(1 - progress, 3);
                const currentValue = Math.floor(targetValue * easeOut);
                
                element.textContent = currentValue.toLocaleString();
                
                if (progress < 1) {
                    requestAnimationFrame(animateNumber);
                }
            };
            
            element.textContent = '0';
            requestAnimationFrame(animateNumber);
        });
    }

    showLoadingIndicator(message = 'Loading...') {
        // Create or update loading overlay
        let overlay = document.getElementById('loadingOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'loadingOverlay';
            overlay.innerHTML = `
                <div class="loading-content">
                    <div class="loading-spinner"></div>
                    <p class="loading-message">${message}</p>
                </div>
            `;
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(255, 255, 255, 0.9);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9999;
                backdrop-filter: blur(2px);
            `;
            document.body.appendChild(overlay);
        } else {
            overlay.querySelector('.loading-message').textContent = message;
        }
        
        overlay.style.display = 'flex';
    }

    hideLoadingIndicator() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
        this.isLoading = false;
    }

    showNotification(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            background: ${type === 'success' ? '#4caf50' : type === 'error' ? '#f44336' : '#2196f3'};
            color: white;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 10000;
            transform: translateX(100%);
            transition: transform 0.3s ease;
            font-weight: 600;
        `;
        
        document.body.appendChild(notification);
        
        // Animate in
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);
        
        // Auto remove
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.remove();
                }
            }, 300);
        }, duration);
    }

    showConfirmDialog(title, message, onConfirm) {
        const result = confirm(`${title}\n\n${message}`);
        if (result && onConfirm) {
            onConfirm();
        }
    }

    exportDataWithProgress() {
        this.showLoadingIndicator('Preparing export...');
        
        // Simulate export process
        setTimeout(() => {
            this.hideLoadingIndicator();
            this.showNotification('Export feature coming soon!', 'info', 3000);
        }, 1500);
    }

    startAutoRefresh() {
        // Auto refresh every 5 minutes
        this.refreshInterval = setInterval(() => {
            this.refreshAllCharts();
            this.showNotification('Dashboard updated automatically', 'info', 2000);
        }, this.config.refreshInterval);
    }

    addLoadingStates() {
        // Add loading states to charts initially
        document.querySelectorAll('.chart-loading').forEach(loading => {
            loading.classList.add('active');
        });
        
        // Remove loading states after charts initialize
        setTimeout(() => {
            document.querySelectorAll('.chart-loading').forEach(loading => {
                loading.classList.remove('active');
            });
        }, 2000);
    }

    enhanceAccessibility() {
        // Add ARIA labels and keyboard navigation
        document.querySelectorAll('.stat-card').forEach((card, index) => {
            card.setAttribute('tabindex', '0');
            card.setAttribute('role', 'button');
            card.setAttribute('aria-label', `Statistics card ${index + 1}`);
            
            card.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.animateStatCard(card);
                }
            });
        });
        
        // Enhance form accessibility
        document.querySelectorAll('input, select').forEach(element => {
            if (!element.getAttribute('aria-label') && element.previousElementSibling?.tagName === 'LABEL') {
                element.setAttribute('aria-describedby', element.previousElementSibling.textContent);
            }
        });
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const dashboard = new ShippingDashboard();
    
    // Make dashboard globally accessible for debugging
    window.shippingDashboard = dashboard;
});
