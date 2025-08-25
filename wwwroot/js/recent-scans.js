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

            console.log('🔍 GetRecentScans Response:', response);
            console.log('📊 Scans Count:', response.data?.recentScans?.length || 0);
            console.log('📈 Total Count:', response.data?.totalCount || 0);

            if (response.success && response.data) {
                this.renderProfessionalScans(response.data);
            } else {
                console.error('❌ API Error:', response.message);
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
        const { recentScans, totalCount, scannedCount, pendingCount, sessionInfo } = data;
        
        if (!recentScans || recentScans.length === 0) {
            this.renderEmpty();
            return;
        }

        console.log('🔍 Rendering scans:', recentScans.length, 'items');
        console.log('📊 Status counts - Done:', scannedCount, 'Todo:', pendingCount, 'Total:', totalCount);

        const scanItemsHtml = recentScans.map((scan, index) => {
            // Extract shortened display text like PDF generation
            const displayText = this.extractBarcodeDisplayText(scan.barcodeValue);
            
            return `
            <div class="recent-scan-item ${scan.itemType.toLowerCase()}" onclick="toggleScanDetails(${index})">
                <div class="recent-scan-left">
                    <div class="recent-scan-barcode">
                        <span class="barcode-text" title="${scan.barcodeValue}">${displayText}</span>
                    </div>
                    <div class="recent-scan-meta">
                        ${scan.isCompleted ? 
                            `✅ Scanned ${this.timeAgo(scan.scannedAt)} ago` : 
                            '⏳ Not scanned yet'
                        }
                    </div>
                </div>
                <div class="recent-scan-type">${scan.itemType}</div>
            </div>`;
        }).join('');

        $(this.containerId).html(`
            <div class="recent-scans-container">
                <div class="recent-scans-header">
                    <span>Recent Scans</span>
                    <span class="recent-scans-badge">${recentScans.length}</span>
                </div>
                <div class="recent-scans-body">
                    ${scanItemsHtml}
                </div>
            </div>
        `);
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
    extractBarcodeDisplayText(fullBarcode) {
        if (!fullBarcode) return 'N/A';

        // Find BOX pattern and extract the meaningful part
        // Example: QR_3_20250825115813_BOX_RP-2400DBG-K_001 -> BOX_RP-2400DBG-K_001
        const boxIndex = fullBarcode.toLowerCase().indexOf('_box_');
        if (boxIndex >= 0) {
            // Return from BOX onwards (skip the first underscore)
            return fullBarcode.substring(boxIndex + 1);
        }

        // Handle other patterns like PALLET or PCS
        const palletIndex = fullBarcode.toLowerCase().indexOf('pallet');
        if (palletIndex >= 0) {
            const parts = fullBarcode.split('_');
            // Find the part with PALLET and return from there
            const palletPartIndex = parts.findIndex(part => part.toLowerCase().includes('pallet'));
            if (palletPartIndex >= 0) {
                return parts.slice(palletPartIndex).join('_');
            }
        }

        const pcsIndex = fullBarcode.toLowerCase().indexOf('pcs');
        if (pcsIndex >= 0) {
            const parts = fullBarcode.split('_');
            // Find the part with PCS and return from there
            const pcsPartIndex = parts.findIndex(part => part.toLowerCase().includes('pcs'));
            if (pcsPartIndex >= 0) {
                return parts.slice(pcsPartIndex).join('_');
            }
        }

        // Fallback: if no specific pattern, try to find last meaningful part
        const parts = fullBarcode.split('_');
        if (parts.length >= 3) {
            // Return last 2-3 parts joined
            const lastParts = parts.slice(-Math.min(3, parts.length));
            return lastParts.join('_');
        }

        // Final fallback: return as is
        return fullBarcode;
    }
}

// 🌟 GLOBAL FUNCTIONS
window.toggleScanDetails = function(index) {
    console.log('Toggle details for item:', index);
};

window.toggleRowBarcode = function(index) {
    const barcodeEl = document.getElementById(`barcode-${index}`);
    const data = window.barcodeToggleData?.[index];
    
    if (!barcodeEl || !data) return;
    
    const isShowingFull = barcodeEl.classList.contains('showing-full');
    
    if (isShowingFull) {
        barcodeEl.textContent = data.short;
        barcodeEl.classList.remove('showing-full');
    } else {
        barcodeEl.textContent = data.full;
        barcodeEl.classList.add('showing-full');
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
