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
                console.log('✅ Response successful, enhancing with pallet tracking...');
                // Enhance data with automatic pallet entries
                const enhancedData = this.enhanceWithPalletTracking(response.data);
                console.log('✅ Calling renderProfessionalScans...');
                this.renderProfessionalScans(enhancedData);
            } else {
                console.error('❌ API Error:', response.message);
                this.renderEmpty();
            }
        } catch (error) {
            console.error('❌ Failed to load scans:', error);
            this.renderError();
        }
    }

    /**
     * Enhance scan data with automatic pallet tracking based on session PO data
     */
    enhanceWithPalletTracking(data) {
        const { recentScans, totalCount, scannedCount, pendingCount, sessionInfo, sessionMetadata } = data;
        
        if (!recentScans || recentScans.length === 0) {
            return data;
        }

        console.log('🚛 Enhancing with pallet tracking for session:', this.sessionId);
        console.log('📊 Session metadata:', sessionMetadata);
        
        // Use actual database values if available from sessionMetadata
        let totalPallets;
        if (sessionMetadata) {
            // Prioritize actual database pallet count, then estimated count
            totalPallets = sessionMetadata.totalPallets > 0 ? 
                sessionMetadata.totalPallets : 
                sessionMetadata.estimatedPallets || this.estimatePalletCount(recentScans, sessionMetadata);
            
            console.log(`📊 Using session data: ${totalPallets} pallets (DB: ${sessionMetadata.totalPallets}, Est: ${sessionMetadata.estimatedPallets}, Boxes: ${sessionMetadata.totalBoxes})`);
        } else {
            // Fallback to basic estimation
            totalPallets = this.estimatePalletCount(recentScans, null);
            console.log(`📊 Using fallback estimation: ${totalPallets} pallets`);
        }
        
        const scannedPallets = this.countScannedPallets(recentScans);
        
        console.log(`🚛 Pallet tracking: ${scannedPallets}/${totalPallets} pallets`);
        
        // Get actual pallet scans from backend data to mark correct ones as scanned
        const actualPalletScans = recentScans.filter(scan => 
            scan.itemType && scan.itemType.toLowerCase() === 'pallet' && scan.isCompleted
        );
        
        // Generate pallet entries
        const palletEntries = this.generatePalletEntries(totalPallets, scannedPallets, actualPalletScans);
        
        // Combine original scans with pallet entries
        const enhancedScans = [...recentScans, ...palletEntries];
        
        return {
            ...data,
            recentScans: enhancedScans,
            totalCount: totalCount + totalPallets,
            totalPallets: totalPallets,
            scannedPallets: scannedPallets,
            sessionMetadata: sessionMetadata // Pass through for debugging
        };
    }

    /**
     * Estimate pallet count based on available data and actual database values
     */
    estimatePalletCount(scans, sessionInfo) {
        // Look for existing pallet scans to get pattern
        const existingPalletScans = scans.filter(scan => 
            scan.itemType && scan.itemType.toLowerCase().includes('pallet')
        );
        
        if (existingPalletScans.length > 0) {
            // Use highest pallet number found + some buffer
            const maxPalletNum = Math.max(...existingPalletScans.map(scan => {
                const match = scan.barcodeValue.match(/pallet.*?(\d+)/i);
                return match ? parseInt(match[1]) : 1;
            }));
            return Math.max(maxPalletNum, 3); // At least 3 pallets
        }
        
        // Calculate based on total quantity and model configurations
        const boxCount = scans.filter(scan => 
            scan.itemType && scan.itemType.toLowerCase() === 'box'
        ).length;
        
        if (boxCount > 0) {
            // Real calculation based on actual model configurations:
            // From database, we can see:
            // - RF-2400DGN-S: 14 boxes total, PcsPerPallet typically 240 
            // - RF-D10GN-K: 2 boxes total, PcsPerPallet typically 192
            // - RF-P50DGC-S: 1 box total, PcsPerPallet typically 1000
            
            // More realistic pallet calculation based on typical patterns:
            // Large quantities (>50 boxes): 1 pallet per 20-25 boxes
            // Medium quantities (10-50 boxes): 1 pallet per 12-18 boxes  
            // Small quantities (<10 boxes): still need at least 1 pallet for proper stacking
            
            let estimatedPallets;
            if (boxCount >= 50) {
                estimatedPallets = Math.ceil(boxCount / 22); // 1 pallet per ~22 boxes
            } else if (boxCount >= 10) {
                estimatedPallets = Math.ceil(boxCount / 15); // 1 pallet per ~15 boxes
            } else if (boxCount >= 5) {
                estimatedPallets = Math.max(2, Math.ceil(boxCount / 8)); // At least 2 pallets for medium loads
            } else {
                estimatedPallets = Math.max(1, Math.ceil(boxCount / 3)); // At least 1 pallet for small loads
            }
            
            // For the specific case in database:
            // Entry 1: RF-2400DGN-S with 14 boxes -> should be ~1-2 pallets
            // Entry 4: RF-2400DEG-K with 12 boxes -> should be ~1-2 pallets  
            // This gives us realistic 2-4 pallets total for this session
            
            console.log(`🚛 Estimated ${estimatedPallets} pallets for ${boxCount} boxes`);
            return Math.max(estimatedPallets, 2); // Minimum 2 pallets for proper logistics
        }
        
        // If no boxes found, estimate based on session complexity
        // More diverse sessions = more pallets needed
        const uniqueModels = [...new Set(scans.map(s => s.barcodeValue?.split('_')[1] || ''))].length;
        const sessionBasedEstimate = Math.max(2, Math.min(uniqueModels + 1, 8)); // 2-8 pallets based on complexity
        console.log(`🚛 Using model diversity estimate: ${sessionBasedEstimate} pallets for ${uniqueModels} unique models`);
        return sessionBasedEstimate;
    }

    /**
     * Count how many pallets have been scanned based on ScanningActivities database
     */
    countScannedPallets(scans) {
        // Count real pallet scans from database (ScanningActivities)
        // This includes both auto-generated entries and actual pallet scans from database
        const realPalletScans = scans.filter(scan => 
            scan.itemType && 
            scan.itemType.toLowerCase() === 'pallet' && 
            scan.isCompleted === true && // This comes from ScanningActivities lookup
            scan.scannedAt && scan.scannedAt !== '0001-01-01T00:00:00' // Valid scan timestamp
        );
        
        console.log('🚛 Pallet scan analysis:');
        console.log('  - Total scans found:', scans.length);
        
        // Count all pallet items (including tracking and real scans)
        const allPalletItems = scans.filter(s => s.itemType && s.itemType.toLowerCase() === 'pallet');
        console.log('  - Pallet type scans:', allPalletItems.length);
        console.log('  - Completed pallet scans:', realPalletScans.length);
        
        // Debug individual pallet scans
        allPalletItems.forEach(scan => {
            console.log(`    📦 ${scan.barcodeValue}: ${scan.isCompleted ? '✅ SCANNED' : '⏳ PENDING'} (${scan.scannedAt})`);
            if (scan.scannedBy) {
                console.log(`        👤 Scanned by: ${scan.scannedBy}`);
            }
        });
        
        return realPalletScans.length;
    }
    
    /**
     * Generate pallet entries for tracking, but skip ones that already exist from database
     */
    generatePalletEntries(totalPallets, scannedPalletCount, actualPalletScans = []) {
        const palletEntries = [];
        
        // Get list of existing pallet barcodes from backend data
        const existingPalletBarcodes = actualPalletScans.map(scan => 
            scan.barcodeValue.toLowerCase().replace(/[^a-z0-9]/g, '') // normalize: pallet01, Pallet01 → pallet01
        );
        
        console.log('🚛 Existing pallets from database:', existingPalletBarcodes);
        
        for (let i = 1; i <= totalPallets; i++) {
            const palletNumber = i.toString().padStart(2, '0');
            const palletName = `Pallet${palletNumber}`;
            const normalizedPalletName = `pallet${palletNumber}`; // pallet01, pallet02, etc
            
            // Skip if this pallet already exists in real database scans
            if (existingPalletBarcodes.includes(normalizedPalletName)) {
                console.log(`    🔄 Skipping ${palletName} - already exists as real scan`);
                continue;
            }
            
            // Generate placeholder for pallets that haven't been scanned yet
            const isScanned = false; // Only real scans from database should be marked as scanned
            
            palletEntries.push({
                barcodeValue: palletName,
                itemType: 'PALLET',
                isCompleted: isScanned,
                scannedAt: null,
                scannedBy: '',
                isPalletTracking: true, // Flag to identify auto-generated entries
                sequenceNumber: i + 1000, // Higher sequence to put after real scans
                poNumber: 'AUTO-GENERATED',
                modelProduct: 'PALLET',
                description: `Pallet tracking ${palletNumber}`,
                status: 'PENDING'
            });
        }
        
        console.log(`🚛 Generated ${palletEntries.length} placeholder pallet entries (${actualPalletScans.length} real scans + ${palletEntries.length} placeholders = ${totalPallets} total)`);
        return palletEntries;
    }

    renderLoading() {
        console.log('🔍 RENDER LOADING DEBUG: Rendering loading state');
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
        const { recentScans, totalCount, scannedCount, pendingCount, sessionInfo, totalPallets, scannedPallets } = data;
        
        if (!recentScans || recentScans.length === 0) {
            this.renderEmpty();
            return;
        }

    // Cache a shallow copy (exclude pallet tracking placeholders when storing)
    this.currentScans = recentScans.filter(s => !s.isPalletTracking).slice(0, this.maxItems);

        console.log('🔍 Rendering scans:', recentScans.length, 'items');
        console.log('📊 Status counts - Done:', scannedCount, 'Todo:', pendingCount, 'Total:', totalCount);
        if (totalPallets) {
            console.log('🚛 Pallet tracking - Scanned:', scannedPallets, 'Total:', totalPallets);
        }

        // Sort scans: regular scans first, then pallet tracking
        const sortedScans = recentScans.sort((a, b) => {
            if (a.isPalletTracking && !b.isPalletTracking) return 1;
            if (!a.isPalletTracking && b.isPalletTracking) return -1;
            return 0;
        });

        const scanItemsHtml = sortedScans.map((scan, index) => {
            const displayText = scan.isPalletTracking ? 
                scan.barcodeValue : // Show Pallet01, Pallet02, etc as-is
                this.extractBarcodeDisplayText(scan.barcodeValue);
                
            const itemType = scan.itemType.toLowerCase();
            const scannedClass = scan.isCompleted ? 'scanned' : '';
            const trackingClass = scan.isPalletTracking ? 'pallet-tracking' : '';
            
            return `
            <div class="recent-scan-item ${itemType} ${scannedClass} ${trackingClass}" onclick="toggleScanDetails(${index})">
                <div class="recent-scan-left">
                    <div class="recent-scan-barcode">
                        <span class="barcode-text" title="${scan.barcodeValue}">${displayText}</span>
                        ${scan.isPalletTracking ? '<i class="fas fa-truck pallet-icon"></i>' : ''}
                    </div>
                </div>
                <div class="recent-scan-type ${itemType} ${scannedClass}">${scan.itemType}</div>
            </div>`;
        }).join('');

        // Calculate progress statistics
        const boxScans = recentScans.filter(s => s.itemType === 'BOX' && s.isCompleted);
        const palletScans = recentScans.filter(s => s.itemType === 'PALLET' && s.isCompleted);
        const pcsScans = recentScans.filter(s => s.itemType === 'PCS' && s.isCompleted);
        
        const totalBoxItems = recentScans.filter(s => s.itemType === 'BOX').length;
        const totalPalletItems = recentScans.filter(s => s.itemType === 'PALLET').length;
        const totalPcsItems = recentScans.filter(s => s.itemType === 'PCS').length;

        // Create simple progress counters
        const progressCounters = [];
        if (totalPalletItems > 0) {
            progressCounters.push(`pallet ${palletScans.length}/${totalPalletItems}`);
        }
        if (totalBoxItems > 0) {
            progressCounters.push(`box ${boxScans.length}/${totalBoxItems}`);
        }
        if (totalPcsItems > 0) {
            progressCounters.push(`pcs ${pcsScans.length}/${totalPcsItems}`);
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
                <div class="recent-scans-badge">
                    <span>${recentScans.length}</span>
                    <small>items</small>
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
