/**
 * EnhancedRecentScansComponent - Recent Scans with PO Filtering and Hierarchical Lock Support
 * Implements real-time updates, PO-specific grouping, dan progress visualization
 */
class EnhancedRecentScansComponent {
    constructor(containerId, lockManager) {
        this.container = document.getElementById(containerId);
        this.lockManager = lockManager;
        this.currentSessionId = null;
        this.currentPOId = null;
        this.recentScans = [];
        this.refreshTimer = null;
        this.isAutoRefreshEnabled = true;
        
        this.config = {
            refreshInterval: 15000, // 15 seconds
            maxItems: 50,
            groupByPO: true
        };
        
        this.initializeComponent();
        this.bindEvents();
        this.startAutoRefresh();
        console.log('📋 EnhancedRecentScansComponent initialized');
    }

    /**
     * Initialize component structure
     */
    initializeComponent() {
        this.container.innerHTML = `
            <div class="recent-scans-container">
                <div class="recent-scans-header">
                    <div class="header-left">
                        <h5 class="component-title">
                            <i class="fas fa-history me-2"></i>
                            Recent Scans
                        </h5>
                        <div class="scan-stats" id="scanStats">
                            <span class="stat-item">
                                <i class="fas fa-check-circle text-success me-1"></i>
                                <span id="scannedCount">0</span> Scanned
                            </span>
                            <span class="stat-item">
                                <i class="fas fa-clock text-warning me-1"></i>
                                <span id="pendingCount">0</span> Pending
                            </span>
                        </div>
                    </div>
                    <div class="header-right">
                        <div class="filter-controls">
                            <select class="form-select form-select-sm" id="poFilter">
                                <option value="">All POs</option>
                            </select>
                            <button class="btn btn-sm btn-outline-primary" id="refreshScans" title="Refresh">
                                <i class="fas fa-sync-alt"></i>
                            </button>
                            <button class="btn btn-sm btn-outline-secondary" id="toggleAutoRefresh" title="Toggle Auto Refresh">
                                <i class="fas fa-play"></i>
                            </button>
                        </div>
                    </div>
                </div>
                
                <div class="recent-scans-body">
                    <div class="loading-indicator" id="loadingIndicator" style="display: none;">
                        <div class="spinner-border spinner-border-sm me-2" role="status"></div>
                        Loading recent scans...
                    </div>
                    
                    <div class="scans-list" id="scansList">
                        <!-- Scans will be populated here -->
                    </div>
                    
                    <div class="empty-state" id="emptyState" style="display: none;">
                        <div class="text-center py-4">
                            <i class="fas fa-inbox fa-3x text-muted mb-3"></i>
                            <h6 class="text-muted">No scans found</h6>
                            <p class="text-muted mb-0">Start scanning to see recent activities</p>
                        </div>
                    </div>
                </div>
            </div>
        `;

        this.addStyles();
    }

    /**
     * Add component styles
     */
    addStyles() {
        const styles = `
            <style id="enhanced-recent-scans-styles">
                .recent-scans-container {
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
                    overflow: hidden;
                }

                .recent-scans-header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 1rem 1.25rem;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    flex-wrap: wrap;
                    gap: 1rem;
                }

                .header-left .component-title {
                    margin: 0 0 0.5rem 0;
                    font-size: 1.1rem;
                    font-weight: 600;
                }

                .scan-stats {
                    display: flex;
                    gap: 1rem;
                    font-size: 0.9rem;
                }

                .stat-item {
                    display: flex;
                    align-items: center;
                    background: rgba(255, 255, 255, 0.2);
                    padding: 0.25rem 0.75rem;
                    border-radius: 20px;
                    font-weight: 500;
                }

                .filter-controls {
                    display: flex;
                    gap: 0.5rem;
                    align-items: center;
                }

                .filter-controls select {
                    min-width: 150px;
                    background: white;
                    border: none;
                    font-size: 0.9rem;
                }

                .filter-controls .btn {
                    background: rgba(255, 255, 255, 0.2);
                    border: 1px solid rgba(255, 255, 255, 0.3);
                    color: white;
                    transition: all 0.2s;
                }

                .filter-controls .btn:hover {
                    background: rgba(255, 255, 255, 0.3);
                    border-color: rgba(255, 255, 255, 0.5);
                    color: white;
                }

                .recent-scans-body {
                    max-height: 500px;
                    overflow-y: auto;
                    position: relative;
                }

                .loading-indicator {
                    padding: 2rem;
                    text-align: center;
                    color: #6c757d;
                    background: #f8f9fa;
                }

                .scans-list {
                    padding: 0;
                }

                .po-group {
                    border-bottom: 1px solid #e9ecef;
                }

                .po-group:last-child {
                    border-bottom: none;
                }

                .po-group-header {
                    background: #f8f9fa;
                    padding: 0.75rem 1.25rem;
                    border-bottom: 1px solid #dee2e6;
                    position: sticky;
                    top: 0;
                    z-index: 10;
                }

                .po-group-title {
                    margin: 0;
                    font-size: 1rem;
                    font-weight: 600;
                    color: #495057;
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                }

                .po-progress-mini {
                    font-size: 0.8rem;
                    color: #6c757d;
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                }

                .progress-mini {
                    width: 60px;
                    height: 4px;
                    background: #e9ecef;
                    border-radius: 2px;
                    overflow: hidden;
                }

                .progress-mini .progress-bar {
                    height: 100%;
                    background: linear-gradient(90deg, #28a745 0%, #20c997 100%);
                    transition: width 0.3s ease;
                }

                .scan-item {
                    padding: 0.75rem 1.25rem;
                    border-bottom: 1px solid #f1f3f4;
                    transition: background-color 0.2s;
                    position: relative;
                }

                .scan-item:hover {
                    background: #f8f9fa;
                }

                .scan-item:last-child {
                    border-bottom: none;
                }

                .scan-item.completed {
                    background: linear-gradient(90deg, rgba(40, 167, 69, 0.05) 0%, rgba(32, 201, 151, 0.05) 100%);
                }

                .scan-item.pending {
                    background: linear-gradient(90deg, rgba(255, 193, 7, 0.05) 0%, rgba(255, 154, 86, 0.05) 100%);
                }

                .scan-item-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: flex-start;
                    margin-bottom: 0.5rem;
                }

                .scan-item-title {
                    font-weight: 600;
                    color: #2c3e50;
                    margin: 0;
                    font-size: 0.95rem;
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                }

                .item-type-badge {
                    padding: 0.2rem 0.5rem;
                    border-radius: 12px;
                    font-size: 0.7rem;
                    font-weight: 600;
                    text-transform: uppercase;
                }

                .item-type-badge.box {
                    background: linear-gradient(135deg, #ff9a56 0%, #ff6b35 100%);
                    color: white;
                }

                .item-type-badge.pallet {
                    background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
                    color: white;
                }

                .item-type-badge.pcs {
                    background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
                    color: white;
                }

                .scan-status {
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                    font-size: 0.8rem;
                    font-weight: 600;
                }

                .status-indicator {
                    width: 8px;
                    height: 8px;
                    border-radius: 50%;
                }

                .status-indicator.completed {
                    background: #28a745;
                    box-shadow: 0 0 0 3px rgba(40, 167, 69, 0.2);
                }

                .status-indicator.pending {
                    background: #ffc107;
                    box-shadow: 0 0 0 3px rgba(255, 193, 7, 0.2);
                    animation: pulse 2s infinite;
                }

                @keyframes pulse {
                    0% { opacity: 1; }
                    50% { opacity: 0.5; }
                    100% { opacity: 1; }
                }

                .scan-item-details {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
                    gap: 0.5rem;
                    font-size: 0.8rem;
                    color: #6c757d;
                }

                .detail-item {
                    display: flex;
                    flex-direction: column;
                }

                .detail-label {
                    font-weight: 600;
                    margin-bottom: 0.1rem;
                    text-transform: uppercase;
                    font-size: 0.7rem;
                    color: #9ca3af;
                }

                .detail-value {
                    color: #374151;
                }

                .sequence-number {
                    position: absolute;
                    right: 0.75rem;
                    top: 50%;
                    transform: translateY(-50%);
                    background: #e9ecef;
                    color: #6c757d;
                    padding: 0.25rem 0.5rem;
                    border-radius: 12px;
                    font-size: 0.7rem;
                    font-weight: 600;
                }

                .scan-item.completed .sequence-number {
                    background: #d4edda;
                    color: #155724;
                }

                .auto-refresh-indicator {
                    display: inline-flex;
                    align-items: center;
                    gap: 0.25rem;
                    font-size: 0.7rem;
                    color: rgba(255, 255, 255, 0.8);
                }

                .auto-refresh-indicator.active {
                    color: #28a745;
                }

                .auto-refresh-indicator .fa-circle {
                    font-size: 0.5rem;
                    animation: blink 1.5s infinite;
                }

                @keyframes blink {
                    0%, 50% { opacity: 1; }
                    51%, 100% { opacity: 0.3; }
                }

                /* Responsive */
                @media (max-width: 768px) {
                    .recent-scans-header {
                        flex-direction: column;
                        align-items: flex-start;
                    }
                    
                    .filter-controls {
                        width: 100%;
                        justify-content: space-between;
                    }
                    
                    .filter-controls select {
                        flex: 1;
                        min-width: auto;
                    }
                    
                    .scan-item-details {
                        grid-template-columns: 1fr;
                    }
                    
                    .sequence-number {
                        position: static;
                        transform: none;
                        margin-top: 0.5rem;
                        align-self: flex-start;
                    }
                }
            </style>
        `;

        if (!document.getElementById('enhanced-recent-scans-styles')) {
            document.head.insertAdjacentHTML('beforeend', styles);
        }
    }

    /**
     * Bind component events
     */
    bindEvents() {
        // Refresh button
        document.getElementById('refreshScans').addEventListener('click', () => {
            this.refreshScans();
        });

        // Auto refresh toggle
        document.getElementById('toggleAutoRefresh').addEventListener('click', () => {
            this.toggleAutoRefresh();
        });

        // PO filter change
        document.getElementById('poFilter').addEventListener('change', (e) => {
            this.currentPOId = e.target.value ? parseInt(e.target.value) : null;
            this.refreshScans();
        });

        // Lock manager state changes
        this.lockManager.onStateChange((event, data) => {
            this.handleLockManagerStateChange(event, data);
        });

        // Lock manager PO selection
        this.lockManager.onPOSelection((po) => {
            this.handlePOSelection(po);
        });
    }

    /**
     * Handle lock manager state changes
     */
    handleLockManagerStateChange(event, data) {
        console.log('📋 Recent scans handling state change:', event, data);
        
        switch (event) {
            case 'SESSION_INITIALIZED':
                this.currentSessionId = data.sessionId;
                this.updatePOFilter(data.availablePOs);
                this.refreshScans();
                break;
                
            case 'PO_SELECTED':
                this.currentPOId = data.selectedPO.id;
                this.updatePOFilterSelection();
                this.refreshScans();
                break;
                
            case 'SESSION_RESET':
                this.currentSessionId = null;
                this.currentPOId = null;
                this.clearScans();
                break;
        }
    }

    /**
     * Handle PO selection
     */
    handlePOSelection(po) {
        this.currentPOId = po.id;
        this.updatePOFilterSelection();
        this.refreshScans();
    }

    /**
     * Update PO filter dropdown
     */
    updatePOFilter(availablePOs) {
        const filter = document.getElementById('poFilter');
        
        filter.innerHTML = '<option value="">All POs</option>' + 
            availablePOs.map(po => `
                <option value="${po.poId}">${po.noPO} (${po.modelProduct})</option>
            `).join('');
    }

    /**
     * Update PO filter selection
     */
    updatePOFilterSelection() {
        const filter = document.getElementById('poFilter');
        filter.value = this.currentPOId || '';
    }

    /**
     * Start auto refresh timer
     */
    startAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
        }

        this.refreshTimer = setInterval(() => {
            if (this.isAutoRefreshEnabled && this.currentSessionId) {
                this.refreshScans(true); // Silent refresh
            }
        }, this.config.refreshInterval);

        this.updateAutoRefreshIndicator();
    }

    /**
     * Stop auto refresh
     */
    stopAutoRefresh() {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
            this.refreshTimer = null;
        }
        this.updateAutoRefreshIndicator();
    }

    /**
     * Toggle auto refresh
     */
    toggleAutoRefresh() {
        this.isAutoRefreshEnabled = !this.isAutoRefreshEnabled;
        
        if (this.isAutoRefreshEnabled) {
            this.startAutoRefresh();
        } else {
            this.stopAutoRefresh();
        }

        this.updateAutoRefreshIndicator();
    }

    /**
     * Update auto refresh indicator
     */
    updateAutoRefreshIndicator() {
        const btn = document.getElementById('toggleAutoRefresh');
        const icon = btn.querySelector('i');
        
        if (this.isAutoRefreshEnabled) {
            icon.className = 'fas fa-pause';
            btn.title = 'Pause Auto Refresh';
            btn.classList.add('active');
        } else {
            icon.className = 'fas fa-play';
            btn.title = 'Start Auto Refresh';
            btn.classList.remove('active');
        }
    }

    /**
     * Refresh scans data
     */
    async refreshScans(silent = false) {
        if (!this.currentSessionId) {
            this.clearScans();
            return;
        }

        try {
            if (!silent) {
                this.showLoading();
            }

            console.log(`📋 Refreshing scans for session ${this.currentSessionId}, PO: ${this.currentPOId || 'All'}`);

            const url = this.currentPOId 
                ? `/Scan/GetRecentScansEnhanced?sessionId=${this.currentSessionId}&poId=${this.currentPOId}&limit=${this.config.maxItems}`
                : `/Scan/GetRecentScansEnhanced?sessionId=${this.currentSessionId}&limit=${this.config.maxItems}`;

            const response = await fetch(url, {
                method: 'GET',
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            if (result.success) {
                this.recentScans = result.data.recentScans || [];
                this.renderScans(result.data);
                this.updateStats(result.data);
            } else {
                throw new Error(result.message || 'Failed to fetch recent scans');
            }

        } catch (error) {
            console.error('📋 Failed to refresh scans:', error);
            this.showError(`Failed to load recent scans: ${error.message}`);
        } finally {
            this.hideLoading();
        }
    }

    /**
     * Render scans list
     */
    renderScans(data) {
        const container = document.getElementById('scansList');
        
        if (!this.recentScans || this.recentScans.length === 0) {
            this.showEmptyState();
            return;
        }

        this.hideEmptyState();

        if (this.config.groupByPO && !this.currentPOId) {
            this.renderGroupedScans(container, data);
        } else {
            this.renderFlatScans(container, data);
        }
    }

    /**
     * Render scans grouped by PO
     */
    renderGroupedScans(container, data) {
        // Group scans by PO
        const groupedScans = this.recentScans.reduce((groups, scan) => {
            const key = `${scan.poNumber}_${scan.modelProduct}`;
            if (!groups[key]) {
                groups[key] = {
                    poNumber: scan.poNumber,
                    modelProduct: scan.modelProduct,
                    scans: []
                };
            }
            groups[key].scans.push(scan);
            return groups;
        }, {});

        container.innerHTML = Object.values(groupedScans).map(group => `
            <div class="po-group">
                <div class="po-group-header">
                    <div class="po-group-title">
                        <span>
                            <i class="fas fa-box me-2"></i>
                            ${group.poNumber} - ${group.modelProduct}
                        </span>
                        <div class="po-progress-mini">
                            ${this.renderProgressMini(group.scans)}
                            <span>${group.scans.filter(s => s.isCompleted).length}/${group.scans.length}</span>
                        </div>
                    </div>
                </div>
                <div class="po-scans">
                    ${group.scans.map(scan => this.renderScanItem(scan)).join('')}
                </div>
            </div>
        `).join('');
    }

    /**
     * Render scans in flat list
     */
    renderFlatScans(container, data) {
        container.innerHTML = this.recentScans.map(scan => this.renderScanItem(scan)).join('');
    }

    /**
     * Render individual scan item
     */
    renderScanItem(scan) {
        const statusClass = scan.isCompleted ? 'completed' : 'pending';
        const statusText = scan.isCompleted ? 'Scanned' : 'Pending';
        const scanTime = scan.isCompleted && scan.scannedAt !== '0001-01-01T00:00:00' 
            ? new Date(scan.scannedAt).toLocaleString('id-ID', {
                day: '2-digit',
                month: '2-digit', 
                hour: '2-digit',
                minute: '2-digit'
              })
            : 'Not scanned';

        return `
            <div class="scan-item ${statusClass}">
                <div class="scan-item-header">
                    <h6 class="scan-item-title">
                        <span class="item-type-badge ${scan.itemType.toLowerCase()}">${scan.itemType}</span>
                        ${scan.shortBarcode || scan.barcodeValue}
                    </h6>
                    <div class="scan-status">
                        <div class="status-indicator ${statusClass}"></div>
                        ${statusText}
                    </div>
                </div>
                
                <div class="scan-item-details">
                    <div class="detail-item">
                        <div class="detail-label">PO Number</div>
                        <div class="detail-value">${scan.poNumber}</div>
                    </div>
                    <div class="detail-item">
                        <div class="detail-label">Model</div>
                        <div class="detail-value">${scan.modelProduct}</div>
                    </div>
                    <div class="detail-item">
                        <div class="detail-label">Scan Time</div>
                        <div class="detail-value">${scanTime}</div>
                    </div>
                    ${scan.scannedBy ? `
                        <div class="detail-item">
                            <div class="detail-label">Scanned By</div>
                            <div class="detail-value">${scan.scannedBy}</div>
                        </div>
                    ` : ''}
                </div>
                
                ${scan.sequenceNumber ? `
                    <div class="sequence-number">#${scan.sequenceNumber}</div>
                ` : ''}
            </div>
        `;
    }

    /**
     * Render progress mini indicator
     */
    renderProgressMini(scans) {
        const completed = scans.filter(s => s.isCompleted).length;
        const total = scans.length;
        const percentage = total > 0 ? (completed / total) * 100 : 0;

        return `
            <div class="progress-mini">
                <div class="progress-bar" style="width: ${percentage}%"></div>
            </div>
        `;
    }

    /**
     * Update statistics
     */
    updateStats(data) {
        document.getElementById('scannedCount').textContent = data.scannedCount || 0;
        document.getElementById('pendingCount').textContent = data.pendingCount || 0;
    }

    /**
     * Show loading state
     */
    showLoading() {
        document.getElementById('loadingIndicator').style.display = 'block';
        document.getElementById('scansList').style.display = 'none';
        document.getElementById('emptyState').style.display = 'none';
    }

    /**
     * Hide loading state
     */
    hideLoading() {
        document.getElementById('loadingIndicator').style.display = 'none';
        document.getElementById('scansList').style.display = 'block';
    }

    /**
     * Show empty state
     */
    showEmptyState() {
        document.getElementById('scansList').style.display = 'none';
        document.getElementById('emptyState').style.display = 'block';
    }

    /**
     * Hide empty state
     */
    hideEmptyState() {
        document.getElementById('emptyState').style.display = 'none';
        document.getElementById('scansList').style.display = 'block';
    }

    /**
     * Clear scans
     */
    clearScans() {
        this.recentScans = [];
        document.getElementById('scansList').innerHTML = '';
        this.updateStats({ scannedCount: 0, pendingCount: 0 });
        this.showEmptyState();
    }

    /**
     * Show error message
     */
    showError(message) {
        const container = document.getElementById('scansList');
        container.innerHTML = `
            <div class="alert alert-danger mx-3 my-3">
                <i class="fas fa-exclamation-triangle me-2"></i>
                ${message}
            </div>
        `;
    }

    /**
     * Cleanup component
     */
    cleanup() {
        this.stopAutoRefresh();
        const styles = document.getElementById('enhanced-recent-scans-styles');
        if (styles) {
            styles.remove();
        }
        console.log('🧹 EnhancedRecentScansComponent cleanup completed');
    }
}

// Export for global use
window.EnhancedRecentScansComponent = EnhancedRecentScansComponent;
