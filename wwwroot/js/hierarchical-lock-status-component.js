/**
 * HierarchicalLockStatusComponent - Visual Status Indicators for Lock System
 * Shows current lock level, session status, PO progress, and hierarchical progression
 */
class HierarchicalLockStatusComponent {
    constructor(containerId, lockManager) {
        this.container = document.getElementById(containerId);
        this.lockManager = lockManager;
        this.currentState = {
            lockLevel: 'NONE',
            sessionId: null,
            selectedPO: null,
            progress: null
        };
        
        this.initializeComponent();
        this.bindEvents();
        console.log('📊 HierarchicalLockStatusComponent initialized');
    }

    /**
     * Initialize component structure
     */
    initializeComponent() {
        this.container.innerHTML = `
            <div class="lock-status-container">
                <div class="status-header">
                    <h5 class="status-title">
                        <i class="fas fa-shield-alt me-2"></i>
                        Hierarchical Lock Status
                    </h5>
                    <div class="status-indicator" id="overallStatus">
                        <span class="status-dot"></span>
                        <span class="status-text">Not Active</span>
                    </div>
                </div>
                
                <div class="status-body">
                    <!-- Lock Level Progression -->
                    <div class="lock-progression">
                        <div class="progression-steps">
                            <div class="step-item" data-step="master">
                                <div class="step-icon">
                                    <i class="fas fa-qrcode"></i>
                                </div>
                                <div class="step-content">
                                    <div class="step-title">Master QR</div>
                                    <div class="step-subtitle">Session Lock</div>
                                </div>
                                <div class="step-connector"></div>
                            </div>
                            
                            <div class="step-item" data-step="po">
                                <div class="step-icon">
                                    <i class="fas fa-list-check"></i>
                                </div>
                                <div class="step-content">
                                    <div class="step-title">PO Selection</div>
                                    <div class="step-subtitle">Context Lock</div>
                                </div>
                                <div class="step-connector"></div>
                            </div>
                            
                            <div class="step-item" data-step="scanning">
                                <div class="step-icon">
                                    <i class="fas fa-barcode"></i>
                                </div>
                                <div class="step-content">
                                    <div class="step-title">Item Scanning</div>
                                    <div class="step-subtitle">Active Scanning</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Session Information -->
                    <div class="session-info" id="sessionInfo" style="display: none;">
                        <div class="info-card">
                            <div class="info-header">
                                <i class="fas fa-database me-2"></i>
                                Session Information
                            </div>
                            <div class="info-content">
                                <div class="info-row">
                                    <span class="info-label">Session ID:</span>
                                    <span class="info-value" id="sessionIdValue">-</span>
                                </div>
                                <div class="info-row">
                                    <span class="info-label">Status:</span>
                                    <span class="info-value" id="sessionStatusValue">-</span>
                                </div>
                                <div class="info-row">
                                    <span class="info-label">Total POs:</span>
                                    <span class="info-value" id="totalPOsValue">-</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- PO Information -->
                    <div class="po-info" id="poInfo" style="display: none;">
                        <div class="info-card">
                            <div class="info-header">
                                <i class="fas fa-box me-2"></i>
                                Selected PO Information
                            </div>
                            <div class="info-content">
                                <div class="info-row">
                                    <span class="info-label">PO Number:</span>
                                    <span class="info-value" id="poNumberValue">-</span>
                                </div>
                                <div class="info-row">
                                    <span class="info-label">Model:</span>
                                    <span class="info-value" id="poModelValue">-</span>
                                </div>
                                <div class="info-row">
                                    <span class="info-label">Next Type:</span>
                                    <span class="info-value badge" id="nextItemTypeValue">-</span>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Progress Information -->
                    <div class="progress-info" id="progressInfo" style="display: none;">
                        <div class="info-card">
                            <div class="info-header">
                                <i class="fas fa-chart-line me-2"></i>
                                Scanning Progress
                            </div>
                            <div class="progress-content">
                                <div class="overall-progress">
                                    <div class="progress-header">
                                        <span>Overall Progress</span>
                                        <span class="progress-percentage" id="overallProgressValue">0%</span>
                                    </div>
                                    <div class="progress-bar-container">
                                        <div class="progress-bar overall" id="overallProgressBar" style="width: 0%"></div>
                                    </div>
                                </div>
                                
                                <div class="item-progress-grid" id="itemProgressGrid">
                                    <!-- Item type progress bars will be populated here -->
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Action Buttons -->
                    <div class="action-buttons" id="actionButtons">
                        <button class="btn btn-outline-primary btn-sm" id="selectPOBtn" style="display: none;">
                            <i class="fas fa-list-check me-2"></i>Select PO
                        </button>
                        <button class="btn btn-outline-warning btn-sm" id="resetSessionBtn" style="display: none;">
                            <i class="fas fa-undo me-2"></i>Reset Session
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
            <style id="hierarchical-lock-status-styles">
                .lock-status-container {
                    background: white;
                    border-radius: 12px;
                    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
                    overflow: hidden;
                }

                .status-header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 1rem 1.25rem;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }

                .status-title {
                    margin: 0;
                    font-size: 1.1rem;
                    font-weight: 600;
                }

                .status-indicator {
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                    font-size: 0.9rem;
                    font-weight: 500;
                }

                .status-dot {
                    width: 8px;
                    height: 8px;
                    border-radius: 50%;
                    background: #6c757d;
                    transition: all 0.3s ease;
                }

                .status-dot.active {
                    background: #28a745;
                    box-shadow: 0 0 0 3px rgba(40, 167, 69, 0.3);
                    animation: statusPulse 2s infinite;
                }

                .status-dot.warning {
                    background: #ffc107;
                    box-shadow: 0 0 0 3px rgba(255, 193, 7, 0.3);
                }

                .status-dot.error {
                    background: #dc3545;
                    box-shadow: 0 0 0 3px rgba(220, 53, 69, 0.3);
                }

                @keyframes statusPulse {
                    0%, 100% { transform: scale(1); }
                    50% { transform: scale(1.2); }
                }

                .status-body {
                    padding: 1.25rem;
                }

                /* Lock Progression */
                .lock-progression {
                    margin-bottom: 1.5rem;
                }

                .progression-steps {
                    display: flex;
                    align-items: center;
                    position: relative;
                }

                .step-item {
                    flex: 1;
                    display: flex;
                    align-items: center;
                    position: relative;
                    transition: all 0.3s ease;
                }

                .step-icon {
                    width: 48px;
                    height: 48px;
                    border-radius: 50%;
                    background: #e9ecef;
                    color: #6c757d;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 1.2rem;
                    transition: all 0.3s ease;
                    border: 3px solid transparent;
                    position: relative;
                    z-index: 2;
                }

                .step-content {
                    margin-left: 0.75rem;
                    flex: 1;
                }

                .step-title {
                    font-weight: 600;
                    color: #2c3e50;
                    font-size: 0.9rem;
                    margin-bottom: 0.1rem;
                }

                .step-subtitle {
                    font-size: 0.75rem;
                    color: #6c757d;
                    text-transform: uppercase;
                }

                .step-connector {
                    position: absolute;
                    top: 50%;
                    right: 0;
                    transform: translateY(-50%);
                    width: 100%;
                    height: 2px;
                    background: #e9ecef;
                    z-index: 1;
                    transition: background 0.3s ease;
                }

                .step-item:last-child .step-connector {
                    display: none;
                }

                /* Step States */
                .step-item.completed .step-icon {
                    background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
                    color: white;
                    border-color: #28a745;
                }

                .step-item.completed .step-connector {
                    background: linear-gradient(90deg, #28a745 0%, #20c997 100%);
                }

                .step-item.active .step-icon {
                    background: linear-gradient(135deg, #007bff 0%, #0056b3 100%);
                    color: white;
                    border-color: #007bff;
                    animation: iconPulse 2s infinite;
                }

                .step-item.pending .step-icon {
                    background: linear-gradient(135deg, #ffc107 0%, #fd7e14 100%);
                    color: white;
                    border-color: #ffc107;
                }

                @keyframes iconPulse {
                    0%, 100% { transform: scale(1); }
                    50% { transform: scale(1.05); }
                }

                /* Info Cards */
                .info-card {
                    background: #f8f9fa;
                    border-radius: 8px;
                    margin-bottom: 1rem;
                    overflow: hidden;
                }

                .info-header {
                    background: linear-gradient(135deg, #495057 0%, #6c757d 100%);
                    color: white;
                    padding: 0.75rem 1rem;
                    font-weight: 600;
                    font-size: 0.9rem;
                }

                .info-content {
                    padding: 1rem;
                }

                .info-row {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 0.5rem;
                    font-size: 0.9rem;
                }

                .info-row:last-child {
                    margin-bottom: 0;
                }

                .info-label {
                    color: #6c757d;
                    font-weight: 500;
                }

                .info-value {
                    color: #2c3e50;
                    font-weight: 600;
                }

                .info-value.badge {
                    background: #007bff;
                    color: white;
                    padding: 0.25rem 0.5rem;
                    border-radius: 12px;
                    font-size: 0.75rem;
                }

                /* Progress */
                .progress-content {
                    padding: 1rem;
                }

                .overall-progress {
                    margin-bottom: 1.5rem;
                }

                .progress-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 0.5rem;
                    font-size: 0.9rem;
                    font-weight: 600;
                    color: #495057;
                }

                .progress-percentage {
                    color: #007bff;
                }

                .progress-bar-container {
                    background: #e9ecef;
                    border-radius: 10px;
                    height: 12px;
                    overflow: hidden;
                    position: relative;
                }

                .progress-bar {
                    height: 100%;
                    border-radius: 10px;
                    transition: width 0.5s ease;
                    position: relative;
                    overflow: hidden;
                }

                .progress-bar.overall {
                    background: linear-gradient(90deg, #007bff 0%, #0056b3 100%);
                }

                .progress-bar::after {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: -100%;
                    width: 100%;
                    height: 100%;
                    background: linear-gradient(90deg, transparent 0%, rgba(255,255,255,0.4) 50%, transparent 100%);
                    animation: shimmer 2s infinite;
                }

                @keyframes shimmer {
                    0% { left: -100%; }
                    100% { left: 100%; }
                }

                .item-progress-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
                    gap: 1rem;
                }

                .item-progress {
                    text-align: center;
                }

                .item-progress-label {
                    font-size: 0.75rem;
                    font-weight: 600;
                    color: #6c757d;
                    margin-bottom: 0.5rem;
                    text-transform: uppercase;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 0.25rem;
                }

                .item-progress-bar {
                    height: 8px;
                    background: #e9ecef;
                    border-radius: 4px;
                    overflow: hidden;
                    margin-bottom: 0.25rem;
                }

                .item-progress-bar .progress-bar {
                    height: 100%;
                }

                .item-progress-bar .progress-bar.box {
                    background: linear-gradient(90deg, #ff9a56 0%, #ff6b35 100%);
                }

                .item-progress-bar .progress-bar.pallet {
                    background: linear-gradient(90deg, #4facfe 0%, #00f2fe 100%);
                }

                .item-progress-bar .progress-bar.pcs {
                    background: linear-gradient(90deg, #43e97b 0%, #38f9d7 100%);
                }

                .item-progress-count {
                    font-size: 0.7rem;
                    color: #495057;
                    font-weight: 600;
                }

                /* Action Buttons */
                .action-buttons {
                    display: flex;
                    gap: 0.5rem;
                    margin-top: 1rem;
                    padding-top: 1rem;
                    border-top: 1px solid #e9ecef;
                }

                .action-buttons .btn {
                    flex: 1;
                    font-size: 0.8rem;
                    transition: all 0.2s ease;
                }

                .action-buttons .btn:hover {
                    transform: translateY(-1px);
                    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
                }

                /* Responsive */
                @media (max-width: 768px) {
                    .progression-steps {
                        flex-direction: column;
                        align-items: flex-start;
                    }
                    
                    .step-item {
                        width: 100%;
                        margin-bottom: 1rem;
                    }
                    
                    .step-connector {
                        display: none;
                    }
                    
                    .item-progress-grid {
                        grid-template-columns: 1fr;
                    }
                    
                    .action-buttons {
                        flex-direction: column;
                    }
                }
            </style>
        `;

        if (!document.getElementById('hierarchical-lock-status-styles')) {
            document.head.insertAdjacentHTML('beforeend', styles);
        }
    }

    /**
     * Bind component events
     */
    bindEvents() {
        // Select PO button
        document.getElementById('selectPOBtn').addEventListener('click', () => {
            this.showPOSelector();
        });

        // Reset session button
        document.getElementById('resetSessionBtn').addEventListener('click', () => {
            this.resetSession();
        });

        // Lock manager state changes
        this.lockManager.onStateChange((event, data) => {
            this.handleStateChange(event, data);
        });

        // Lock manager lock level changes
        this.lockManager.onLockChange((level, state) => {
            this.handleLockChange(level, state);
        });
    }

    /**
     * Handle state changes
     */
    handleStateChange(event, data) {
        console.log('📊 Status component handling state change:', event, data);
        
        switch (event) {
            case 'SESSION_INITIALIZED':
                this.updateSessionInfo(data);
                this.updateLockProgression('MASTER');
                this.updateOverallStatus('active', 'Session Active');
                break;
                
            case 'PO_SELECTED':
                this.updatePOInfo(data.selectedPO);
                this.updateProgressInfo(data.selectedPO.progress);
                this.updateLockProgression('PO');
                break;
                
            case 'PO_PROGRESS_UPDATED':
                if (data.selectedPO) {
                    this.updateProgressInfo(data.selectedPO.progress);
                }
                break;
                
            case 'SESSION_RESET':
                this.resetDisplay();
                break;
                
            case 'SESSION_COMPLETED':
                this.updateOverallStatus('success', 'Session Completed');
                this.updateLockProgression('COMPLETED');
                break;
        }
    }

    /**
     * Handle lock level changes
     */
    handleLockChange(level, state) {
        console.log('📊 Status component handling lock change:', level, state);
        
        switch (level) {
            case 'MASTER':
                this.updateLockProgression('MASTER');
                break;
            case 'PO':
                this.updateLockProgression('PO');
                break;
            case 'SCANNING':
                this.updateLockProgression('SCANNING');
                this.updateOverallStatus('active', 'Scanning Active');
                break;
        }
    }

    /**
     * Update overall status indicator
     */
    updateOverallStatus(type, text) {
        const indicator = document.getElementById('overallStatus');
        const dot = indicator.querySelector('.status-dot');
        const textEl = indicator.querySelector('.status-text');
        
        // Reset classes
        dot.classList.remove('active', 'warning', 'error');
        
        switch (type) {
            case 'active':
                dot.classList.add('active');
                break;
            case 'warning':
                dot.classList.add('warning');
                break;
            case 'error':
                dot.classList.add('error');
                break;
            case 'success':
                dot.classList.add('active');
                break;
        }
        
        textEl.textContent = text;
    }

    /**
     * Update lock progression steps
     */
    updateLockProgression(currentLevel) {
        const steps = document.querySelectorAll('.step-item');
        
        steps.forEach((step, index) => {
            const stepType = step.dataset.step;
            step.classList.remove('completed', 'active', 'pending');
            
            switch (currentLevel) {
                case 'MASTER':
                    if (stepType === 'master') {
                        step.classList.add('completed');
                    } else if (stepType === 'po') {
                        step.classList.add('active');
                    }
                    break;
                    
                case 'PO':
                    if (stepType === 'master' || stepType === 'po') {
                        step.classList.add('completed');
                    } else if (stepType === 'scanning') {
                        step.classList.add('active');
                    }
                    break;
                    
                case 'SCANNING':
                    step.classList.add('completed');
                    if (stepType === 'scanning') {
                        step.classList.add('active');
                    }
                    break;
                    
                case 'COMPLETED':
                    step.classList.add('completed');
                    break;
            }
        });
    }

    /**
     * Update session information
     */
    updateSessionInfo(data) {
        document.getElementById('sessionIdValue').textContent = data.sessionId || '-';
        document.getElementById('sessionStatusValue').textContent = data.sessionStatus || '-';
        document.getElementById('totalPOsValue').textContent = data.availablePOs?.length || '-';
        
        document.getElementById('sessionInfo').style.display = 'block';
        document.getElementById('selectPOBtn').style.display = 'inline-block';
        document.getElementById('resetSessionBtn').style.display = 'inline-block';
    }

    /**
     * Update PO information
     */
    updatePOInfo(po) {
        document.getElementById('poNumberValue').textContent = po.noPO || '-';
        document.getElementById('poModelValue').textContent = po.modelProduct || '-';
        
        const nextTypeEl = document.getElementById('nextItemTypeValue');
        nextTypeEl.textContent = po.nextAvailableItemType || '-';
        nextTypeEl.className = `info-value badge bg-${this.getItemTypeColor(po.nextAvailableItemType)}`;
        
        document.getElementById('poInfo').style.display = 'block';
    }

    /**
     * Update progress information
     */
    updateProgressInfo(progress) {
        if (!progress) return;
        
        // Overall progress
        const overallPercentage = Math.round(progress.overallProgressPercentage || 0);
        document.getElementById('overallProgressValue').textContent = `${overallPercentage}%`;
        document.getElementById('overallProgressBar').style.width = `${overallPercentage}%`;
        
        // Item type progress
        const gridContainer = document.getElementById('itemProgressGrid');
        const itemTypes = [];
        
        if (progress.totalBoxes > 0) {
            itemTypes.push({
                type: 'box',
                label: 'BOX',
                icon: 'fas fa-box',
                scanned: progress.scannedBoxes,
                total: progress.totalBoxes,
                percentage: progress.boxProgressPercentage
            });
        }
        
        if (progress.totalPallets > 0) {
            itemTypes.push({
                type: 'pallet',
                label: 'PALLET',
                icon: 'fas fa-pallet',
                scanned: progress.scannedPallets,
                total: progress.totalPallets,
                percentage: progress.palletProgressPercentage
            });
        }
        
        if (progress.totalPcs > 0) {
            itemTypes.push({
                type: 'pcs',
                label: 'PCS',
                icon: 'fas fa-cubes',
                scanned: progress.scannedPcs,
                total: progress.totalPcs,
                percentage: progress.pcsProgressPercentage
            });
        }
        
        gridContainer.innerHTML = itemTypes.map(item => `
            <div class="item-progress">
                <div class="item-progress-label">
                    <i class="${item.icon}"></i>
                    ${item.label}
                </div>
                <div class="item-progress-bar">
                    <div class="progress-bar ${item.type}" style="width: ${item.percentage}%"></div>
                </div>
                <div class="item-progress-count">${item.scanned}/${item.total}</div>
            </div>
        `).join('');
        
        document.getElementById('progressInfo').style.display = 'block';
    }

    /**
     * Get item type color for badges
     */
    getItemTypeColor(itemType) {
        switch (itemType?.toLowerCase()) {
            case 'box': return 'warning';
            case 'pallet': return 'info';
            case 'pcs': return 'success';
            default: return 'secondary';
        }
    }

    /**
     * Show PO selector
     */
    showPOSelector() {
        // Trigger PO selector from parent application
        if (window.poSelector) {
            window.poSelector.show();
        }
    }

    /**
     * Reset session
     */
    resetSession() {
        if (confirm('Are you sure you want to reset the current session? This will clear all progress.')) {
            this.lockManager.resetSession();
        }
    }

    /**
     * Reset display to initial state
     */
    resetDisplay() {
        this.updateOverallStatus('inactive', 'Not Active');
        this.updateLockProgression('NONE');
        
        document.getElementById('sessionInfo').style.display = 'none';
        document.getElementById('poInfo').style.display = 'none';
        document.getElementById('progressInfo').style.display = 'none';
        document.getElementById('selectPOBtn').style.display = 'none';
        document.getElementById('resetSessionBtn').style.display = 'none';
        
        // Reset values
        document.getElementById('sessionIdValue').textContent = '-';
        document.getElementById('sessionStatusValue').textContent = '-';
        document.getElementById('totalPOsValue').textContent = '-';
        document.getElementById('poNumberValue').textContent = '-';
        document.getElementById('poModelValue').textContent = '-';
        document.getElementById('nextItemTypeValue').textContent = '-';
        document.getElementById('overallProgressValue').textContent = '0%';
        document.getElementById('overallProgressBar').style.width = '0%';
        document.getElementById('itemProgressGrid').innerHTML = '';
    }

    /**
     * Cleanup component
     */
    cleanup() {
        const styles = document.getElementById('hierarchical-lock-status-styles');
        if (styles) {
            styles.remove();
        }
        console.log('🧹 HierarchicalLockStatusComponent cleanup completed');
    }
}

// Export for global use
window.HierarchicalLockStatusComponent = HierarchicalLockStatusComponent;
