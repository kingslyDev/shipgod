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
    this.currentScans = []; // cache of last rendered scans for quick prepend
        
        this.init();
    }

    init() {
        this.createContainer();
        this.bindEvents();
        console.log('✅ Professional RecentScansManager initialized');
    }

    createContainer() {
        console.log('🔍 CREATE CONTAINER DEBUG: Checking if container exists...');
        console.log('🔍 Container selector:', this.containerId);
        console.log('🔍 Container exists:', $(this.containerId).length > 0);
        
        if ($(this.containerId).length === 0) {
            console.log('⚠️ Container not found, creating one...');
            $('#scanStatus').parent().after(`<div id="${this.containerId.replace('#', '')}" class="recent-scans-container" style="display: none;"></div>`);
        } else {
            console.log('✅ Container found, using existing one');
        }
        this.renderEmpty();
    }

    bindEvents() {
        // SignalR real-time updates
        if (window.signalRConnection) {
            window.signalRConnection.on("BarcodeScanned", (data) => {
                if (data.sessionId === this.sessionId && this.isVisible) {
                    // Update last scan time for activity tracking
                    this.lastScanTime = Date.now();
                    console.log('🔄 New scan detected - immediate refresh');
                    // Add smooth animation delay
                    setTimeout(() => this.loadScans(), 300);
                }
            });
            
            // Listen for progress updates to refresh display
            window.signalRConnection.on("ProgressUpdated", (data) => {
                if (data.sessionId === this.sessionId && this.isVisible) {
                    this.lastScanTime = Date.now();
                    console.log('🔄 Progress updated - refresh');
                    setTimeout(() => this.loadScans(), 500);
                }
            });
        }
    }

    show(sessionId) {
        console.log('🔍 RECENT SCANS DEBUG: show() called with sessionId:', sessionId);
        console.log('🔍 Container element exists:', $(this.containerId).length > 0);
        console.log('🔍 Container current display:', $(this.containerId).css('display'));
        
        if (!sessionId) {
            console.warn('❌ No sessionId provided to show()');
            return;
        }
        
        this.sessionId = sessionId;
        this.isVisible = true;
        this.lastScanTime = null; // Track last scan time
        
        console.log('🔍 About to slideDown container...');
        $(this.containerId).slideDown(300, () => {
            console.log('✅ SlideDown completed, calling loadScans()');
            this.loadScans();
        });
        
        // Optimized refresh - less frequent background refresh
        this.refreshInterval = setInterval(() => {
            if (this.isVisible && !this.hasRecentActivity()) {
                console.log('🔄 Background refresh check');
                this.loadScans();
            }
        }, 30000); // Every 30 seconds instead of 15
        
        console.log(`👁️ Professional recent scans shown for session ${sessionId}`);
    }
    
    /**
     * Check if there was recent scanning activity (within last 30 seconds)
     */
    hasRecentActivity() {
        if (!this.lastScanTime) return false;
        const now = Date.now();
        const thirtySecondsAgo = now - (30 * 1000);
        return this.lastScanTime > thirtySecondsAgo;
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
        console.log('🔍 LOADSCANS DEBUG: Starting loadScans()');
        console.log('🔍 SessionId:', this.sessionId, 'IsVisible:', this.isVisible);
        
        if (!this.sessionId || !this.isVisible) {
            console.warn('❌ LoadScans aborted - missing sessionId or not visible');
            return;
        }

        try {
            // Add loading state
            console.log('🔍 Rendering loading state...');
            this.renderLoading();
            
            console.log('🔍 Making AJAX call to /Scan/GetRecentScans...');
            const response = await $.get('/Scan/GetRecentScans', { 
                sessionId: this.sessionId, 
                limit: this.maxItems 
            });

            console.log('🔍 GetRecentScans Response:', response);
            console.log('📊 Scans Count:', response.data?.recentScans?.length || 0);
            console.log('📈 Total Count:', response.data?.totalCount || 0);

            if (response.success && response.data) {
                // Render as-is using credible stats from backend
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
        console.log('� RENDER LOADING DEBUG: Rendering loading state');
        $(this.containerId).html(`
            <div class="recent-scans-header loading">
                <div class="header-left">
                    <i class="fas fa-sync-alt fa-spin me-2"></i>
                    <span>Loading Recent Scans...</span>
                </div>
            </div>
        `);
        console.log('✅ Loading state rendered');
    }

    renderProfessionalScans(data) {
        const { recentScans, totalCount, scannedCount, pendingCount, sessionInfo, stats } = data;
        
        if (!recentScans || recentScans.length === 0) {
            this.renderEmpty();
            return;
        }

        // Cache a shallow copy of what we render
        this.currentScans = recentScans.slice(0, this.maxItems);

        console.log('🔍 Rendering scans:', recentScans.length, 'items');
        console.log('📊 Status counts - Done:', scannedCount, 'Todo:', pendingCount, 'Total:', totalCount);

        // Keep order as returned
        const sortedScans = recentScans;

        const scanItemsHtml = sortedScans.map((scan, index) => {
            const displayText = this.extractBarcodeDisplayText(scan.barcodeValue);
                
            const itemType = scan.itemType.toLowerCase();
            const scannedClass = scan.isCompleted ? 'scanned' : '';
            
            return `
            <div class="recent-scan-item ${itemType} ${scannedClass}" onclick="toggleScanDetails(${index})">
                <div class="recent-scan-left">
                    <div class="recent-scan-barcode">
                        <span class="barcode-text" title="${scan.barcodeValue}">${displayText}</span>
                    </div>
                </div>
                <div class="recent-scan-type ${itemType} ${scannedClass}">${scan.itemType}</div>
            </div>`;
        }).join('');

        // Calculate progress statistics
        const boxScans = recentScans.filter(s => s.itemType === 'BOX' && s.isCompleted);
        const palletScans = recentScans.filter(s => s.itemType === 'PALLET' && s.isCompleted);
        const pcsScans = recentScans.filter(s => s.itemType === 'PCS' && s.isCompleted);
        
        // Use credible totals from stats; fall back to visible recent list counts
        const totalBoxItems = (stats && typeof stats.boxTotal === 'number') ? stats.boxTotal : recentScans.filter(s => s.itemType === 'BOX').length;
        const totalPalletItems = (stats && typeof stats.palletTotal === 'number') ? stats.palletTotal : recentScans.filter(s => s.itemType === 'PALLET').length;
        const totalPcsItems = (stats && typeof stats.pcsTotal === 'number') ? stats.pcsTotal : recentScans.filter(s => s.itemType === 'PCS').length;

        // Create simple progress counters
        const progressCounters = [];
        if (totalPalletItems > 0) {
            const palletDone = (stats && typeof stats.palletScanned === 'number') ? stats.palletScanned : palletScans.length;
            progressCounters.push(`pallet ${palletDone}/${totalPalletItems}`);
        }
        if (totalBoxItems > 0) {
            const boxDone = (stats && typeof stats.boxScanned === 'number') ? stats.boxScanned : boxScans.length;
            progressCounters.push(`box ${boxDone}/${totalBoxItems}`);
        }
        if (totalPcsItems > 0) {
            const pcsDone = (stats && typeof stats.pcsScanned === 'number') ? stats.pcsScanned : pcsScans.length;
            progressCounters.push(`pcs ${pcsDone}/${totalPcsItems}`);
        }
        
        const progressText = progressCounters.length > 0 ? 
            `<small class="progress-text">${progressCounters.join(', ')}</small>` : '';

        console.log('🔍 RENDER PROFESSIONAL SCANS DEBUG: Rendering', recentScans.length, 'scans');
        $(this.containerId).html(`
            <div class="recent-scans-header">
                <div class="header-main">
                    <span><i class="fas fa-history me-2"></i>Recent Scans</span>
                    ${progressText}
                </div>
                <div class="recent-scans-badge" title="Scanned/Total (Items shown are latest only)">
                    <span>${scannedCount}/${totalCount}</span>
                    <small>scanned</small>
                </div>
            </div>
            <div class="recent-scans-body">
                ${scanItemsHtml}
            </div>
        `);
        console.log('✅ Professional scans rendered with progress counters');
    }

    renderEmpty() {
        console.log('🔍 RENDER EMPTY DEBUG: Rendering empty state');
        $(this.containerId).html(`
            <div class="recent-scans-header">
                <span><i class="fas fa-history me-2"></i>Recent Scans</span>
                <div class="recent-scans-badge">
                    <span>0</span>
                    <small>items</small>
                </div>
            </div>
            <div class="recent-scans-empty">
                <i class="fas fa-barcode empty-icon"></i>
                <div class="empty-text">No recent scans yet</div>
                <div class="empty-subtext">Start scanning to see items appear here</div>
            </div>
        `);
        console.log('✅ Empty state rendered');
    }

    renderError() {
        console.log('🔍 RENDER ERROR DEBUG: Rendering error state');
        $(this.containerId).html(`
            <div class="recent-scans-header error">
                <div class="header-main">
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
        `);
        console.log('✅ Error state rendered');
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

    /**
     * 🚀 Add a freshly scanned item instantly without waiting for server refresh
     * Keeps UX responsive especially for PCS scans (%Q*)
     */
    addImmediateScan(barcodeValue, scanType) {
        try {
            if (!this.isVisible || !this.sessionId) return;

            // Normalize type
            const upperType = (scanType || '').toUpperCase();
            const detectedType = upperType || (barcodeValue.startsWith('%Q') ? 'PCS' : 'ITEM');

            // Avoid duplicates (already present at top)
            if (this.currentScans.length > 0 && this.currentScans[0].barcodeValue === barcodeValue) {
                return;
            }

            const newScan = {
                barcodeValue: barcodeValue,
                itemType: detectedType,
                isCompleted: true,
                scannedAt: new Date().toISOString(),
                isPalletTracking: false
            };

            // Prepend in cache
            this.currentScans.unshift(newScan);
            // Trim to maxItems
            if (this.currentScans.length > this.maxItems) this.currentScans.pop();

            // Re-render lightweight list (without full reload) preserving existing DOM header
            const bodyEl = $(this.containerId).find('.recent-scans-body');
            if (bodyEl.length === 0) {
                // Fallback full reload
                this.loadScans();
                return;
            }

            const displayText = this.extractBarcodeDisplayText(barcodeValue);
            const itemTypeClass = detectedType.toLowerCase();
            const newItemHtml = `
            <div class="recent-scan-item ${itemTypeClass} scanned" style="display:none;">
                <div class="recent-scan-left">
                    <div class="recent-scan-barcode">
                        <span class="barcode-text" title="${barcodeValue}">${displayText}</span>
                    </div>
                </div>
                <div class="recent-scan-type ${itemTypeClass} scanned">${detectedType}</div>
            </div>`;

            bodyEl.prepend(newItemHtml);
            const inserted = bodyEl.children().first();
            inserted.slideDown(120).addClass('flash-highlight');
            setTimeout(() => inserted.removeClass('flash-highlight'), 1500);

            // Update badge count (items count excludes pallet placeholders)
            $(this.containerId).find('.recent-scans-badge span:first').text(this.currentScans.length);
        } catch (e) {
            console.warn('addImmediateScan failed, falling back to loadScans()', e);
            this.loadScans();
        }
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
