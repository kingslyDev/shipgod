/**
 * 🚀 PROFESSIONAL RECENT SCANS MANAGER
 * Ultra-modern with model information and professional styling
 */
class RecentScansManager {
    constructor(options = {}) {
        this.containerId = options.containerId || '#recent-scans-container';
        this.sessionId = null;
        this.isVisible = false;
        this.maxItems = options.maxItems || 8;
        this.refreshInterval = null;
        
        this.init();
    }

    init() {
        this.createContainer();
        this.bindEvents();
        console.log('✅ Professional RecentScansManager initialized');
    }

    createContainer() {
        if ($(this.containerId).length === 0) {
            $('#scanStatus').parent().after(`<div id="${this.containerId.replace('#', '')}" style="display: none;"></div>`);
        }
        this.renderEmpty();
    }

    bindEvents() {
        // SignalR real-time updates
        if (window.signalRConnection) {
            window.signalRConnection.on("BarcodeScanned", (data) => {
                if (data.sessionId === this.sessionId && this.isVisible) {
                    // Add smooth animation delay
                    setTimeout(() => this.loadScans(), 300);
                }
            });
            
            // Listen for progress updates to refresh display
            window.signalRConnection.on("ProgressUpdated", (data) => {
                if (data.sessionId === this.sessionId && this.isVisible) {
                    setTimeout(() => this.loadScans(), 500);
                }
            });
        }
    }

    show(sessionId) {
        if (!sessionId) return;
        
        this.sessionId = sessionId;
        this.isVisible = true;
        
        $(this.containerId).slideDown(300, () => {
            this.loadScans();
        });
        
        // Auto refresh every 15 seconds when visible
        this.refreshInterval = setInterval(() => {
            if (this.isVisible) this.loadScans();
        }, 15000);
        
        console.log(`👁️ Professional recent scans shown for session ${sessionId}`);
    }

    hide() {
        this.isVisible = false;
        this.sessionId = null;
        
        // Clear refresh interval
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
        
        $(this.containerId).slideUp(300);
        console.log('👀 Recent scans hidden');
    }

    async loadScans() {
        if (!this.sessionId || !this.isVisible) return;

        try {
            // Add loading state
            this.renderLoading();
            
            const response = await $.get('/Scan/GetRecentScans', { 
                sessionId: this.sessionId, 
                limit: this.maxItems 
            });

            if (response.success && response.data) {
                this.renderProfessionalScans(response.data);
            } else {
                this.renderEmpty();
            }
        } catch (error) {
            console.error('❌ Failed to load scans:', error);
            this.renderError();
        }
    }

    renderLoading() {
        $(this.containerId).html(`
            <div class="recent-scans-container professional">
                <div class="recent-scans-header loading">
                    <div class="header-left">
                        <i class="fas fa-sync-alt fa-spin me-2"></i>
                        <span>Loading Recent Scans...</span>
                    </div>
                </div>
            </div>
        `);
    }

    renderProfessionalScans(data) {
        const { recentScans, totalCount, totalScannedToday, sessionInfo, lastScanTime } = data;
        
        if (!recentScans || recentScans.length === 0) {
            this.renderEmpty();
            return;
        }

        const headerHtml = `
            <div class="recent-scans-header professional">
                <div class="header-left">
                    <i class="fas fa-history me-2 text-primary"></i>
                    <span class="header-title">Recent Scans</span>
                    <span class="scan-badge session">${totalCount}</span>
                </div>
                <div class="header-right">
                    <div class="header-stats">
                        <span class="stat-item">
                            <i class="fas fa-calendar-day me-1"></i>
                            ${totalScannedToday} today
                        </span>
                        <span class="stat-item">
                            <i class="fas fa-clock me-1"></i>
                            ${lastScanTime}
                        </span>
                    </div>
                </div>
            </div>
        `;

        const sessionInfoHtml = sessionInfo ? `
            <div class="session-info">
                <i class="fas fa-file-alt me-2"></i>
                <span>${sessionInfo}</span>
            </div>
        ` : '';

        const itemsHtml = recentScans.map((scan, index) => `
            <div class="professional-scan-item ${scan.itemType.toLowerCase()} ${scan.isCompleted ? 'completed' : 'pending'}" 
                 data-scan-id="${index}" 
                 onclick="toggleScanDetails(${index})">
                <div class="scan-item-header">
                    <div class="scan-left">
                        <div class="scan-icon">
                            <i class="fas fa-${this.getItemIcon(scan.itemType)}"></i>
                        </div>
                        <div class="scan-main-info">
                            <div class="scan-model">
                                <span class="model-name">${scan.displayName || scan.modelProduct}</span>
                                <span class="item-type-badge ${scan.itemType.toLowerCase()}">${scan.itemType}</span>
                            </div>
                            <div class="scan-barcode-info">
                                <div class="barcode-display" onclick="toggleBarcodeView(this)" data-full="${scan.barcodeValue}" title="Click to toggle full barcode view">
                                    <span class="barcode-short">${this.truncateBarcode(scan.barcodeValue)}</span>
                                    <i class="fas fa-expand toggle-icon" title="Toggle full view"></i>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="scan-right">
                        <div class="scan-time">${this.timeAgo(scan.scannedAt)}</div>
                        <div class="scan-status ${scan.status.toLowerCase()}">
                            <i class="fas fa-${scan.isCompleted ? 'check-circle' : 'clock'}"></i>
                            ${scan.status}
                        </div>
                    </div>
                </div>
                
                <div class="scan-details" id="details-${index}" style="display: none;">
                    <div class="detail-row">
                        <span class="detail-label">Barcode:</span>
                        <span class="detail-value barcode-value" onclick="copyBarcode('${scan.barcodeValue}')">
                            ${scan.shortBarcode}
                            <i class="fas fa-copy ms-2 copy-icon" title="Click to copy full barcode"></i>
                        </span>
                    </div>
                    ${scan.description ? `
                        <div class="detail-row">
                            <span class="detail-label">Description:</span>
                            <span class="detail-value">${scan.description}</span>
                        </div>
                    ` : ''}
                    ${scan.container ? `
                        <div class="detail-row">
                            <span class="detail-label">Container:</span>
                            <span class="detail-value">${scan.container}</span>
                        </div>
                    ` : ''}
                    <div class="detail-row">
                        <span class="detail-label">Scanned by:</span>
                        <span class="detail-value">
                            <i class="fas fa-user me-1"></i>
                            ${scan.scannedBy.split('@')[0]}
                        </span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">Full Time:</span>
                        <span class="detail-value">${new Date(scan.scannedAt).toLocaleString()}</span>
                    </div>
                </div>
            </div>
        `).join('');

        const html = `
            <div class="recent-scans-container professional">
                ${headerHtml}
                ${sessionInfoHtml}
                <div class="recent-scans-body">
                    ${itemsHtml}
                </div>
                <div class="recent-scans-footer">
                    <span class="footer-text">
                        <i class="fas fa-info-circle me-1"></i>
                        Click items for details • Auto-refresh enabled
                    </span>
                </div>
            </div>
        `;

        $(this.containerId).html(html);
        
        // Store scan data for interactions
        this.scanData = recentScans;
    }

    renderEmpty() {
        $(this.containerId).html(`
            <div class="recent-scans-container professional empty">
                <div class="recent-scans-header">
                    <div class="header-left">
                        <i class="fas fa-history me-2 text-muted"></i>
                        <span class="header-title">Recent Scans</span>
                        <span class="scan-badge empty">0</span>
                    </div>
                </div>
                <div class="empty-state">
                    <i class="fas fa-barcode empty-icon"></i>
                    <span class="empty-text">No recent scans yet</span>
                    <small class="empty-subtext">Start scanning to see items here</small>
                </div>
            </div>
        `);
    }

    renderError() {
        $(this.containerId).html(`
            <div class="recent-scans-container professional error">
                <div class="recent-scans-header">
                    <div class="header-left">
                        <i class="fas fa-exclamation-triangle me-2 text-warning"></i>
                        <span class="header-title">Recent Scans</span>
                        <span class="scan-badge error">!</span>
                    </div>
                </div>
                <div class="error-state">
                    <i class="fas fa-wifi-off error-icon"></i>
                    <span class="error-text">Failed to load</span>
                    <button onclick="recentScansManager.loadScans()" class="btn btn-sm btn-outline-primary mt-2">
                        <i class="fas fa-refresh me-1"></i>Retry
                    </button>
                </div>
            </div>
        `);
    }

    getItemIcon(itemType) {
        const icons = {
            'BOX': 'box',
            'PALLET': 'pallet',
            'PCS': 'cube'
        };
        return icons[itemType] || 'box';
    }

    timeAgo(date) {
        const diff = Math.floor((new Date() - new Date(date)) / 1000);
        if (diff < 60) return 'Just now';
        if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
        if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
        return `${Math.floor(diff / 86400)}d ago`;
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
        this.hide();
        console.log('🗑️ Professional RecentScansManager destroyed');
    }

    // 🔧 Utility Methods
    truncateBarcode(barcode) {
        if (!barcode) return 'N/A';
        return barcode.length > 15 ? `${barcode.substring(0, 12)}...` : barcode;
    }
}

// 🌟 GLOBAL INTERACTION FUNCTIONS
window.toggleBarcodeView = function(element) {
    const $element = $(element);
    const $shortSpan = $element.find('.barcode-short');
    const $icon = $element.find('.toggle-icon');
    const fullBarcode = $element.data('full');
    
    if (!fullBarcode) return;
    
    const isShowingFull = $element.hasClass('showing-full');
    
    if (isShowingFull) {
        // Show truncated version
        $shortSpan.text(fullBarcode.length > 15 ? `${fullBarcode.substring(0, 12)}...` : fullBarcode);
        $icon.removeClass('fa-compress').addClass('fa-expand');
        $icon.attr('title', 'Click to expand full barcode');
        $element.removeClass('showing-full');
    } else {
        // Show full version
        $shortSpan.text(fullBarcode);
        $icon.removeClass('fa-expand').addClass('fa-compress');
        $icon.attr('title', 'Click to compress barcode');
        $element.addClass('showing-full');
    }
};

window.toggleScanDetails = function(index) {
    const detailsEl = $(`#details-${index}`);
    const isVisible = detailsEl.is(':visible');
    
    // Close all other details first
    $('.scan-details').slideUp(200);
    
    if (!isVisible) {
        detailsEl.slideDown(300);
    }
};

window.copyBarcode = function(barcode) {
    navigator.clipboard.writeText(barcode).then(() => {
        // Show success animation
        $('.copy-icon').removeClass('fa-copy').addClass('fa-check text-success');
        setTimeout(() => {
            $('.copy-icon').removeClass('fa-check text-success').addClass('fa-copy');
        }, 2000);
        
        // Show toast notification
        if (window.showAlert) {
            window.showAlert(`📋 Barcode copied: ${barcode}`, 'success');
        }
    }).catch(err => {
        console.error('Failed to copy:', err);
        if (window.showAlert) {
            window.showAlert('❌ Failed to copy barcode', 'danger');
        }
    });
};

window.RecentScansManager = RecentScansManager;
