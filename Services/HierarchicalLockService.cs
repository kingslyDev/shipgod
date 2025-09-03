using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Hubs;

namespace ShipmentFinishGood.Services
{
    public class HierarchicalLockService : IHierarchicalLockService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly ILogger<HierarchicalLockService> _logger;

        public HierarchicalLockService(
            AppDbContext context, 
            IHubContext<ProgressHub> hubContext,
            ILogger<HierarchicalLockService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        #region Session Lock Management

        // New method: Lock using Master QR only (auto-resolve sessionId)
        public async Task<Result<UserSessionLockDto>> LockUserToSessionByMasterQRAsync(string userId, string masterQRCode)
        {
            try
            {
                _logger.LogInformation($"🔒 HIERARCHICAL LOCK: User {userId} attempting to lock using Master QR {masterQRCode}");

                // Find session by looking up the Master QR in BarcodeRegistry
                _logger.LogInformation($"🔍 Searching for Master QR: '{masterQRCode}' in BarcodeRegistry...");
                
                var masterBarcode = await _context.BarcodeRegistries
                    .Where(b => b.BarcodeValue == masterQRCode && 
                               b.BarcodeType == "MASTER" && 
                               b.IsActive == true)
                    .FirstOrDefaultAsync();

                if (masterBarcode == null)
                {
                    _logger.LogWarning($"❌ No Master QR found in BarcodeRegistry: {masterQRCode}");
                    
                    // Try case insensitive and partial match
                    var partialMatch = await _context.BarcodeRegistries
                        .Where(b => b.BarcodeValue.Contains(masterQRCode) && 
                                   b.BarcodeType == "MASTER" && 
                                   b.IsActive == true)
                        .FirstOrDefaultAsync();
                    
                    if (partialMatch != null)
                    {
                        _logger.LogInformation($"✅ Found partial match: {partialMatch.BarcodeValue} for Session {partialMatch.SessionId}");
                        masterBarcode = partialMatch;
                    }
                    else
                    {
                        // Debug: Show available master QRs
                        var availableMasters = await _context.BarcodeRegistries
                            .Where(b => b.BarcodeType == "MASTER" && b.IsActive == true)
                            .Select(b => new { b.BarcodeValue, b.SessionId })
                            .Take(5)
                            .ToListAsync();
                        
                        _logger.LogInformation($"📋 Available Master QRs: {string.Join(", ", availableMasters.Select(m => $"[{m.BarcodeValue}:Session{m.SessionId}]"))}");
                        
                        return Result<UserSessionLockDto>.Failure($"Master QR '{masterQRCode}' not found. Check available Master QRs in logs.");
                    }
                }

                _logger.LogInformation($"✅ Found Master QR {masterQRCode} linked to Session {masterBarcode.SessionId}");

                // Now find the actual session
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == masterBarcode.SessionId);

                if (session == null)
                {
                    _logger.LogWarning($"❌ Session {masterBarcode.SessionId} not found in UploadSessions");
                    return Result<UserSessionLockDto>.Failure($"Session {masterBarcode.SessionId} not found.");
                }

                _logger.LogInformation($"✅ Found session {session.SessionId} ({session.FileName}) for Master QR {masterQRCode}");

                // Use existing method with resolved sessionId
                return await LockUserToSessionAsync(userId, session.SessionId, masterQRCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error locking user {userId} to session using Master QR {masterQRCode}");
                return Result<UserSessionLockDto>.Failure($"Error creating session lock: {ex.Message}");
            }
        }

        public async Task<Result<UserSessionLockDto>> LockUserToSessionAsync(string userId, int sessionId, string masterQRCode)
        {
            try
            {
                _logger.LogInformation($"🔒 HIERARCHICAL LOCK: User {userId} attempting to lock to session {sessionId} with QR {masterQRCode}");

                // Check if user is already locked to any session
                var existingLock = await GetActiveSessionLockForUserAsync(userId);
                if (existingLock != null)
                {
                    _logger.LogWarning($"❌ User {userId} already locked to session {existingLock.SessionId}");
                    return Result<UserSessionLockDto>.Failure(
                        $"User is already locked to session {existingLock.SessionId}. Complete or unlock that session first.");
                }

                // Validate session exists and has valid data
                _logger.LogInformation($"🔍 Searching for session {sessionId}...");
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                _logger.LogInformation($"🔍 Session search result: {(session == null ? "NOT FOUND" : $"FOUND - Status: {session.Status}, POs: {session.POMasters?.Count ?? 0}")}");

                if (session == null)
                {
                    _logger.LogError($"❌ Session {sessionId} not found in database");
                    return Result<UserSessionLockDto>.Failure("Session not found");
                }

                if (!session.POMasters.Any())
                    return Result<UserSessionLockDto>.Failure("Session has no PO data to scan");

                // Create session lock
                var sessionLock = new UserSessionLock
                {
                    UserId = userId,
                    SessionId = sessionId,
                    MasterQRCode = masterQRCode,
                    LockedAt = DateTime.Now,
                    IsActive = true,
                    CreatedBy = userId
                };

                _context.UserSessionLocks.Add(sessionLock);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Session lock created: LockId {sessionLock.LockId}");

                // Initialize PO Item Registries for all POs in this session
                foreach (var po in session.POMasters)
                {
                    await InitializePOItemRegistryAsync(po.POId);
                }

                // Send SignalR notification
                await _hubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("SessionLocked", new
                    {
                        sessionId,
                        lockedBy = userId,
                        qrIdentity = masterQRCode,
                        timestamp = DateTime.Now,
                        lockType = "HIERARCHICAL"
                    });

                return Result<UserSessionLockDto>.Success(await MapToSessionLockDtoAsync(sessionLock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error locking user {userId} to session {sessionId}");
                return Result<UserSessionLockDto>.Failure($"Error creating session lock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UnlockUserFromSessionAsync(string userId, int sessionId)
        {
            try
            {
                var sessionLock = await _context.UserSessionLocks
                    .Include(sl => sl.POLocks.Where(pl => pl.IsActive))
                    .FirstOrDefaultAsync(sl => sl.UserId == userId && 
                                             sl.SessionId == sessionId && 
                                             sl.IsActive);

                if (sessionLock == null)
                    return Result<bool>.Failure("User is not locked to this session");

                // Unlock any active PO locks first
                foreach (var poLock in sessionLock.POLocks)
                {
                    poLock.IsActive = false;
                    poLock.UnlockedAt = DateTime.Now;
                }

                // Unlock session
                sessionLock.IsActive = false;
                sessionLock.UnlockedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔓 Session unlocked: User {userId} from session {sessionId}");

                // Send SignalR notification
                await _hubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("SessionUnlocked", new
                    {
                        sessionId,
                        unlockedBy = userId,
                        timestamp = DateTime.Now,
                        lockType = "HIERARCHICAL"
                    });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking user {userId} from session {sessionId}");
                return Result<bool>.Failure($"Error unlocking session: {ex.Message}");
            }
        }

        public async Task<Result<UserSessionLockDto?>> GetUserSessionLockAsync(string userId)
        {
            try
            {
                var sessionLock = await GetActiveSessionLockForUserAsync(userId);
                if (sessionLock == null)
                    return Result<UserSessionLockDto?>.Success(null);

                return Result<UserSessionLockDto?>.Success(await MapToSessionLockDtoAsync(sessionLock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting session lock for user {userId}");
                return Result<UserSessionLockDto?>.Failure($"Error retrieving session lock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> IsUserLockedToSessionAsync(string userId)
        {
            try
            {
                var hasLock = await _context.UserSessionLocks
                    .AnyAsync(sl => sl.UserId == userId && sl.IsActive);
                return Result<bool>.Success(hasLock);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking session lock: {ex.Message}");
            }
        }

        #endregion

        #region PO Lock Management

        public async Task<Result<UserPOLockDto>> LockUserToPOAsync(string userId, int poId)
        {
            try
            {
                _logger.LogInformation($"🔒 PO LOCK: User {userId} attempting to lock to PO {poId}");

                // Check if user has active session lock
                var sessionLock = await GetActiveSessionLockForUserAsync(userId);
                if (sessionLock == null)
                    return Result<UserPOLockDto>.Failure("User must be locked to a session first");

                _logger.LogInformation($"🔍 User {userId} locked to session {sessionLock.SessionId}");

                // Debug: Check all POs in the session
                var allPOsInSession = await _context.POMasters
                    .Where(p => p.SourceSessionId == sessionLock.SessionId)
                    .Select(p => new { p.POId, p.NoPO, p.SourceSessionId })
                    .ToListAsync();
                
                _logger.LogInformation($"📋 POs in session {sessionLock.SessionId}: {string.Join(", ", allPOsInSession.Select(p => $"[{p.POId}:{p.NoPO}]"))}");

                // Check if PO belongs to locked session
                var po = await _context.POMasters
                    .FirstOrDefaultAsync(p => p.POId == poId && p.SourceSessionId == sessionLock.SessionId);

                if (po == null)
                {
                    _logger.LogWarning($"❌ PO {poId} not found in session {sessionLock.SessionId}");
                    return Result<UserPOLockDto>.Failure($"PO {poId} does not belong to your locked session {sessionLock.SessionId}. Available POs: {string.Join(", ", allPOsInSession.Select(p => p.POId))}");
                }

                _logger.LogInformation($"✅ PO {poId} ({po.NoPO}) found in session {sessionLock.SessionId}");

                // Check if user has active PO lock
                var existingPOLock = await GetActivePOLockForUserAsync(userId);
                if (existingPOLock != null)
                {
                    return Result<UserPOLockDto>.Failure(
                        $"User is already locked to PO {existingPOLock.NoPO}. Complete or unlock that PO first.");
                }

                // Create PO lock
                var poLock = new UserPOLock
                {
                    UserId = userId,
                    SessionLockId = sessionLock.LockId,
                    POId = poId,
                    NoPO = po.NoPO,
                    LockedAt = DateTime.Now,
                    IsActive = true,
                    CreatedBy = userId
                };

                _context.UserPOLocks.Add(poLock);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ PO lock created: POLockId {poLock.POLockId} for PO {po.NoPO}");

                // Send SignalR notification
                await _hubContext.Clients.Group($"Session_{sessionLock.SessionId}")
                    .SendAsync("POLocked", new
                    {
                        sessionId = sessionLock.SessionId,
                        poId,
                        noPO = po.NoPO,
                        lockedBy = userId,
                        timestamp = DateTime.Now
                    });

                return Result<UserPOLockDto>.Success(await MapToPOLockDtoAsync(poLock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error locking user {userId} to PO {poId}");
                return Result<UserPOLockDto>.Failure($"Error creating PO lock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UnlockUserFromPOAsync(string userId, int poLockId)
        {
            try
            {
                var poLock = await _context.UserPOLocks
                    .Include(pl => pl.SessionLock)
                    .FirstOrDefaultAsync(pl => pl.POLockId == poLockId && 
                                             pl.UserId == userId && 
                                             pl.IsActive);

                if (poLock == null)
                    return Result<bool>.Failure("PO lock not found or already unlocked");

                poLock.IsActive = false;
                poLock.UnlockedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔓 PO unlocked: User {userId} from PO {poLock.NoPO}");

                // Send SignalR notification
                await _hubContext.Clients.Group($"Session_{poLock.SessionLock.SessionId}")
                    .SendAsync("POUnlocked", new
                    {
                        sessionId = poLock.SessionLock.SessionId,
                        poId = poLock.POId,
                        noPO = poLock.NoPO,
                        unlockedBy = userId,
                        timestamp = DateTime.Now
                    });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking PO {poLockId} for user {userId}");
                return Result<bool>.Failure($"Error unlocking PO: {ex.Message}");
            }
        }

        public async Task<Result<UserPOLockDto?>> GetUserPOLockAsync(string userId)
        {
            try
            {
                var poLock = await GetActivePOLockForUserAsync(userId);
                if (poLock == null)
                    return Result<UserPOLockDto?>.Success(null);

                return Result<UserPOLockDto?>.Success(await MapToPOLockDtoAsync(poLock));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PO lock for user {userId}");
                return Result<UserPOLockDto?>.Failure($"Error retrieving PO lock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> IsUserLockedToPOAsync(string userId)
        {
            try
            {
                var hasLock = await _context.UserPOLocks
                    .AnyAsync(pl => pl.UserId == userId && pl.IsActive);
                return Result<bool>.Success(hasLock);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking PO lock: {ex.Message}");
            }
        }

        #endregion

        #region PO Selection & Management

        public async Task<Result<List<POSelectionDto>>> GetAvailablePOsForSessionAsync(int sessionId)
        {
            try
            {
                var pos = await _context.POMasters
                    .Where(p => p.SourceSessionId == sessionId)
                    .ToListAsync();

                var poSelections = new List<POSelectionDto>();

                foreach (var po in pos)
                {
                    var progress = await GetPOProgressAsync(po.POId);
                    var progressDto = progress.IsSuccess ? progress.Value : new POProgressUpdateDto();

                    poSelections.Add(new POSelectionDto
                    {
                        POId = po.POId,
                        NoPO = po.NoPO,
                        ModelProduk = po.ModelProduk,
                        ShipmentDetail = po.ShipmentDetail ?? "",
                        Container = po.Container ?? "",
                        QtyBox = po.QtyBox,
                        QtyPallet = po.QtyPallet,
                        QtyPcs = po.QtyPcs,
                        QtyTotal = po.QtyTotal,
                        ScannedBoxes = progressDto.ScannedBoxes,
                        ScannedPallets = progressDto.ScannedPallets,
                        ScannedPcs = progressDto.ScannedPcs,
                        Progress = progressDto.OverallProgress,
                        IsCompleted = progressDto.IsFullyComplete,
                        IsAvailable = !progressDto.IsFullyComplete
                    });
                }

                return Result<List<POSelectionDto>>.Success(poSelections.OrderBy(p => p.NoPO).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available POs for session {sessionId}");
                return Result<List<POSelectionDto>>.Failure($"Error retrieving POs: {ex.Message}");
            }
        }

        public async Task<Result<POSelectionDto?>> GetPODetailsAsync(int poId)
        {
            try
            {
                var po = await _context.POMasters
                    .FirstOrDefaultAsync(p => p.POId == poId);

                if (po == null)
                    return Result<POSelectionDto?>.Success(null);

                var progress = await GetPOProgressAsync(poId);
                var progressDto = progress.IsSuccess ? progress.Value : new POProgressUpdateDto();

                var poSelection = new POSelectionDto
                {
                    POId = po.POId,
                    NoPO = po.NoPO,
                    ModelProduk = po.ModelProduk,
                    ShipmentDetail = po.ShipmentDetail ?? "",
                    Container = po.Container ?? "",
                    QtyBox = po.QtyBox,
                    QtyPallet = po.QtyPallet,
                    QtyPcs = po.QtyPcs,
                    QtyTotal = po.QtyTotal,
                    ScannedBoxes = progressDto.ScannedBoxes,
                    ScannedPallets = progressDto.ScannedPallets,
                    ScannedPcs = progressDto.ScannedPcs,
                    Progress = progressDto.OverallProgress,
                    IsCompleted = progressDto.IsFullyComplete,
                    IsAvailable = !progressDto.IsFullyComplete
                };

                return Result<POSelectionDto?>.Success(poSelection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PO details for {poId}");
                return Result<POSelectionDto?>.Failure($"Error retrieving PO details: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CompletePOAsync(int poId, string completedBy)
        {
            try
            {
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                    return Result<bool>.Failure("PO not found");

                // Check if PO is actually complete
                var isComplete = await CheckPOCompletionAsync(poId);
                if (!isComplete.IsSuccess || !isComplete.Value)
                    return Result<bool>.Failure("PO is not yet complete");

                // Mark PO as completed
                po.Status = "SCAN_COMPLETED";
                
                // Auto-unlock user from this PO
                var userPOLock = await _context.UserPOLocks
                    .FirstOrDefaultAsync(pl => pl.POId == poId && pl.IsActive);
                
                if (userPOLock != null)
                {
                    userPOLock.IsActive = false;
                    userPOLock.UnlockedAt = DateTime.Now;
                    userPOLock.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ PO {po.NoPO} completed by {completedBy}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing PO {poId}");
                return Result<bool>.Failure($"Error completing PO: {ex.Message}");
            }
        }

        #endregion

        #region Item Scanning with Hierarchical Validation

        public async Task<Result<POItemScanResultDto>> ScanItemWithHierarchicalValidationAsync(
            string userId, string barcode, string scannedBy)
        {
            try
            {
                _logger.LogInformation($"📦 HIERARCHICAL SCAN: User {userId} scanning {barcode}");

                // Step 1: Validate user has active PO lock
                var poLock = await GetActivePOLockForUserAsync(userId);
                if (poLock == null)
                {
                    return Result<POItemScanResultDto>.Failure(
                        "You must select a PO first. Please select a PO from the available list.");
                }

                // Step 2: Validate item belongs to locked PO
                var belongsResult = await ValidateItemBelongsToPOAsync(barcode, poLock.POId);
                if (!belongsResult.IsSuccess)
                {
                    return Result<POItemScanResultDto>.Failure(belongsResult.Error!);
                }

                // Step 3: Determine item type
                var itemTypeResult = await DetermineItemTypeAsync(barcode);
                if (!itemTypeResult.IsSuccess)
                {
                    return Result<POItemScanResultDto>.Failure(itemTypeResult.Error!);
                }

                var itemType = itemTypeResult.Value;

                // Step 4: Validate PO has this item type
                var po = await _context.POMasters.FindAsync(poLock.POId);
                if (!ValidatePOHasItemType(po!, itemType))
                {
                    return Result<POItemScanResultDto>.Failure(
                        $"PO {po!.NoPO} does not contain {itemType} items. This PO only has: " +
                        GetAvailableItemTypes(po));
                }

                // Step 5: Check if item already scanned
                var existingItem = await _context.POItemRegistries
                    .FirstOrDefaultAsync(pir => pir.BarcodeValue == barcode && pir.IsActive);

                if (existingItem != null && existingItem.Status == "SCANNED")
                {
                    return Result<POItemScanResultDto>.Failure(
                        $"{itemType} barcode {barcode} has already been scanned");
                }

                // Step 6: Record the scan
                if (existingItem == null)
                {
                    // Create new registry entry
                    existingItem = new POItemRegistry
                    {
                        POId = poLock.POId,
                        ItemType = itemType,
                        BarcodeValue = barcode,
                        ItemSequence = await GetNextItemSequenceAsync(poLock.POId, itemType),
                        Status = "SCANNED",
                        ScannedAt = DateTime.Now,
                        ScannedBy = scannedBy,
                        IsActive = true
                    };
                    _context.POItemRegistries.Add(existingItem);
                }
                else
                {
                    // Update existing registry
                    existingItem.Status = "SCANNED";
                    existingItem.ScannedAt = DateTime.Now;
                    existingItem.ScannedBy = scannedBy;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ {itemType} scan recorded: {barcode} for PO {po!.NoPO}");

                // Step 7: Get updated progress
                var progressResult = await GetPOProgressAsync(poLock.POId);
                var progressUpdate = progressResult.IsSuccess ? progressResult.Value : null;

                // Step 8: Check for auto-completion
                bool poCompleted = false;
                bool sessionCompleted = false;

                if (progressUpdate?.IsFullyComplete == true)
                {
                    await AutoUnlockCompletedPOAsync(poLock.POId, scannedBy);
                    poCompleted = true;

                    // Check if session is complete
                    var sessionComplete = await CheckSessionCompletionAsync(poLock.SessionLock.SessionId);
                    if (sessionComplete.IsSuccess && sessionComplete.Value)
                    {
                        await AutoUnlockCompletedSessionAsync(poLock.SessionLock.SessionId, scannedBy);
                        sessionCompleted = true;
                    }
                }

                // Step 9: Send SignalR notifications
                await _hubContext.Clients.Group($"Session_{poLock.SessionLock.SessionId}")
                    .SendAsync("ItemScanned", new
                    {
                        sessionId = poLock.SessionLock.SessionId,
                        poId = poLock.POId,
                        noPO = po.NoPO,
                        barcode,
                        itemType,
                        scannedBy,
                        timestamp = DateTime.Now,
                        progressUpdate,
                        poCompleted,
                        sessionCompleted
                    });

                var result = new POItemScanResultDto
                {
                    Success = true,
                    Message = $"{itemType} scanned successfully for PO {po.NoPO}",
                    BarcodeValue = barcode,
                    ItemType = itemType,
                    POId = poLock.POId,
                    NoPO = po.NoPO,
                    ScannedAt = DateTime.Now,
                    ScannedBy = scannedBy,
                    ProgressUpdate = progressUpdate,
                    POCompleted = poCompleted,
                    SessionCompleted = sessionCompleted
                };

                return Result<POItemScanResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning item {barcode} for user {userId}");
                return Result<POItemScanResultDto>.Failure($"Error scanning item: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task<UserSessionLock?> GetActiveSessionLockForUserAsync(string userId)
        {
            return await _context.UserSessionLocks
                .Include(sl => sl.Session)
                .Include(sl => sl.POLocks.Where(pl => pl.IsActive))
                .FirstOrDefaultAsync(sl => sl.UserId == userId && sl.IsActive);
        }

        private async Task<UserPOLock?> GetActivePOLockForUserAsync(string userId)
        {
            return await _context.UserPOLocks
                .Include(pl => pl.SessionLock)
                .Include(pl => pl.POMaster)
                .FirstOrDefaultAsync(pl => pl.UserId == userId && pl.IsActive);
        }

        private async Task<UserSessionLockDto> MapToSessionLockDtoAsync(UserSessionLock sessionLock)
        {
            var session = sessionLock.Session ?? await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstAsync(s => s.SessionId == sessionLock.SessionId);

            var totalPOs = session.POMasters.Count;
            var completedPOs = await _context.POMasters
                .CountAsync(p => p.SourceSessionId == sessionLock.SessionId && p.Status == "SCAN_COMPLETED");

            return new UserSessionLockDto
            {
                LockId = sessionLock.LockId,
                UserId = sessionLock.UserId,
                SessionId = sessionLock.SessionId,
                MasterQRCode = sessionLock.MasterQRCode,
                SessionName = session.FileName,
                LockedAt = sessionLock.LockedAt,
                IsActive = sessionLock.IsActive,
                TotalPOs = totalPOs,
                CompletedPOs = completedPOs,
                SessionProgress = totalPOs > 0 ? (double)completedPOs / totalPOs * 100 : 0
            };
        }

        private async Task<UserPOLockDto> MapToPOLockDtoAsync(UserPOLock poLock)
        {
            var po = poLock.POMaster ?? await _context.POMasters.FirstAsync(p => p.POId == poLock.POId);
            var progress = await GetPOProgressAsync(poLock.POId);
            var progressDto = progress.IsSuccess ? progress.Value : new POProgressUpdateDto();

            return new UserPOLockDto
            {
                POLockId = poLock.POLockId,
                UserId = poLock.UserId,
                SessionLockId = poLock.SessionLockId,
                POId = poLock.POId,
                NoPO = poLock.NoPO,
                ModelProduk = po.ModelProduk,
                LockedAt = poLock.LockedAt,
                IsActive = poLock.IsActive,
                TotalBoxes = po.QtyBox,
                TotalPallets = po.QtyPallet,
                TotalPcs = po.QtyPcs,
                ScannedBoxes = progressDto.ScannedBoxes,
                ScannedPallets = progressDto.ScannedPallets,
                ScannedPcs = progressDto.ScannedPcs,
                POProgress = progressDto.OverallProgress,
                IsCompleted = progressDto.IsFullyComplete,
                CompletedAt = poLock.CompletedAt
            };
        }

        private bool ValidatePOHasItemType(POMaster po, string itemType)
        {
            return itemType switch
            {
                "BOX" => po.QtyBox > 0,
                "PALLET" => po.QtyPallet > 0,
                "PCS" => po.QtyPcs > 0,
                _ => false
            };
        }

        private string GetAvailableItemTypes(POMaster po)
        {
            var types = new List<string>();
            if (po.QtyBox > 0) types.Add($"BOX ({po.QtyBox})");
            if (po.QtyPallet > 0) types.Add($"PALLET ({po.QtyPallet})");
            if (po.QtyPcs > 0) types.Add($"PCS ({po.QtyPcs})");
            return string.Join(", ", types);
        }

        private async Task<int> GetNextItemSequenceAsync(int poId, string itemType)
        {
            var maxSequence = await _context.POItemRegistries
                .Where(pir => pir.POId == poId && pir.ItemType == itemType)
                .MaxAsync(pir => (int?)pir.ItemSequence) ?? 0;
            
            return maxSequence + 1;
        }

        // Implement remaining interface methods...
        public async Task<Result<bool>> InitializePOItemRegistryAsync(int poId)
        {
            try
            {
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                    return Result<bool>.Failure("PO not found");

                // Check if already initialized
                var existingCount = await _context.POItemRegistries
                    .CountAsync(pir => pir.POId == poId && pir.IsActive);
                
                if (existingCount > 0)
                    return Result<bool>.Success(true); // Already initialized

                var itemsToCreate = new List<POItemRegistry>();
                
                // Initialize BOX items
                for (int i = 1; i <= po.QtyBox; i++)
                {
                    itemsToCreate.Add(new POItemRegistry
                    {
                        POId = poId,
                        ItemType = "BOX",
                        BarcodeValue = $"QR_{po.SourceSessionId}_{po.NoPO}_BOX_{i:D3}",
                        ItemSequence = i,
                        Status = "PENDING",
                        IsActive = true
                    });
                }

                // Initialize PALLET items (if any)
                for (int i = 1; i <= po.QtyPallet; i++)
                {
                    itemsToCreate.Add(new POItemRegistry
                    {
                        POId = poId,
                        ItemType = "PALLET",
                        BarcodeValue = $"PALLET_{po.NoPO}_{i:D3}",
                        ItemSequence = i,
                        Status = "PENDING",
                        IsActive = true
                    });
                }

                // Initialize PCS items (if any)
                for (int i = 1; i <= po.QtyPcs; i++)
                {
                    itemsToCreate.Add(new POItemRegistry
                    {
                        POId = poId,
                        ItemType = "PCS",
                        BarcodeValue = $"%Q_{po.NoPO}_{i:D3}",
                        ItemSequence = i,
                        Status = "PENDING",
                        IsActive = true
                    });
                }

                _context.POItemRegistries.AddRange(itemsToCreate);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Initialized {itemsToCreate.Count} items for PO {po.NoPO}");
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing PO item registry for {poId}");
                return Result<bool>.Failure($"Error initializing PO items: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ValidateItemBelongsToPOAsync(string barcode, int poId)
        {
            try
            {
                _logger.LogInformation($"🔍 Validating barcode '{barcode}' for PO {poId}");
                
                // First check if item exists in POItemRegistry for this PO
                var registryItem = await _context.POItemRegistries
                    .FirstOrDefaultAsync(pir => pir.BarcodeValue == barcode && 
                                               pir.POId == poId && 
                                               pir.IsActive);

                if (registryItem != null)
                {
                    _logger.LogInformation($"✅ Found barcode in POItemRegistry");
                    return Result<bool>.Success(true);
                }

                _logger.LogInformation($"🔍 Barcode not found in POItemRegistry, checking PO pattern...");

                // If not in registry, check if barcode pattern matches PO
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                {
                    _logger.LogError($"❌ PO {poId} not found");
                    return Result<bool>.Failure("PO not found");
                }

                _logger.LogInformation($"🔍 Found PO: '{po.NoPO}' - {po.ModelProduk}");

                var upperBarcode = barcode?.ToUpperInvariant() ?? "";

                // Ensure PO number is not null or empty
                if (string.IsNullOrEmpty(po.NoPO))
                {
                    _logger.LogWarning($"❌ PO {poId} has null or empty NoPO field");
                    return Result<bool>.Failure($"PO {poId} has invalid PO number");
                }

                _logger.LogInformation($"🔍 Checking patterns for barcode: '{upperBarcode}' against PO: '{po.NoPO}'");

                // Check BOX pattern
                if (upperBarcode.Contains("BOX") && !string.IsNullOrEmpty(barcode) && barcode.Contains(po.NoPO))
                {
                    _logger.LogInformation($"✅ BOX pattern matched");
                    return Result<bool>.Success(true);
                }

                // Check PALLET pattern
                if (upperBarcode.Contains("PALLET") && !string.IsNullOrEmpty(barcode) && barcode.Contains(po.NoPO))
                {
                    _logger.LogInformation($"✅ PALLET pattern matched");
                    return Result<bool>.Success(true);
                }

                // Check PCS pattern
                if (upperBarcode.StartsWith("%Q") && !string.IsNullOrEmpty(barcode) && barcode.Contains(po.NoPO))
                {
                    _logger.LogInformation($"✅ PCS pattern matched");
                    return Result<bool>.Success(true);
                }

                _logger.LogWarning($"❌ No pattern matched for barcode '{barcode}' and PO '{po.NoPO}'");
                return Result<bool>.Failure($"Barcode {barcode} does not belong to PO {po.NoPO}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating barcode {barcode} for PO {poId}");
                return Result<bool>.Failure($"Error validating barcode: {ex.Message}");
            }
        }

        public Task<Result<string>> DetermineItemTypeAsync(string barcode)
        {
            try
            {
                var upperBarcode = barcode.ToUpperInvariant();
                
                if (upperBarcode.Contains("PALLET"))
                    return Task.FromResult(Result<string>.Success("PALLET"));
                
                if (upperBarcode.StartsWith("%Q"))
                    return Task.FromResult(Result<string>.Success("PCS"));
                
                if (upperBarcode.Contains("BOX"))
                    return Task.FromResult(Result<string>.Success("BOX"));

                return Task.FromResult(Result<string>.Failure("Cannot determine item type from barcode"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result<string>.Failure($"Error determining item type: {ex.Message}"));
            }
        }

        public async Task<Result<POProgressUpdateDto>> GetPOProgressAsync(int poId)
        {
            try
            {
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                    return Result<POProgressUpdateDto>.Failure("PO not found");

                // Get scanned counts from POItemRegistry
                var scannedBoxes = await _context.POItemRegistries
                    .CountAsync(pir => pir.POId == poId && 
                                      pir.ItemType == "BOX" && 
                                      pir.Status == "SCANNED" && 
                                      pir.IsActive);

                var scannedPallets = await _context.POItemRegistries
                    .CountAsync(pir => pir.POId == poId && 
                                      pir.ItemType == "PALLET" && 
                                      pir.Status == "SCANNED" && 
                                      pir.IsActive);

                var scannedPcs = await _context.POItemRegistries
                    .CountAsync(pir => pir.POId == poId && 
                                      pir.ItemType == "PCS" && 
                                      pir.Status == "SCANNED" && 
                                      pir.IsActive);

                var totalScanned = scannedBoxes + scannedPallets + scannedPcs;
                var totalItems = po.QtyBox + po.QtyPallet + po.QtyPcs;

                var progressDto = new POProgressUpdateDto
                {
                    POId = poId,
                    NoPO = po.NoPO,
                    ScannedBoxes = scannedBoxes,
                    ScannedPallets = scannedPallets,
                    ScannedPcs = scannedPcs,
                    TotalScanned = totalScanned,
                    TotalBoxes = po.QtyBox,
                    TotalPallets = po.QtyPallet,
                    TotalPcs = po.QtyPcs,
                    TotalItems = totalItems,
                    BoxProgress = po.QtyBox > 0 ? (double)scannedBoxes / po.QtyBox * 100 : 100,
                    PalletProgress = po.QtyPallet > 0 ? (double)scannedPallets / po.QtyPallet * 100 : 100,
                    PcsProgress = po.QtyPcs > 0 ? (double)scannedPcs / po.QtyPcs * 100 : 100,
                    OverallProgress = totalItems > 0 ? (double)totalScanned / totalItems * 100 : 100,
                    IsBoxComplete = po.QtyBox == 0 || scannedBoxes >= po.QtyBox,
                    IsPalletComplete = po.QtyPallet == 0 || scannedPallets >= po.QtyPallet,
                    IsPcsComplete = po.QtyPcs == 0 || scannedPcs >= po.QtyPcs,
                    IsFullyComplete = (po.QtyBox == 0 || scannedBoxes >= po.QtyBox) &&
                                     (po.QtyPallet == 0 || scannedPallets >= po.QtyPallet) &&
                                     (po.QtyPcs == 0 || scannedPcs >= po.QtyPcs)
                };

                return Result<POProgressUpdateDto>.Success(progressDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting PO progress for {poId}");
                return Result<POProgressUpdateDto>.Failure($"Error getting progress: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CheckPOCompletionAsync(int poId)
        {
            try
            {
                var progress = await GetPOProgressAsync(poId);
                return Result<bool>.Success(progress.IsSuccess && progress.Value?.IsFullyComplete == true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking PO completion: {ex.Message}");
            }
        }

        public async Task<Result<bool>> CheckSessionCompletionAsync(int sessionId)
        {
            try
            {
                var totalPOs = await _context.POMasters
                    .CountAsync(p => p.SourceSessionId == sessionId);

                var completedPOs = await _context.POMasters
                    .CountAsync(p => p.SourceSessionId == sessionId && p.Status == "SCAN_COMPLETED");

                return Result<bool>.Success(totalPOs > 0 && completedPOs >= totalPOs);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Error checking session completion: {ex.Message}");
            }
        }

        public async Task<Result<HierarchicalLockStatusDto>> GetUserLockStatusAsync(string userId)
        {
            try
            {
                var status = new HierarchicalLockStatusDto();

                // Check session lock
                var sessionLock = await GetActiveSessionLockForUserAsync(userId);
                if (sessionLock != null)
                {
                    status.IsSessionLocked = true;
                    status.SessionLock = await MapToSessionLockDtoAsync(sessionLock);

                    // Get available POs for this session
                    var availablePOsResult = await GetAvailablePOsForSessionAsync(sessionLock.SessionId);
                    if (availablePOsResult.IsSuccess && availablePOsResult.Value != null)
                    {
                        status.AvailablePOs = availablePOsResult.Value;
                    }

                    // Check PO lock
                    var poLock = await GetActivePOLockForUserAsync(userId);
                    if (poLock != null)
                    {
                        status.IsPOLocked = true;
                        status.POLock = await MapToPOLockDtoAsync(poLock);
                    }
                }

                return Result<HierarchicalLockStatusDto>.Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lock status for user {userId}");
                return Result<HierarchicalLockStatusDto>.Failure($"Error getting lock status: {ex.Message}");
            }
        }

        public async Task<Result<List<UserSessionLockDto>>> GetActiveSessionLocksAsync()
        {
            try
            {
                var sessionLocks = await _context.UserSessionLocks
                    .Where(sl => sl.IsActive)
                    .ToListAsync();

                var sessionLockDtos = new List<UserSessionLockDto>();
                foreach (var sessionLock in sessionLocks)
                {
                    sessionLockDtos.Add(await MapToSessionLockDtoAsync(sessionLock));
                }

                return Result<List<UserSessionLockDto>>.Success(sessionLockDtos);
            }
            catch (Exception ex)
            {
                return Result<List<UserSessionLockDto>>.Failure($"Error getting active session locks: {ex.Message}");
            }
        }

        public async Task<Result<List<UserPOLockDto>>> GetActivePOLocksAsync()
        {
            try
            {
                var poLocks = await _context.UserPOLocks
                    .Where(pl => pl.IsActive)
                    .ToListAsync();

                var poLockDtos = new List<UserPOLockDto>();
                foreach (var poLock in poLocks)
                {
                    poLockDtos.Add(await MapToPOLockDtoAsync(poLock));
                }

                return Result<List<UserPOLockDto>>.Success(poLockDtos);
            }
            catch (Exception ex)
            {
                return Result<List<UserPOLockDto>>.Failure($"Error getting active PO locks: {ex.Message}");
            }
        }

        public async Task<Result<bool>> AutoUnlockCompletedPOAsync(int poId, string completedBy)
        {
            try
            {
                var isComplete = await CheckPOCompletionAsync(poId);
                if (!isComplete.IsSuccess || !isComplete.Value)
                    return Result<bool>.Success(false); // Not complete yet

                // Mark PO as completed
                var po = await _context.POMasters.FindAsync(poId);
                if (po != null)
                {
                    po.Status = "SCAN_COMPLETED";
                }

                // Auto-unlock users from this PO
                var activePOLocks = await _context.UserPOLocks
                    .Where(pl => pl.POId == poId && pl.IsActive)
                    .ToListAsync();

                foreach (var poLock in activePOLocks)
                {
                    poLock.IsActive = false;
                    poLock.UnlockedAt = DateTime.Now;
                    poLock.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Auto-unlocked {activePOLocks.Count} users from completed PO {po?.NoPO}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auto-unlocking PO {poId}");
                return Result<bool>.Failure($"Error auto-unlocking PO: {ex.Message}");
            }
        }

        public async Task<Result<bool>> AutoUnlockCompletedSessionAsync(int sessionId, string completedBy)
        {
            try
            {
                var isComplete = await CheckSessionCompletionAsync(sessionId);
                if (!isComplete.IsSuccess || !isComplete.Value)
                    return Result<bool>.Success(false); // Not complete yet

                // Mark session as completed
                var session = await _context.UploadSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.Status = "SCAN_COMPLETED";
                }

                // Auto-unlock users from this session
                var activeSessionLocks = await _context.UserSessionLocks
                    .Where(sl => sl.SessionId == sessionId && sl.IsActive)
                    .Include(sl => sl.POLocks.Where(pl => pl.IsActive))
                    .ToListAsync();

                foreach (var sessionLock in activeSessionLocks)
                {
                    // Unlock any active PO locks
                    foreach (var poLock in sessionLock.POLocks)
                    {
                        poLock.IsActive = false;
                        poLock.UnlockedAt = DateTime.Now;
                        poLock.CompletedAt = DateTime.Now;
                    }

                    // Unlock session
                    sessionLock.IsActive = false;
                    sessionLock.UnlockedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Auto-unlocked {activeSessionLocks.Count} users from completed session {sessionId}");

                // Send SignalR notification
                await _hubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("SessionAutoCompleted", new
                    {
                        sessionId,
                        completedBy,
                        message = "All POs completed! Session finished successfully.",
                        timestamp = DateTime.Now
                    });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auto-unlocking session {sessionId}");
                return Result<bool>.Failure($"Error auto-unlocking session: {ex.Message}");
            }
        }

        public async Task<Result<bool>> EmergencyUnlockUserAsync(string userId, string unlockedBy)
        {
            try
            {
                // Unlock all active session locks for this user
                var sessionLocks = await _context.UserSessionLocks
                    .Where(sl => sl.UserId == userId && sl.IsActive)
                    .Include(sl => sl.POLocks.Where(pl => pl.IsActive))
                    .ToListAsync();

                foreach (var sessionLock in sessionLocks)
                {
                    // Unlock any active PO locks
                    foreach (var poLock in sessionLock.POLocks)
                    {
                        poLock.IsActive = false;
                        poLock.UnlockedAt = DateTime.Now;
                    }

                    // Unlock session
                    sessionLock.IsActive = false;
                    sessionLock.UnlockedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🚨 Emergency unlock completed for user {userId} by {unlockedBy}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during emergency unlock for user {userId}");
                return Result<bool>.Failure($"Error during emergency unlock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ForceUnlockSessionAsync(int sessionId, string unlockedBy)
        {
            try
            {
                var sessionLocks = await _context.UserSessionLocks
                    .Where(sl => sl.SessionId == sessionId && sl.IsActive)
                    .Include(sl => sl.POLocks.Where(pl => pl.IsActive))
                    .ToListAsync();

                foreach (var sessionLock in sessionLocks)
                {
                    // Unlock any active PO locks
                    foreach (var poLock in sessionLock.POLocks)
                    {
                        poLock.IsActive = false;
                        poLock.UnlockedAt = DateTime.Now;
                    }

                    // Unlock session
                    sessionLock.IsActive = false;
                    sessionLock.UnlockedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔧 Force unlock completed for session {sessionId} by {unlockedBy}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during force unlock for session {sessionId}");
                return Result<bool>.Failure($"Error during force unlock: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ForceUnlockPOAsync(int poId, string unlockedBy)
        {
            try
            {
                var poLocks = await _context.UserPOLocks
                    .Where(pl => pl.POId == poId && pl.IsActive)
                    .ToListAsync();

                foreach (var poLock in poLocks)
                {
                    poLock.IsActive = false;
                    poLock.UnlockedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"🔧 Force unlock completed for PO {poId} by {unlockedBy}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during force unlock for PO {poId}");
                return Result<bool>.Failure($"Error during force unlock: {ex.Message}");
            }
        }

        #endregion
    }
}
