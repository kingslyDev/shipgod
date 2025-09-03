// Hierarchical Lock Management for Scanner
class HierarchicalLockManager {
    constructor() {
        this.currentSessionLock = null;
        this.currentPOLock = null;
        this.availablePOs = [];
        this.isInitialized = false;
        
        // Bind methods to maintain context
        this.initializeUI = this.initializeUI.bind(this);
        this.checkLockStatus = this.checkLockStatus.bind(this);
        this.showPOSelection = this.showPOSelection.bind(this);
        this.hidePOSelection = this.hidePOSelection.bind(this);
        this.lockToPO = this.lockToPO.bind(this);
        this.unlockFromPO = this.unlockFromPO.bind(this);
        this.processHierarchicalScan = this.processHierarchicalScan.bind(this);
    }

    async initialize() {
        console.log('🚀 Initializing Hierarchical Lock Manager...');
        
        // Initialize UI components
        this.initializeUI();
        
        // Check current lock status
        await this.checkLockStatus();
        
        // Set up event listeners
        this.setupEventListeners();
        
        this.isInitialized = true;
        console.log('✅ Hierarchical Lock Manager initialized');
    }

    initializeUI() {
        // Create PO Selection Modal if it doesn't exist
        if (!document.getElementById('poSelectionModal')) {
            const modalHtml = `
                <div class="modal fade" id="poSelectionModal" tabindex="-1" aria-labelledby="poSelectionModalLabel" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-primary text-white">
                                <h5 class="modal-title" id="poSelectionModalLabel">
                                    <i class="fas fa-list-ul me-2"></i>Select PO to Scan
                                </h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body" id="poSelectionBody">
                                <!-- PO List will be populated here -->
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', modalHtml);
        }

        // Create Hierarchical Status Display
        if (!document.getElementById('hierarchicalStatus')) {
            const statusHtml = `
                <div id="hierarchicalStatus" class="card mb-3" style="display: none;">
                    <div class="card-header bg-info text-white">
                        <h6 class="mb-0">
                            <i class="fas fa-sitemap me-2"></i>Hierarchical Lock Status
                        </h6>
                    </div>
                    <div class="card-body p-2">
                        <div class="row">
                            <div class="col-md-6">
                                <div id="sessionLockStatus" class="lock-status-item">
                                    <small class="text-muted">Session Lock:</small>
                                    <div id="sessionLockInfo" class="fw-bold">Not Locked</div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div id="poLockStatus" class="lock-status-item">
                                    <small class="text-muted">PO Lock:</small>
                                    <div id="poLockInfo" class="fw-bold">Not Locked</div>
                                </div>
                            </div>
                        </div>
                        <div class="row mt-2">
                            <div class="col-12">
                                <div class="d-flex gap-2">
                                    <button id="selectPOBtn" class="btn btn-outline-primary btn-sm" style="display: none;" onclick="hierarchicalLock.showPOSelection()">
                                        <i class="fas fa-list me-1"></i>Select PO
                                    </button>
                                    <button id="unlockPOBtn" class="btn btn-outline-warning btn-sm" style="display: none;" onclick="hierarchicalLock.unlockFromPO()">
                                        <i class="fas fa-unlock me-1"></i>Unlock PO
                                    </button>
                                    <button id="emergencyUnlockBtn" class="btn btn-outline-danger btn-sm" style="display: none;" onclick="hierarchicalLock.emergencyUnlock()">
                                        <i class="fas fa-exclamation-triangle me-1"></i>Emergency Unlock
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            
            // Insert after scanner card
            const scannerCard = document.getElementById('scannerCard');
            if (scannerCard) {
                scannerCard.parentNode.insertBefore(
                    document.createRange().createContextualFragment(statusHtml).firstChild,
                    scannerCard.nextSibling
                );
            }
        }
    }

    setupEventListeners() {
        // Override the original processScan function
        if (typeof window.originalProcessScan === 'undefined') {
            window.originalProcessScan = window.processScan;
            window.processScan = () => {
                this.processHierarchicalScan();
            };
        }
    }

    async checkLockStatus() {
        try {
            const response = await fetch('/Scan/GetHierarchicalLockStatus', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            if (result.success && result.data) {
                this.updateLockStatus(result.data);
            }
        } catch (error) {
            console.error('Error checking lock status:', error);
        }
    }

    updateLockStatus(lockStatus) {
        this.currentSessionLock = lockStatus.sessionLock;
        this.currentPOLock = lockStatus.poLock;
        this.availablePOs = lockStatus.availablePOs || [];

        const statusCard = document.getElementById('hierarchicalStatus');
        const sessionInfo = document.getElementById('sessionLockInfo');
        const poInfo = document.getElementById('poLockInfo');
        const selectPOBtn = document.getElementById('selectPOBtn');
        const unlockPOBtn = document.getElementById('unlockPOBtn');
        const emergencyBtn = document.getElementById('emergencyUnlockBtn');

        if (lockStatus.isSessionLocked) {
            statusCard.style.display = 'block';
            sessionInfo.innerHTML = `
                <span class="badge bg-success">
                    <i class="fas fa-lock me-1"></i>
                    ${this.currentSessionLock?.sessionName || 'Session Locked'}
                </span>
            `;
            
            if (lockStatus.isPOLocked) {
                poInfo.innerHTML = `
                    <span class="badge bg-warning">
                        <i class="fas fa-lock me-1"></i>
                        ${this.currentPOLock?.noPO || 'PO Locked'}
                    </span>
                `;
                selectPOBtn.style.display = 'none';
                unlockPOBtn.style.display = 'inline-block';
            } else {
                poInfo.innerHTML = `
                    <span class="badge bg-secondary">
                        <i class="fas fa-unlock me-1"></i>
                        Select PO
                    </span>
                `;
                selectPOBtn.style.display = 'inline-block';
                unlockPOBtn.style.display = 'none';
            }
            
            emergencyBtn.style.display = 'inline-block';
        } else {
            statusCard.style.display = 'none';
            sessionInfo.textContent = 'Not Locked';
            poInfo.textContent = 'Not Locked';
            selectPOBtn.style.display = 'none';
            unlockPOBtn.style.display = 'none';
            emergencyBtn.style.display = 'none';
        }
    }

    async showPOSelectionModal(sessionId = null) {
        console.log('🎯 showPOSelectionModal called with sessionId:', sessionId);
        
        // Update current session if provided
        if (sessionId && !this.currentSessionLock) {
            // Create a temporary session lock object for the modal
            this.currentSessionLock = { sessionId: sessionId };
            console.log('📝 Temporary session lock created for modal');
        }
        
        if (!this.currentSessionLock) {
            showAlert('You must be locked to a session first', 'warning');
            return;
        }

        try {
            console.log('🔍 Fetching available POs for session:', this.currentSessionLock.sessionId);
            
            // Get available POs for the current session
            const response = await fetch(`/Scan/GetAvailablePOs?sessionId=${this.currentSessionLock.sessionId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            console.log('📦 PO fetch result:', result);
            
            if (result.success && result.data) {
                console.log('✅ POs loaded successfully, count:', result.data.length);
                this.renderPOSelection(result.data);
                
                // Show the modal
                const modalElement = document.getElementById('poSelectionModal');
                if (modalElement) {
                    const modal = new bootstrap.Modal(modalElement);
                    modal.show();
                    console.log('📋 PO Selection Modal shown');
                } else {
                    console.error('❌ PO Selection Modal element not found');
                }
            } else {
                console.error('❌ Error loading POs:', result.message);
                showAlert('Error loading POs: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('❌ Exception in showPOSelectionModal:', error);
            showAlert('Error showing PO selection', 'danger');
        }
    }

    renderPOSelection(pos) {
        const tbody = document.getElementById('poSelectionBody');
        
        if (pos.length === 0) {
            tbody.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-inbox fa-3x text-muted mb-3"></i>
                    <h5 class="text-muted">No POs Available</h5>
                    <p class="text-muted">All POs in this session have been completed.</p>
                </div>
            `;
            return;
        }

        let html = `
            <div class="table-responsive">
                <table class="table table-hover">
                    <thead class="table-light">
                        <tr>
                            <th>PO Number</th>
                            <th>Model Product</th>
                            <th>Items</th>
                            <th>Progress</th>
                            <th>Status</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        pos.forEach(po => {
            const itemTypes = [];
            if (po.hasBoxes) itemTypes.push(`📦 ${po.qtyBox} Box`);
            if (po.hasPallets) itemTypes.push(`🚛 ${po.qtyPallet} Pallet`);
            if (po.hasPcs) itemTypes.push(`🔢 ${po.qtyPcs} PCS`);

            const progressClass = po.progress >= 100 ? 'bg-success' : po.progress >= 50 ? 'bg-warning' : 'bg-info';
            const statusBadge = po.isCompleted ? 
                '<span class="badge bg-success">Completed</span>' : 
                '<span class="badge bg-primary">Available</span>';

            html += `
                <tr ${po.isCompleted ? 'class="table-secondary"' : ''}>
                    <td>
                        <strong>${po.noPO}</strong>
                        ${po.container ? `<br><small class="text-muted">${po.container}</small>` : ''}
                    </td>
                    <td>
                        ${po.modelProduk}
                        ${po.shipmentDetail ? `<br><small class="text-muted">${po.shipmentDetail}</small>` : ''}
                    </td>
                    <td>
                        <small>${itemTypes.join('<br>')}</small>
                    </td>
                    <td>
                        <div class="progress" style="height: 20px;">
                            <div class="progress-bar ${progressClass}" role="progressbar" style="width: ${po.progress}%">
                                ${po.progress.toFixed(1)}%
                            </div>
                        </div>
                        <small class="text-muted">
                            ${po.scannedBoxes + po.scannedPallets + po.scannedPcs}/${po.qtyBox + po.qtyPallet + po.qtyPcs} items
                        </small>
                    </td>
                    <td>${statusBadge}</td>
                    <td>
                        ${po.isAvailable && !po.isCompleted ? 
                            `<button class="btn btn-primary btn-sm" onclick="hierarchicalLock.lockToPO(${po.poId}, '${po.noPO}')">
                                <i class="fas fa-lock me-1"></i>Select
                            </button>` :
                            `<button class="btn btn-secondary btn-sm" disabled>
                                <i class="fas fa-check me-1"></i>Completed
                            </button>`
                        }
                    </td>
                </tr>
            `;
        });

        html += `
                    </tbody>
                </table>
            </div>
        `;

        tbody.innerHTML = html;
    }

    async lockToPO(poId, noPO) {
        try {
            const response = await fetch('/Scan/LockToPO', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify({ poId: poId })
            });

            const result = await response.json();
            if (result.success) {
                // Hide modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('poSelectionModal'));
                modal.hide();

                // Update lock status
                await this.checkLockStatus();

                // Show success message
                showAlert(`Successfully locked to PO: ${noPO}`, 'success');

                // Focus on scan input
                document.getElementById('mainScanInput').focus();
            } else {
                showAlert('Error locking to PO: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('Error locking to PO:', error);
            showAlert('Error locking to PO', 'danger');
        }
    }

    async unlockFromPO() {
        if (!this.currentPOLock) {
            showAlert('No PO lock to unlock', 'warning');
            return;
        }

        if (!confirm(`Are you sure you want to unlock from PO: ${this.currentPOLock.noPO}?`)) {
            return;
        }

        try {
            const response = await fetch('/Scan/UnlockFromPO', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify({ poLockId: this.currentPOLock.poLockId })
            });

            const result = await response.json();
            if (result.success) {
                // Update lock status
                await this.checkLockStatus();

                showAlert('Successfully unlocked from PO', 'success');
            } else {
                showAlert('Error unlocking from PO: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('Error unlocking from PO:', error);
            showAlert('Error unlocking from PO', 'danger');
        }
    }

    async emergencyUnlock() {
        if (!confirm('🚨 EMERGENCY UNLOCK\n\nThis will unlock you from all sessions and POs.\nAre you sure you want to proceed?')) {
            return;
        }

        try {
            const response = await fetch('/Scan/EmergencyUnlockHierarchical', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                }
            });

            const result = await response.json();
            if (result.success) {
                // Update lock status
                await this.checkLockStatus();

                showAlert('Emergency unlock successful', 'success');
            } else {
                showAlert('Error during emergency unlock: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('Error during emergency unlock:', error);
            showAlert('Error during emergency unlock', 'danger');
        }
    }

    async processHierarchicalScan() {
        const input = document.getElementById('mainScanInput');
        const scannedValue = input.val ? input.val().trim() : input.value.trim();
        
        if (!scannedValue) {
            showAlert('Please enter or scan a value', 'warning');
            if (input.focus) input.focus();
            return;
        }

        console.log('🔍 HIERARCHICAL SCAN:', scannedValue);

        // Step 1: Check if it's a Master QR (for session lock)
        if (this.isMasterQR(scannedValue)) {
            await this.processMasterQRScan(scannedValue);
        }
        // Step 2: If user has session but no PO lock, show PO selection
        else if (this.currentSessionLock && !this.currentPOLock) {
            showAlert('Please select a PO first before scanning items', 'warning');
            this.showPOSelection();
        }
        // Step 3: If user has PO lock, scan the item
        else if (this.currentPOLock) {
            await this.processItemScan(scannedValue);
        }
        // Step 4: No locks at all - scan Master QR first
        else {
            showAlert('Please scan Master QR first to lock to a session', 'warning');
        }

        // Clear input
        if (input.val) input.val('').focus();
        else { input.value = ''; input.focus(); }
    }

    isMasterQR(value) {
        return value.startsWith('QR_') && !value.includes('_BOX_') && !value.includes('PALLET') && !value.startsWith('%Q');
    }

    async processMasterQRScan(qrCode) {
        try {
            // Find session that matches this QR
            const session = activeSessions.find(s => s.qrIdentity === qrCode);
            if (!session) {
                showAlert('Invalid Master QR code', 'danger');
                return;
            }

            const response = await fetch('/Scan/LockToSession', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify({ 
                    sessionId: session.sessionId, 
                    qrCode: qrCode 
                })
            });

            const result = await response.json();
            if (result.success) {
                // Update lock status
                await this.checkLockStatus();

                showAlert(`Successfully locked to session: ${result.data?.sessionName || 'Session'}`, 'success');
                
                // Show PO selection after successful session lock
                setTimeout(() => {
                    this.showPOSelection();
                }, 1000);
            } else {
                showAlert('Error locking to session: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('Error processing Master QR scan:', error);
            showAlert('Error scanning Master QR', 'danger');
        }
    }

    async processItemScan(barcode) {
        try {
            const response = await fetch('/Scan/ScanItemHierarchical', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: JSON.stringify({ barcode: barcode })
            });

            const result = await response.json();
            if (result.success) {
                showAlert(result.message || 'Item scanned successfully', 'success');

                // Update progress if available
                if (result.data?.progressUpdate) {
                    this.updateProgressDisplay(result.data.progressUpdate);
                }

                // Check for auto-unlock notifications
                if (result.data?.poCompleted) {
                    setTimeout(() => {
                        showAlert(`🎉 PO ${this.currentPOLock?.noPO} completed! You can now select another PO.`, 'success');
                        this.checkLockStatus(); // Refresh lock status
                    }, 1500);
                }

                if (result.data?.sessionCompleted) {
                    setTimeout(() => {
                        showAlert('🎉 All POs completed! Session finished successfully.', 'success');
                        this.checkLockStatus(); // Refresh lock status
                    }, 2000);
                }
            } else {
                showAlert('Error scanning item: ' + (result.message || 'Unknown error'), 'danger');
            }
        } catch (error) {
            console.error('Error processing item scan:', error);
            showAlert('Error scanning item', 'danger');
        }
    }

    updateProgressDisplay(progressUpdate) {
        // Update existing progress displays if they exist
        if (typeof updateEnhancedProgress === 'function') {
            updateEnhancedProgress(progressUpdate);
        }

        // Also update hierarchical status if needed
        if (this.currentPOLock) {
            this.currentPOLock.poProgress = progressUpdate.overallProgress;
            this.currentPOLock.isCompleted = progressUpdate.isFullyComplete;
        }
    }

    // Method to check if user is in hierarchical mode (session locked)
    isInHierarchicalMode() {
        return this.currentSessionLock && this.currentSessionLock.isActive;
    }

    // Method to get current PO lock
    getCurrentPOLock() {
        return this.currentPOLock && this.currentPOLock.isActive ? this.currentPOLock : null;
    }

    // Method to show PO selection modal
    showPOSelectionModal(sessionId = null) {
        const modal = document.getElementById('poSelectionModal');
        if (modal) {
            // If sessionId provided, use it, otherwise use current session
            const targetSessionId = sessionId || (this.currentSessionLock ? this.currentSessionLock.sessionId : null);
            
            if (targetSessionId) {
                this.showPOSelection(targetSessionId);
            } else {
                console.error('No session ID available for PO selection');
                showAlert('No active session found for PO selection', 'danger');
            }
        }
    }

    // Method to update status display
    updateStatusDisplay() {
        // Update hierarchical lock status card
        this.updateLockStatus({
            sessionLock: this.currentSessionLock,
            poLock: this.currentPOLock,
            availablePOs: this.availablePOs
        });
    }

    // Method for emergency unlock all
    emergencyUnlockAll() {
        if (confirm('Are you sure you want to unlock all hierarchical locks? This will reset your session.')) {
            this.emergencyUnlock();
        }
    }
}

// Global instance
let hierarchicalLock = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Wait for existing scanner initialization to complete
    setTimeout(() => {
        hierarchicalLock = new HierarchicalLockManager();
        hierarchicalLock.initialize();
        
        // Make it globally available
        window.hierarchicalLock = hierarchicalLock;
    }, 1000);
});

// Add CSS for hierarchical lock
const hierarchicalLockStyles = `
<style>
.lock-status-item {
    padding: 0.5rem;
    border-radius: 0.375rem;
    background-color: #f8f9fa;
    margin-bottom: 0.5rem;
}

.lock-status-item:last-child {
    margin-bottom: 0;
}

#hierarchicalStatus .badge {
    font-size: 0.875rem;
    padding: 0.375rem 0.75rem;
}

#poSelectionModal .table th {
    border-top: none;
    font-weight: 600;
    color: #495057;
}

#poSelectionModal .progress {
    background-color: #e9ecef;
}

.table-secondary {
    opacity: 0.6;
}

.modal-dialog-centered {
    align-items: center;
}

@media (max-width: 768px) {
    #hierarchicalStatus .row > div {
        margin-bottom: 0.5rem;
    }
    
    #hierarchicalStatus .d-flex {
        flex-direction: column;
    }
    
    #hierarchicalStatus .btn {
        margin-bottom: 0.25rem;
        width: 100%;
    }
}
</style>
`;

document.head.insertAdjacentHTML('beforeend', hierarchicalLockStyles);
