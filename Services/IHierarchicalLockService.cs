using ShipmentFinishGood.Common;
using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface IHierarchicalLockService
    {
        // Session Lock Management
        Task<Result<UserSessionLockDto>> LockUserToSessionByMasterQRAsync(string userId, string masterQRCode);
        Task<Result<UserSessionLockDto>> LockUserToSessionAsync(string userId, int sessionId, string masterQRCode);
        Task<Result<bool>> UnlockUserFromSessionAsync(string userId, int sessionId);
        Task<Result<UserSessionLockDto?>> GetUserSessionLockAsync(string userId);
        Task<Result<bool>> IsUserLockedToSessionAsync(string userId);
        
        // PO Lock Management
        Task<Result<UserPOLockDto>> LockUserToPOAsync(string userId, int poId);
        Task<Result<bool>> UnlockUserFromPOAsync(string userId, int poLockId);
        Task<Result<UserPOLockDto?>> GetUserPOLockAsync(string userId);
        Task<Result<bool>> IsUserLockedToPOAsync(string userId);
        
        // PO Selection & Management
        Task<Result<List<POSelectionDto>>> GetAvailablePOsForSessionAsync(int sessionId);
        Task<Result<POSelectionDto?>> GetPODetailsAsync(int poId);
        Task<Result<bool>> CompletePOAsync(int poId, string completedBy);
        
        // Item Scanning with Hierarchical Validation
        Task<Result<POItemScanResultDto>> ScanItemWithHierarchicalValidationAsync(
            string userId, string barcode, string scannedBy);
        
        // Item Registry Management
        Task<Result<bool>> InitializePOItemRegistryAsync(int poId);
        Task<Result<bool>> ValidateItemBelongsToPOAsync(string barcode, int poId);
        Task<Result<string>> DetermineItemTypeAsync(string barcode);
        
        // Progress & Completion Management
        Task<Result<POProgressUpdateDto>> GetPOProgressAsync(int poId);
        Task<Result<bool>> CheckPOCompletionAsync(int poId);
        Task<Result<bool>> CheckSessionCompletionAsync(int sessionId);
        
        // Lock Status & Analytics
        Task<Result<HierarchicalLockStatusDto>> GetUserLockStatusAsync(string userId);
        Task<Result<List<UserSessionLockDto>>> GetActiveSessionLocksAsync();
        Task<Result<List<UserPOLockDto>>> GetActivePOLocksAsync();
        
        // Auto-unlock Management
        Task<Result<bool>> AutoUnlockCompletedPOAsync(int poId, string completedBy);
        Task<Result<bool>> AutoUnlockCompletedSessionAsync(int sessionId, string completedBy);
        
        // Emergency & Manual Operations
        Task<Result<bool>> EmergencyUnlockUserAsync(string userId, string unlockedBy);
        Task<Result<bool>> ForceUnlockSessionAsync(int sessionId, string unlockedBy);
        Task<Result<bool>> ForceUnlockPOAsync(int poId, string unlockedBy);
    }
}
