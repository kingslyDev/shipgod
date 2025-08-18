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
        this.animationQueue = [];
        
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
        this.filters.country = urlParams.get('country') || '';
        this.filters.fromDate = urlParams.get('fromDate') || '';
        this.filters.toDate = urlParams.get('toDate') || '';
        this.filters.status = urlParams.get('status') || '';

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
        document.getElementById('filterForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.applyFiltersWithAnimation();
        });

        // Clear filters with confirmation for baby boomers
        document.getElementById('clearFilters').addEventListener('click', () => {
            this.clearFiltersWithConfirmation();
        });

        // Manual refresh with visual feedback
        document.getElementById('refreshData').addEventListener('click', () => {
            this.refreshDataWithAnimation();
        });

        // Chart refresh buttons
        document.querySelectorAll('.chart-refresh').forEach(button => {
            button.addEventListener('click', (e) => {
                const chartType = e.target.dataset.chart;
                this.refreshChartWithAnimation(chartType);
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
        this.filters.country = document.getElementById('countryFilter').value;
        this.filters.fromDate = document.getElementById('dateFromFilter').value;
        this.filters.toDate = document.getElementById('dateToFilter').value;
        this.filters.status = document.getElementById('statusFilter').value;

        // Animate filter application
        const filterForm = document.getElementById('filterForm');
        filterForm.style.opacity = '0.7';
        filterForm.style.pointerEvents = 'none';

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
        const originalIcon = refreshBtn.innerHTML;
        
        // Animate refresh button
        refreshBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Refreshing...';
        refreshBtn.disabled = true;
        
        this.showLoadingIndicator('Refreshing dashboard data...');
        
        // Refresh charts and data
        this.refreshAllCharts();
        
        setTimeout(() => {
            window.location.reload();
        }, 1000);
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
        `;
        
        card.style.position = 'relative';
        card.appendChild(ripple);
        
        // Remove ripple after animation
        setTimeout(() => {
            ripple.remove();
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
            this.clearFilters();
        });

        // Auto-apply filters on change (debounced)
        const filterInputs = document.querySelectorAll('.filter-input');
        filterInputs.forEach(input => {
            input.addEventListener('change', () => {
                this.debounce(() => this.applyFilters(), 500)();
            });
        });

        // Refresh button
        document.getElementById('refreshDashboard')?.addEventListener('click', () => {
            this.refreshData();
        });
    }

    applyFilters() {
        this.showLoading();
        
        // Get filter values
        this.filters.country = document.getElementById('countryFilter').value;
        this.filters.fromDate = document.getElementById('fromDateFilter').value;
        this.filters.toDate = document.getElementById('toDateFilter').value;
        this.filters.status = document.getElementById('statusFilter').value;

        // Build URL with filters
        const params = new URLSearchParams();
        if (this.filters.country) params.set('country', this.filters.country);
        if (this.filters.fromDate) params.set('fromDate', this.filters.fromDate);
        if (this.filters.toDate) params.set('toDate', this.filters.toDate);
        if (this.filters.status) params.set('status', this.filters.status);

        // Reload page with filters
        window.location.href = `/Shipping/Dashboard?${params.toString()}`;
    }

    clearFilters() {
        document.getElementById('countryFilter').value = '';
        document.getElementById('fromDateFilter').value = '';
        document.getElementById('toDateFilter').value = '';
        document.getElementById('statusFilter').value = '';
        
        // Redirect to dashboard without filters
        window.location.href = '/Shipping/Dashboard';
    }

    async refreshData() {
        this.showLoading();
        
        try {
            // Refresh statistics
            await this.updateStatistics();
            
            // Refresh charts
            await this.updateCharts();
            
            // Refresh recent activity
            await this.updateRecentActivity();
            
            this.hideLoading();
            this.showSuccessMessage('Dashboard updated successfully');
        } catch (error) {
            this.hideLoading();
            this.showErrorMessage('Failed to refresh dashboard data');
            console.error('Refresh error:', error);
        }
    }

    async updateStatistics() {
        const params = this.buildFilterParams();
        const response = await fetch(`/Shipping/GetStatistics?${params}`);
        const data = await response.json();
        
        if (data.success) {
            // Update stat cards
            document.querySelector('[data-stat="total"] .stat-value').textContent = data.data.total;
            document.querySelector('[data-stat="completed"] .stat-value').textContent = data.data.completed;
            document.querySelector('[data-stat="processing"] .stat-value').textContent = data.data.processing;
            document.querySelector('[data-stat="pending"] .stat-value').textContent = data.data.pending;
            
            // Update completion rate
            const completionRate = data.data.completionRate.toFixed(1);
            document.querySelector('[data-stat="completion"] .stat-value').textContent = `${completionRate}%`;
        }
    }

    async updateCharts() {
        const params = this.buildFilterParams();
        const response = await fetch(`/Shipping/GetChartData?${params}`);
        const data = await response.json();
        
        if (data.success) {
            // Update status chart
            if (this.charts.statusChart) {
                this.charts.statusChart.data.datasets[0].data = data.statusChart.data;
                this.charts.statusChart.update();
            }
            
            // Update country chart
            if (this.charts.countryChart) {
                this.charts.countryChart.data.labels = data.countryChart.labels;
                this.charts.countryChart.data.datasets[0].data = data.countryChart.sessions;
                this.charts.countryChart.data.datasets[1].data = data.countryChart.boxes;
                this.charts.countryChart.update();
            }
            
            // Update monthly chart
            if (this.charts.monthlyChart) {
                this.charts.monthlyChart.data.labels = data.monthlyChart.labels;
                this.charts.monthlyChart.data.datasets[0].data = data.monthlyChart.sessions;
                this.charts.monthlyChart.data.datasets[1].data = data.monthlyChart.completed;
                this.charts.monthlyChart.update();
            }
        }
    }

    async updateRecentActivity() {
        const response = await fetch('/Shipping/GetRecentActivity');
        const data = await response.json();
        
        if (data.success) {
            const tbody = document.querySelector('#recentActivityTable tbody');
            tbody.innerHTML = '';
            
            data.data.slice(0, 10).forEach(activity => {
                const row = this.createActivityRow(activity);
                tbody.appendChild(row);
            });
        }
    }

    initializeCharts() {
        this.initStatusChart();
        this.initCountryChart();
        this.initMonthlyChart();
    }

    initStatusChart() {
        const ctx = document.getElementById('statusChart');
        if (!ctx) return;

        this.charts.statusChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Completed', 'Processing', 'Pending', 'Others'],
                datasets: [{
                    data: [0, 0, 0, 0], // Will be updated by API
                    backgroundColor: [
                        '#4caf50',
                        '#ff9800',
                        '#00bcd4',
                        '#6c757d'
                    ],
                    borderWidth: 2,
                    borderColor: '#ffffff'
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
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed * 100) / total).toFixed(1);
                                return `${context.label}: ${context.parsed} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    initCountryChart() {
        const ctx = document.getElementById('countryChart');
        if (!ctx) return;

        this.charts.countryChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: [],
                datasets: [{
                    label: 'Sessions',
                    data: [],
                    backgroundColor: '#1976d2',
                    yAxisID: 'y'
                }, {
                    label: 'Boxes',
                    data: [],
                    backgroundColor: '#4caf50',
                    yAxisID: 'y1'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: 'Sessions'
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: 'Boxes'
                        },
                        grid: {
                            drawOnChartArea: false,
                        }
                    }
                },
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        });
    }

    initMonthlyChart() {
        const ctx = document.getElementById('monthlyChart');
        if (!ctx) return;

        this.charts.monthlyChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Total Sessions',
                    data: [],
                    borderColor: '#1976d2',
                    backgroundColor: 'rgba(25, 118, 210, 0.1)',
                    fill: true,
                    tension: 0.4
                }, {
                    label: 'Completed Sessions',
                    data: [],
                    borderColor: '#4caf50',
                    backgroundColor: 'rgba(76, 175, 80, 0.1)',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Number of Sessions'
                        }
                    }
                },
                plugins: {
                    legend: {
                        position: 'top'
                    }
                }
            }
        });
    }

    createActivityRow(activity) {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <span class="status-badge ${this.getStatusClass(activity.result)}">
                    ${activity.action}
                </span>
            </td>
            <td><code>${activity.barcode || 'N/A'}</code></td>
            <td>${activity.user || 'System'}</td>
            <td>${this.formatTimestamp(activity.timestamp)}</td>
        `;
        return row;
    }

    getStatusClass(result) {
        switch(result?.toLowerCase()) {
            case 'success': return 'completed';
            case 'error': return 'error';
            case 'pending': return 'pending';
            default: return 'processing';
        }
    }

    formatTimestamp(timestamp) {
        return new Date(timestamp).toLocaleString('id-ID', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    buildFilterParams() {
        const params = new URLSearchParams();
        if (this.filters.country) params.set('country', this.filters.country);
        if (this.filters.fromDate) params.set('fromDate', this.filters.fromDate);
        if (this.filters.toDate) params.set('toDate', this.filters.toDate);
        if (this.filters.status) params.set('status', this.filters.status);
        return params.toString();
    }

    startAutoRefresh() {
        // Refresh every 5 minutes
        this.refreshInterval = setInterval(() => {
            this.updateStatistics();
            this.updateRecentActivity();
        }, 300000);
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    showLoading() {
        // Add loading overlay
        const loading = document.createElement('div');
        loading.id = 'loadingOverlay';
        loading.className = 'loading-overlay';
        loading.innerHTML = `
            <div class="loading-spinner">
                <i class="fas fa-spinner fa-spin fa-2x"></i>
                <p>Updating dashboard...</p>
            </div>
        `;
        document.body.appendChild(loading);
    }

    hideLoading() {
        const loading = document.getElementById('loadingOverlay');
        if (loading) {
            loading.remove();
        }
    }

    addLoadingStates() {
        // Add loading skeletons to stat cards
        document.querySelectorAll('.stat-card').forEach(card => {
            card.addEventListener('mouseenter', () => {
                card.style.transform = 'translateY(-3px)';
            });
            card.addEventListener('mouseleave', () => {
                card.style.transform = 'translateY(0)';
            });
        });
    }

    showSuccessMessage(message) {
        this.showToast(message, 'success');
    }

    showErrorMessage(message) {
        this.showToast(message, 'error');
    }

    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
            <span>${message}</span>
        `;
        
        document.body.appendChild(toast);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Cleanup when page unloads
    destroy() {
        this.stopAutoRefresh();
        
        // Destroy charts
        Object.values(this.charts).forEach(chart => {
            if (chart && typeof chart.destroy === 'function') {
                chart.destroy();
            }
        });
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.shippingDashboard = new ShippingDashboard();
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (window.shippingDashboard) {
        window.shippingDashboard.destroy();
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ShippingDashboard;
}
