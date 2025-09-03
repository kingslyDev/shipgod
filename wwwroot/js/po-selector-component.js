/**
 * POSelectorComponent - UI Component for PO Selection in Hierarchical Lock System
 * Handles PO selection modal/dropdown dengan progress visualization
 */
class POSelectorComponent {
    constructor(containerId, lockManager) {
        this.container = document.getElementById(containerId);
        this.lockManager = lockManager;
        this.isVisible = false;
        this.selectedPOId = null;
        
        this.callbacks = {
            onPOSelected: [],
            onCancel: []
        };
        
        this.initializeComponent();
        this.bindEvents();
        console.log('🎯 POSelectorComponent initialized');
    }

    /**
     * Initialize component HTML structure
     */
    initializeComponent() {
        this.container.innerHTML = `
            <div class="po-selector-modal" id="poSelectorModal" style="display: none;">
                <div class="modal-backdrop"></div>
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">
                            <i class="fas fa-list-check me-2"></i>
                            Pilih Purchase Order untuk Scanning
                        </h5>
                        <button type="button" class="btn-close" id="closePOSelector">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                    <div class="modal-body">
                        <div class="po-selector-info mb-3">
                            <div class="alert alert-info">
                                <i class="fas fa-info-circle me-2"></i>
                                <strong>Hierarchical Lock System:</strong> Pilih PO untuk melanjutkan scanning. 
                                Anda hanya bisa scan items yang terkait dengan PO yang dipilih.
                            </div>
                        </div>
                        <div class="po-list" id="poList">
                            <!-- PO items will be populated here -->
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" id="cancelPOSelection">
                            <i class="fas fa-times me-2"></i>Cancel
                        </button>
                        <button type="button" class="btn btn-primary" id="confirmPOSelection" disabled>
                            <i class="fas fa-check me-2"></i>Pilih PO
                        </button>
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
            <style id="po-selector-styles">
                .po-selector-modal {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    z-index: 9999;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }

                .po-selector-modal .modal-backdrop {
                    position: absolute;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0, 0, 0, 0.5);
                    cursor: pointer;
                }

                .po-selector-modal .modal-content {
                    position: relative;
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                    max-width: 800px;
                    width: 90%;
                    z-index: 10000;
                    max-height: 80vh;
                    overflow: hidden;
                    animation: modalSlideIn 0.3s ease-out;
                }

                @keyframes modalSlideIn {
                    from {
                        opacity: 0;
                        transform: translateY(-50px) scale(0.9);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0) scale(1);
                    }
                }

                .po-selector-modal .modal-header {
                    padding: 1.5rem;
                    border-bottom: 1px solid #dee2e6;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }

                .po-selector-modal .modal-title {
                    margin: 0;
                    font-size: 1.25rem;
                    font-weight: 600;
                }

                .po-selector-modal .btn-close {
                    background: none;
                    border: none;
                    color: white;
                    font-size: 1.2rem;
                    cursor: pointer;
                    padding: 0.5rem;
                    border-radius: 50%;
                    transition: background-color 0.2s;
                }

                .po-selector-modal .btn-close:hover {
                    background: rgba(255, 255, 255, 0.2);
                }

                .po-selector-modal .modal-body {
                    padding: 1.5rem;
                    max-height: 60vh;
                    overflow-y: auto;
                }

                .po-item {
                    border: 2px solid #e9ecef;
                    border-radius: 8px;
                    padding: 1rem;
                    margin-bottom: 1rem;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    position: relative;
                }

                .po-item:hover {
                    border-color: #007bff;
                    transform: translateY(-2px);
                    box-shadow: 0 4px 12px rgba(0, 123, 255, 0.15);
                }

                .po-item.selected {
                    border-color: #28a745;
                    background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
                }

                .po-item.unavailable {
                    opacity: 0.6;
                    cursor: not-allowed;
                    background: #f8f9fa;
                }

                .po-item.unavailable:hover {
                    transform: none;
                    box-shadow: none;
                    border-color: #e9ecef;
                }

                .po-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: flex-start;
                    margin-bottom: 0.75rem;
                }

                .po-title {
                    font-weight: 600;
                    font-size: 1.1rem;
                    color: #2c3e50;
                    margin: 0;
                }

                .po-status {
                    padding: 0.25rem 0.75rem;
                    border-radius: 20px;
                    font-size: 0.8rem;
                    font-weight: 600;
                    text-transform: uppercase;
                }

                .po-status.available {
                    background: #d4edda;
                    color: #155724;
                }

                .po-status.unavailable {
                    background: #f8d7da;
                    color: #721c24;
                }

                .po-details {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 0.5rem;
                    margin-bottom: 1rem;
                    font-size: 0.9rem;
                    color: #6c757d;
                }

                .po-progress {
                    margin-top: 1rem;
                }

                .progress-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 0.5rem;
                }

                .progress-title {
                    font-weight: 600;
                    font-size: 0.9rem;
                    color: #495057;
                }

                .progress-percentage {
                    font-weight: 600;
                    color: #007bff;
                }

                .progress-bars {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
                    gap: 0.75rem;
                }

                .progress-item {
                    text-align: center;
                }

                .progress-item-label {
                    font-size: 0.75rem;
                    font-weight: 600;
                    color: #6c757d;
                    margin-bottom: 0.25rem;
                    text-transform: uppercase;
                }

                .progress-bar-container {
                    background: #e9ecef;
                    border-radius: 10px;
                    height: 8px;
                    overflow: hidden;
                    margin-bottom: 0.25rem;
                }

                .progress-bar {
                    height: 100%;
                    border-radius: 10px;
                    transition: width 0.3s ease;
                }

                .progress-bar.box { background: linear-gradient(90deg, #ff9a56 0%, #ff6b35 100%); }
                .progress-bar.pallet { background: linear-gradient(90deg, #4facfe 0%, #00f2fe 100%); }
                .progress-bar.pcs { background: linear-gradient(90deg, #43e97b 0%, #38f9d7 100%); }

                .progress-item-count {
                    font-size: 0.75rem;
                    color: #495057;
                }

                .po-item .selection-indicator {
                    position: absolute;
                    top: -1px;
                    right: -1px;
                    width: 24px;
                    height: 24px;
                    background: #28a745;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: white;
                    font-size: 0.8rem;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                }

                .po-item.selected .selection-indicator {
                    opacity: 1;
                }

                .modal-footer {
                    padding: 1rem 1.5rem;
                    border-top: 1px solid #dee2e6;
                    background: #f8f9fa;
                    display: flex;
                    justify-content: flex-end;
                    gap: 0.75rem;
                }
            </style>
        `;

        if (!document.getElementById('po-selector-styles')) {
            document.head.insertAdjacentHTML('beforeend', styles);
        }
    }

    /**
     * Bind component events
     */
    bindEvents() {
        // Delay binding to ensure DOM elements exist
        setTimeout(() => {
            // Close modal events
            const closeBtn = document.getElementById('closePOSelector');
            const cancelBtn = document.getElementById('cancelPOSelection');
            const confirmBtn = document.getElementById('confirmPOSelection');
            const modal = document.getElementById('poSelectorModal');
            
            if (closeBtn) closeBtn.addEventListener('click', () => this.hide());
            if (cancelBtn) cancelBtn.addEventListener('click', () => this.hide());
            if (confirmBtn) confirmBtn.addEventListener('click', () => this.confirmSelection());
            
            // Close on backdrop click - fix selector
            if (modal) {
                const backdrop = modal.querySelector('.modal-backdrop');
                if (backdrop) {
                    backdrop.addEventListener('click', () => this.hide());
                }
            }
            
            // Escape key to close
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this.isVisible) {
                    this.hide();
                }
            });
        }, 100);
    }

    /**
     * Show PO selector dengan available POs
     */
    async show() {
        try {
            console.log('🎯 Showing PO selector...');
            
            // Get available POs dari lock manager
            const pos = this.lockManager.getAvailablePOs();
            if (pos.length === 0) {
                throw new Error('No available POs found');
            }

            this.renderPOList(pos);
            document.getElementById('poSelectorModal').style.display = 'flex';
            this.isVisible = true;
            
            // Focus first available PO
            const firstAvailable = document.querySelector('.po-item:not(.unavailable)');
            if (firstAvailable) {
                firstAvailable.focus();
            }
            
        } catch (error) {
            console.error('❌ Failed to show PO selector:', error);
            this.lockManager.notifyError('PO_SELECTOR_ERROR', error.message);
        }
    }

    /**
     * Hide PO selector
     */
    hide() {
        document.getElementById('poSelectorModal').style.display = 'none';
        this.isVisible = false;
        this.selectedPOId = null;
        this.updateConfirmButton();
        
        // Notify cancel
        this.callbacks.onCancel.forEach(callback => {
            try {
                callback();
            } catch (error) {
                console.error('Cancel callback error:', error);
            }
        });
    }

    /**
     * Render PO list dengan progress visualization
     */
    renderPOList(pos) {
        const container = document.getElementById('poList');
        
        container.innerHTML = pos.map(po => `
            <div class="po-item ${po.isAvailable ? 'available' : 'unavailable'}" 
                 data-po-id="${po.poId}"
                 tabindex="0">
                <div class="selection-indicator">
                    <i class="fas fa-check"></i>
                </div>
                
                <div class="po-header">
                    <h6 class="po-title">${po.noPO}</h6>
                    <span class="po-status ${po.isAvailable ? 'available' : 'unavailable'}">
                        ${po.isAvailable ? 'Available' : 'Unavailable'}
                    </span>
                </div>
                
                <div class="po-details">
                    <div><strong>Model:</strong> ${po.modelProduct}</div>
                    <div><strong>Status:</strong> ${po.status}</div>
                    <div><strong>Next Type:</strong> ${po.nextItemType || 'N/A'}</div>
                    <div><strong>Available Types:</strong> ${po.availableItemTypes.join(', ')}</div>
                </div>
                
                ${po.unavailableReason ? `
                    <div class="alert alert-warning alert-sm">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        ${po.unavailableReason}
                    </div>
                ` : ''}
                
                <div class="po-progress">
                    <div class="progress-header">
                        <span class="progress-title">Progress Overview</span>
                        <span class="progress-percentage">${po.progress.overallProgressPercentage.toFixed(1)}%</span>
                    </div>
                    
                    <div class="progress-bars">
                        ${po.progress.totalBoxes > 0 ? `
                            <div class="progress-item">
                                <div class="progress-item-label">BOX</div>
                                <div class="progress-bar-container">
                                    <div class="progress-bar box" style="width: ${po.progress.boxProgressPercentage}%"></div>
                                </div>
                                <div class="progress-item-count">${po.progress.scannedBoxes}/${po.progress.totalBoxes}</div>
                            </div>
                        ` : ''}
                        
                        ${po.progress.totalPallets > 0 ? `
                            <div class="progress-item">
                                <div class="progress-item-label">PALLET</div>
                                <div class="progress-bar-container">
                                    <div class="progress-bar pallet" style="width: ${po.progress.palletProgressPercentage}%"></div>
                                </div>
                                <div class="progress-item-count">${po.progress.scannedPallets}/${po.progress.totalPallets}</div>
                            </div>
                        ` : ''}
                        
                        ${po.progress.totalPcs > 0 ? `
                            <div class="progress-item">
                                <div class="progress-item-label">PCS</div>
                                <div class="progress-bar-container">
                                    <div class="progress-bar pcs" style="width: ${po.progress.pcsProgressPercentage}%"></div>
                                </div>
                                <div class="progress-item-count">${po.progress.scannedPcs}/${po.progress.totalPcs}</div>
                            </div>
                        ` : ''}
                    </div>
                </div>
            </div>
        `).join('');

        // Bind PO selection events
        container.querySelectorAll('.po-item.available').forEach(item => {
            item.addEventListener('click', () => this.selectPO(item.dataset.poId));
            item.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.selectPO(item.dataset.poId);
                }
            });
        });
    }

    /**
     * Select PO
     */
    selectPO(poId) {
        // Remove previous selection
        document.querySelectorAll('.po-item').forEach(item => {
            item.classList.remove('selected');
        });

        // Add selection to clicked item
        const selectedItem = document.querySelector(`[data-po-id="${poId}"]`);
        if (selectedItem && selectedItem.classList.contains('available')) {
            selectedItem.classList.add('selected');
            this.selectedPOId = parseInt(poId);
            this.updateConfirmButton();
            
            console.log(`🎯 PO ${poId} selected`);
        }
    }

    /**
     * Update confirm button state
     */
    updateConfirmButton() {
        const confirmBtn = document.getElementById('confirmPOSelection');
        confirmBtn.disabled = !this.selectedPOId;
        
        if (this.selectedPOId) {
            confirmBtn.innerHTML = '<i class="fas fa-check me-2"></i>Pilih PO';
        } else {
            confirmBtn.innerHTML = '<i class="fas fa-check me-2"></i>Pilih PO';
        }
    }

    /**
     * Confirm PO selection
     */
    async confirmSelection() {
        if (!this.selectedPOId) return;

        try {
            console.log(`🎯 Confirming PO selection: ${this.selectedPOId}`);
            
            // Use lock manager to select PO
            const result = await this.lockManager.selectPO(this.selectedPOId);
            if (result.success) {
                this.hide();
                
                // Notify callbacks
                this.callbacks.onPOSelected.forEach(callback => {
                    try {
                        callback(result.po);
                    } catch (error) {
                        console.error('PO selected callback error:', error);
                    }
                });
                
                console.log('✅ PO selection confirmed');
            } else {
                throw new Error(result.error || 'Failed to select PO');
            }
            
        } catch (error) {
            console.error('❌ Failed to confirm PO selection:', error);
            alert(`Failed to select PO: ${error.message}`);
        }
    }

    // ===== EVENT SUBSCRIPTION =====

    /**
     * Subscribe to PO selection events
     */
    onPOSelected(callback) {
        this.callbacks.onPOSelected.push(callback);
    }

    /**
     * Subscribe to cancel events
     */
    onCancel(callback) {
        this.callbacks.onCancel.push(callback);
    }

    /**
     * Cleanup component
     */
    cleanup() {
        this.hide();
        const styles = document.getElementById('po-selector-styles');
        if (styles) {
            styles.remove();
        }
        console.log('🧹 POSelectorComponent cleanup completed');
    }
}

// Export for global use
window.POSelectorComponent = POSelectorComponent;
