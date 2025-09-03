/**
 * HierarchicalLockManager - Smart State Management for Scanning System
 * Replaces localStorage logic dengan hierarchical lock validation
 * Implements periodic backend validation dan real-time state cleanup
 */
class HierarchicalLockManager {
    constructor() {
        this.state = {
            sessionId: null,
            sessionStatus: null,
            selectedPO: null,
            availablePOs: [],
            lockLevel: 'NONE', // NONE, MASTER, PO, SCANNING
            lastValidation: null,
            isValidating: false
        };
        
        this.config = {
            validationInterval: 30000, // 30 seconds
            maxRetries: 3,
            retryDelay: 1000
        };
        
        this.callbacks = {
            onStateChange: [],
            onError: [],
            onPOSelection: [],
            onLockChange: []
        };
        
        this.validationTimer = null;
        this.signalRConnection = null;
        
        this.initializeSignalR();
        console.log('🔒 HierarchicalLockManager initialized');
    }

    // ===== CORE STATE MANAGEMENT =====

    /**
     * Initialize session with backend validation
     */
    async initializeSession(sessionId) {
        try {
            console.log(`🔍 Initializing session ${sessionId}...`);
            
            const validation = await this.validateSessionState(sessionId);
            if (!validation.success) {
                throw new Error(validation.message || 'Session validation failed');
            }

            this.state.sessionId = sessionId;
            this.state.sessionStatus = validation.data.status;
            this.state.lockLevel = this.determineLockLevel(validation.data);
            this.state.lastValidation = new Date();

            // Load available POs
            await this.loadAvailablePOs();
            
                // Initialize SignalR after ensuring the library is available
                this.initializeSignalR();
            this.startPeriodicValidation();
            
            this.notifyStateChange('SESSION_INITIALIZED', this.state);
            console.log('✅ Session initialized successfully', this.state);
            
            return { success: true, state: this.state };
        } catch (error) {
            console.error('❌ Session initialization failed:', error);
            this.notifyError('SESSION_INIT_FAILED', error.message);
            return { success: false, error: error.message };
                    if (typeof window.signalR === 'undefined') {
                        // Wait briefly for SignalR to become available if scripts are loading
                        await new Promise((resolve, reject) => {
                            let attempts = 0;
                            const maxAttempts = 50; // ~5s
                            const timer = setInterval(() => {
                                if (typeof window.signalR !== 'undefined') { clearInterval(timer); resolve(); }
                                else if (++attempts >= maxAttempts) { clearInterval(timer); reject(new Error('SignalR library not loaded')); }
                            }, 100);
                        });
                    }

        }
    }

    /**
     * Smart PO selection dengan validation
     */
    async selectPO(poId) {
        try {
            console.log(`🎯 Selecting PO ${poId}...`);
            
            // Validate PO state
            const validation = await this.validatePOState(poId);
            if (!validation.success) {
                throw new Error(validation.message || 'PO validation failed');
            }

            if (!validation.data.canContinueScanning) {
                throw new Error(`PO ${validation.data.noPO} is completed or unavailable for scanning`);
            }

            this.state.selectedPO = {
                id: poId,
                ...validation.data
            };
            this.state.lockLevel = 'PO';
            
            this.notifyStateChange('PO_SELECTED', this.state);
            this.notifyPOSelection(this.state.selectedPO);
            
            console.log('✅ PO selected successfully', this.state.selectedPO);
            return { success: true, po: this.state.selectedPO };
        } catch (error) {
            console.error('❌ PO selection failed:', error);
            this.notifyError('PO_SELECTION_FAILED', error.message);
            return { success: false, error: error.message };
        }
    }

    /**
     * Scan lock management
     */
    enterScanningMode() {
        if (this.state.lockLevel !== 'PO') {
            throw new Error('Must select PO before scanning');
        }
        
        this.state.lockLevel = 'SCANNING';
        this.notifyLockChange('SCANNING', this.state);
        console.log('🔒 Entered scanning mode');
    }

    exitScanningMode() {
        this.state.lockLevel = 'PO';
        this.notifyLockChange('PO', this.state);
        console.log('🔓 Exited scanning mode');
    }

    // ===== BACKEND VALIDATION =====

    /**
     * Validate session state dengan backend
     */
    async validateSessionState(sessionId = null) {
        const id = sessionId || this.state.sessionId;
        if (!id) throw new Error('No session ID available');

        try {
            const response = await fetch(`/Scan/ValidateSessionState?sessionId=${id}`, {
                method: 'GET',
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📡 Session validation response:', result);
            
            return result;
        } catch (error) {
            console.error('📡 Session validation error:', error);
            throw error;
        }
    }

    /**
     * Validate PO state dengan backend
     */
    async validatePOState(poId) {
        try {
            const response = await fetch(`/Scan/ValidatePOState?poId=${poId}`, {
                method: 'GET',
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            console.log('📡 PO validation response:', result);
            
            return result;
        } catch (error) {
            console.error('📡 PO validation error:', error);
            throw error;
        }
    }

    /**
     * Load available POs untuk session
     */
    async loadAvailablePOs() {
        if (!this.state.sessionId) throw new Error('No session ID available');

        try {
            const response = await fetch(`/Scan/GetAvailablePOs?sessionId=${this.state.sessionId}`, {
                method: 'GET',
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const result = await response.json();
            if (result.success) {
                this.state.availablePOs = result.data;
                console.log('📡 Available POs loaded:', this.state.availablePOs);
            } else {
                throw new Error(result.message || 'Failed to load available POs');
            }
        } catch (error) {
            console.error('📡 Load available POs error:', error);
            throw error;
        }
    }

    // ===== PERIODIC VALIDATION =====

    /**
     * Start periodic validation timer
     */
    startPeriodicValidation() {
        if (this.validationTimer) {
            clearInterval(this.validationTimer);
        }

        this.validationTimer = setInterval(async () => {
            await this.performPeriodicValidation();
        }, this.config.validationInterval);

        console.log(`⏰ Periodic validation started (${this.config.validationInterval}ms)`);
    }

    /**
     * Stop periodic validation
     */
    stopPeriodicValidation() {
        if (this.validationTimer) {
            clearInterval(this.validationTimer);
            this.validationTimer = null;
            console.log('⏰ Periodic validation stopped');
        }
    }

    /**
     * Perform periodic validation check
     */
    async performPeriodicValidation() {
        if (this.state.isValidating || !this.state.sessionId) return;

        try {
            this.state.isValidating = true;
            console.log('🔄 Performing periodic validation...');

            // Validate session
            const sessionValidation = await this.validateSessionState();
            if (!sessionValidation.success) {
                this.handleValidationFailure('SESSION_INVALID', sessionValidation.message);
                return;
            }

            // Validate selected PO if any
            if (this.state.selectedPO) {
                const poValidation = await this.validatePOState(this.state.selectedPO.id);
                if (!poValidation.success || !poValidation.data.canContinueScanning) {
                    this.handleValidationFailure('PO_INVALID', `Selected PO is no longer available: ${poValidation.message}`);
                    return;
                }

                // Update PO progress
                this.state.selectedPO.progress = poValidation.data.progress;
            }

            // Refresh available POs
            await this.loadAvailablePOs();

            this.state.lastValidation = new Date();
            console.log('✅ Periodic validation completed');

        } catch (error) {
            console.error('❌ Periodic validation failed:', error);
            this.notifyError('VALIDATION_ERROR', error.message);
        } finally {
            this.state.isValidating = false;
        }
    }

    /**
     * Handle validation failures
     */
    handleValidationFailure(type, message) {
        console.warn(`⚠️ Validation failure: ${type} - ${message}`);
        
        switch (type) {
            case 'SESSION_INVALID':
                this.resetSession();
                break;
            case 'PO_INVALID':
                this.resetPOSelection();
                break;
        }
        
        this.notifyError(type, message);
    }

    // ===== SIGNALR INTEGRATION =====

    /**
     * Initialize SignalR connection untuk real-time updates
     */
    async initializeSignalR() {
        try {
            this.signalRConnection = new signalR.HubConnectionBuilder()
                .withUrl("/progressHub")
                .withAutomaticReconnect()
                .build();

            // Handle real-time state updates
            this.signalRConnection.on("SessionStateChanged", (data) => {
                console.log('📡 SignalR: Session state changed', data);
                this.handleSignalRStateChange(data);
            });

            this.signalRConnection.on("POProgressUpdated", (data) => {
                console.log('📡 SignalR: PO progress updated', data);
                this.handleSignalRProgressUpdate(data);
            });

            this.signalRConnection.on("SessionCompleted", (data) => {
                console.log('📡 SignalR: Session completed', data);
                this.handleSignalRSessionCompleted(data);
            });

            await this.signalRConnection.start();
            console.log('📡 SignalR connection established');
            
        } catch (error) {
            console.error('📡 SignalR initialization failed:', error);
        }
    }

    /**
     * Handle SignalR state changes
     */
    handleSignalRStateChange(data) {
        if (data.sessionId === this.state.sessionId) {
            // Force validation
            this.performPeriodicValidation();
        }
    }

    /**
     * Handle SignalR progress updates
     */
    handleSignalRProgressUpdate(data) {
        if (this.state.selectedPO && data.poId === this.state.selectedPO.id) {
            this.state.selectedPO.progress = data.progress;
            this.notifyStateChange('PO_PROGRESS_UPDATED', this.state);
        }
    }

    /**
     * Handle SignalR session completion
     */
    handleSignalRSessionCompleted(data) {
        if (data.sessionId === this.state.sessionId) {
            this.resetSession();
            this.notifyStateChange('SESSION_COMPLETED', { message: 'Session has been completed' });
        }
    }

    // ===== STATE UTILITIES =====

    /**
     * Determine lock level dari session data
     */
    determineLockLevel(sessionData) {
        if (!sessionData.canScan) return 'COMPLETED';
        if (sessionData.status === 'PROCESSED') return 'MASTER';
        return 'NONE';
    }

    /**
     * Reset session state
     */
    resetSession() {
        this.stopPeriodicValidation();
        this.state = {
            sessionId: null,
            sessionStatus: null,
            selectedPO: null,
            availablePOs: [],
            lockLevel: 'NONE',
            lastValidation: null,
            isValidating: false
        };
        this.notifyStateChange('SESSION_RESET', this.state);
        console.log('🔄 Session state reset');
    }

    /**
     * Reset PO selection
     */
    resetPOSelection() {
        this.state.selectedPO = null;
        this.state.lockLevel = this.state.sessionId ? 'MASTER' : 'NONE';
        this.notifyStateChange('PO_RESET', this.state);
        console.log('🔄 PO selection reset');
    }

    // ===== EVENT SYSTEM =====

    /**
     * Subscribe to state changes
     */
    onStateChange(callback) {
        this.callbacks.onStateChange.push(callback);
    }

    /**
     * Subscribe to errors
     */
    onError(callback) {
        this.callbacks.onError.push(callback);
    }

    /**
     * Subscribe to PO selection
     */
    onPOSelection(callback) {
        this.callbacks.onPOSelection.push(callback);
    }

    /**
     * Subscribe to lock changes
     */
    onLockChange(callback) {
        this.callbacks.onLockChange.push(callback);
    }

    /**
     * Notify state change
     */
    notifyStateChange(event, data) {
        this.callbacks.onStateChange.forEach(callback => {
            try {
                callback(event, data);
            } catch (error) {
                console.error('State change callback error:', error);
            }
        });
    }

    /**
     * Notify error
     */
    notifyError(type, message) {
        this.callbacks.onError.forEach(callback => {
            try {
                callback(type, message);
            } catch (error) {
                console.error('Error callback error:', error);
            }
        });
    }

    /**
     * Notify PO selection
     */
    notifyPOSelection(po) {
        this.callbacks.onPOSelection.forEach(callback => {
            try {
                callback(po);
            } catch (error) {
                console.error('PO selection callback error:', error);
            }
        });
    }

    /**
     * Notify lock change
     */
    notifyLockChange(level, state) {
        this.callbacks.onLockChange.forEach(callback => {
            try {
                callback(level, state);
            } catch (error) {
                console.error('Lock change callback error:', error);
            }
        });
    }

    // ===== PUBLIC API =====

    /**
     * Get current state
     */
    getState() {
        return { ...this.state };
    }

    /**
     * Check if session is active
     */
    isSessionActive() {
        return this.state.sessionId && this.state.lockLevel !== 'NONE';
    }

    /**
     * Check if PO is selected
     */
    isPOSelected() {
        return this.state.selectedPO !== null;
    }

    /**
     * Get available POs
     */
    getAvailablePOs() {
        return [...this.state.availablePOs];
    }

    /**
     * Get selected PO
     */
    getSelectedPO() {
        return this.state.selectedPO ? { ...this.state.selectedPO } : null;
    }

    /**
     * Cleanup resources
     */
    cleanup() {
        this.stopPeriodicValidation();
        if (this.signalRConnection) {
            this.signalRConnection.stop();
        }
        console.log('🧹 HierarchicalLockManager cleanup completed');
    }
}

// Global instance
(function bootstrapHierarchicalLockManager() {
    function create() {
        window.hierarchicalLockManager = new HierarchicalLockManager();
    }

    if (typeof window.signalR !== 'undefined') {
        create();
        return;
    }
    // Wait up to 5s for SignalR if our script loaded first
    let attempts = 0;
    const maxAttempts = 50; // ~5s
    const timer = setInterval(() => {
        if (typeof window.signalR !== 'undefined') { clearInterval(timer); create(); }
        else if (++attempts >= maxAttempts) { clearInterval(timer); console.warn('SignalR still not available; proceeding without realtime.'); create(); }
    }, 100);
})();
