    using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Hubs;

namespace ShipmentFinishGood.Services
{
    public class ScanningService : IScanningService
    {
        private readonly AppDbContext _context;
        private readonly IBarcodeService _barcodeService;
        private readonly IHubContext<ProgressHub> _hubContext;

        public ScanningService(AppDbContext context, IBarcodeService barcodeService, IHubContext<ProgressHub> hubContext)
        {
            _context = context;
            _barcodeService = barcodeService;
            _hubContext = hubContext;
        }

        public async Task<List<ScanSessionSummaryDto>> GetActiveSessionsAsync()
        {
            // Get sessions that have master barcode in BarcodeRegistries
            var sessions = await _context.UploadSessions
                .Include(s => s.POMasters)
                .Where(s => s.POMasters.Any() &&
                           _context.BarcodeRegistries.Any(b => b.SessionId == s.SessionId && 
                                                              b.BarcodeType == "MASTER" && 
                                                              b.IsActive))
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();

            // If no sessions found, try to auto-generate barcodes for VALIDATED sessions
            if (!sessions.Any())
            {
                var validatedSessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => (s.Status == "VALIDATED" || s.Status == "PROCESSED") && s.POMasters.Any())
                    .OrderByDescending(s => s.UploadDate)
                    .ToListAsync();

                foreach (var session in validatedSessions)
                {
                    // Check if barcodes don't exist for this session
                    var existingBarcodes = await _context.BarcodeRegistries
                        .CountAsync(b => b.SessionId == session.SessionId && b.IsActive);
                    
                    if (existingBarcodes == 0)
                    {
                        // Auto-generate QR Identity if missing
                        if (string.IsNullOrEmpty(session.IdentityQRCode))
                        {
                            session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                        }
                        session.Status = "QR_GENERATED";
                        
                        // Update TotalBoxes
                        session.TotalBoxes = session.POMasters.Sum(p => p.QtyBox);
                        
                        _context.UploadSessions.Update(session);

                        // Generate barcodes for the session
                        await _barcodeService.GenerateBarcodesForSessionAsync(session.SessionId, "System");
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Re-fetch sessions after generating barcodes
                sessions = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => s.POMasters.Any() &&
                               _context.BarcodeRegistries.Any(b => b.SessionId == s.SessionId && 
                                                                  b.BarcodeType == "MASTER" && 
                                                                  b.IsActive))
                    .OrderByDescending(s => s.UploadDate)
                    .ToListAsync();
            }

            var result = new List<ScanSessionSummaryDto>();
            
            foreach (var session in sessions)
            {
                // Get master barcode for this session
                var masterBarcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.SessionId == session.SessionId && 
                                            b.BarcodeType == "MASTER" && 
                                            b.IsActive);

                if (masterBarcode != null)
                {
                    // Calculate completed POs (POs with all their barcodes scanned)
                    var completedPOs = 0;
                    foreach (var po in session.POMasters)
                    {
                        var totalBarcodes = await _context.BarcodeRegistries
                            .CountAsync(b => b.SessionId == session.SessionId && 
                                           b.POId == po.POId && 
                                           b.IsActive);
                        
                        var scannedBarcodes = await _context.ScanningActivities
                            .CountAsync(s => s.POContext == $"PO_{po.POId}");
                        
                        if (totalBarcodes > 0 && scannedBarcodes >= totalBarcodes)
                        {
                            completedPOs++;
                        }
                    }

                    result.Add(new ScanSessionSummaryDto
                    {
                        SessionId = session.SessionId,
                        FileName = session.FileName,
                        QRIdentity = masterBarcode.BarcodeValue, // Use master barcode instead of IdentityQRCode
                        ShipmentType = session.ShipmentType ?? "LOOSE",
                        ShipmentDate = session.ShipmentDate,
                        TotalBoxes = session.TotalBoxes > 0 ? session.TotalBoxes : session.POMasters.Sum(p => p.QtyBox),
                        TotalPOs = session.POMasters.Count,
                        CompletedPOs = completedPOs,
                        CreatedDate = session.UploadDate,
                        CreatedBy = session.UploadedBy,
                        Status = GetScanStatus(session.SessionId)
                    });
                }
            }

            return result;
        }

        public async Task<ScanSessionDto?> GetScanSessionAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            // Check if barcodes exist, if not generate them
            var totalBarcodes = await _barcodeService.GetTotalBarcodeCountAsync(sessionId);
            if (totalBarcodes == 0)
            {
                // Auto-generate QR Identity if missing
                if (string.IsNullOrEmpty(session.IdentityQRCode))
                {
                    session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                    session.Status = "QR_GENERATED";
                    _context.UploadSessions.Update(session);
                    await _context.SaveChangesAsync();
                }

                await _barcodeService.GenerateBarcodesForSessionAsync(sessionId, "System");
            }

            // Get master barcode from BarcodeRegistries
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            var barcodes = await _barcodeService.GetBarcodeListForSessionAsync(sessionId);
            var scannedBarcodes = await _barcodeService.GetScannedBarcodesAsync(sessionId);
            var scannedCount = await _barcodeService.GetScannedBarcodeCountAsync(sessionId);
            
            return new ScanSessionDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                QRIdentity = masterBarcode?.BarcodeValue ?? session.IdentityQRCode ?? string.Empty, // Use master barcode if available
                ShipmentType = session.ShipmentType ?? "LOOSE",
                ShipmentDate = session.ShipmentDate,
                TotalBoxes = session.TotalBoxes,
                TotalBarcodes = barcodes.Count,
                ScannedCount = scannedCount,
                BarcodeList = barcodes.Select(b => new BarcodeDto 
                { 
                    BarcodeValue = b.BarcodeValue, 
                    ModelProduct = b.ModelProduct, 
                    BoxNumber = b.BoxNumber 
                }).ToList(),
                ScannedBarcodes = scannedBarcodes,
                IsMasterScanned = await IsMasterQRScannedAsync(sessionId),
                CanComplete = await _barcodeService.IsSessionCompleteAsync(sessionId)
            };
        }

        public async Task<Result<ScanResultDto>> ScanMasterQRAsync(int sessionId, string qrCode, string scannedBy)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null)
                return Result<ScanResultDto>.Failure("Session not found");

            if (session.Status == "SCAN_COMPLETED")
                return Result<ScanResultDto>.Failure("Session has been completed. No further scans allowed.");

            // Get master barcode from BarcodeRegistries instead of using IdentityQRCode
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return Result<ScanResultDto>.Failure("Master QR code not found for this session");

            if (masterBarcode.BarcodeValue != qrCode)
                return Result<ScanResultDto>.Failure("Invalid QR code for this session");

            // Check if user is already locked to another session
            var userLockCheck = await CheckUserLockAsync(scannedBy);
            if (userLockCheck.IsSuccess)
            {
                var lockedSessionResult = await GetUserLockedSessionAsync(scannedBy);
                if (lockedSessionResult.IsSuccess && lockedSessionResult.Value != qrCode)
                    return Result<ScanResultDto>.Failure($"You are locked to session {lockedSessionResult.Value}. Complete that session first.");
            }

            // Check if already scanned
            if (masterBarcode.Status == "SCANNED")
            {
                // Allow additional users to join the existing session by recording their lock
                var alreadyLockedByUser = await _context.ScanningActivities
                    .AnyAsync(sa => sa.UserId == scannedBy && sa.Action == "SCAN_MASTER" && sa.BarcodeValue == qrCode);

                if (!alreadyLockedByUser)
                {
                    var joinActivity = new ScanningActivity
                    {
                        BarcodeValue = qrCode,
                        Action = "SCAN_MASTER",
                        UserId = scannedBy,
                        Timestamp = DateTime.Now,
                        Result = "SUCCESS"
                    };
                    _context.ScanningActivities.Add(joinActivity);
                    await _context.SaveChangesAsync();
                }

                var joinedResult = new ScanResultDto
                {
                    BarcodeValue = qrCode,
                    ScanType = ScanItemType.MasterQR,
                    Message = "Master QR already scanned. You've been joined to this session.",
                    Timestamp = DateTime.Now,
                    ScannedBy = scannedBy
                };

                return Result<ScanResultDto>.Success(joinedResult);
            }

            // Mark the master QR barcode as scanned in BarcodeRegistry
            var markMasterResult = await _barcodeService.MarkBarcodeAsScannedAsync(qrCode, scannedBy);
            if (!markMasterResult.IsSuccess)
                return Result<ScanResultDto>.Failure($"Failed to mark master QR as scanned: {markMasterResult.Error}");

            // Record the scan - this locks the user to this session
            var scanActivity = new ScanningActivity
            {
                BarcodeValue = qrCode,
                Action = "SCAN_MASTER",
                UserId = scannedBy,
                Timestamp = DateTime.Now,
                Result = "SUCCESS"
            };

            _context.ScanningActivities.Add(scanActivity);
            await _context.SaveChangesAsync();

            var result = new ScanResultDto
            {
                BarcodeValue = qrCode,
                ScanType = ScanItemType.MasterQR,
                Message = "Master QR scanned successfully. You are now locked to this session.",
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            };

            // Send SignalR notification to all clients in session group
            await _hubContext.Clients.Group($"Session_{sessionId}")
                .SendAsync("SessionLocked", new { 
                    sessionId, 
                    lockedBy = scannedBy, 
                    qrIdentity = qrCode, 
                    timestamp = DateTime.Now 
                });

            // Also broadcast to all sessions overview
            await _hubContext.Clients.All.SendAsync("SessionStateChanged", new { 
                sessionId, 
                action = "locked", 
                by = scannedBy, 
                timestamp = DateTime.Now 
            });

            return Result<ScanResultDto>.Success(result);
        }

        public async Task<Result<ScanResultDto>> ScanBoxBarcodeAsync(int sessionId, string barcode, string scannedBy)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null)
                return Result<ScanResultDto>.Failure("Session not found");

            if (session.Status == "SCAN_COMPLETED")
                return Result<ScanResultDto>.Failure("Session has been completed. No further scans allowed.");

            // Get the barcode from BarcodeRegistries to validate
            var barcodeRegistry = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.BarcodeValue == barcode && 
                                        b.SessionId == sessionId && 
                                        b.IsActive && 
                                        b.BarcodeType == "BOX");

            if (barcodeRegistry == null)
                return Result<ScanResultDto>.Failure("Invalid barcode for this session");

            // Get master barcode to check lock session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return Result<ScanResultDto>.Failure("Master QR code not found for this session");

            // Check if user is locked to this session
            var userLockResult = await CheckUserLockAsync(scannedBy);
            if (!userLockResult.IsSuccess)
                return Result<ScanResultDto>.Failure("You must scan Master QR first to lock to a session");

            var lockedSessionResult = await GetUserLockedSessionAsync(scannedBy);
            if (!lockedSessionResult.IsSuccess || lockedSessionResult.Value != masterBarcode.BarcodeValue)
                return Result<ScanResultDto>.Failure("You can only scan boxes from your locked session");

            // Check if master QR was scanned first
            var masterScanned = await IsMasterQRScannedAsync(sessionId);
            if (!masterScanned)
                return Result<ScanResultDto>.Failure("Please scan Master QR first");

            // Check if barcode already scanned
            if (barcodeRegistry.Status == "SCANNED")
                return Result<ScanResultDto>.Failure("Box already scanned");

            // Mark barcode as scanned using BarcodeService
            var markResult = await _barcodeService.MarkBarcodeAsScannedAsync(barcode, scannedBy);
            if (!markResult.IsSuccess)
                return Result<ScanResultDto>.Failure(markResult.Error ?? "Failed to mark barcode as scanned");

            // Record the scan in ScanningActivities for audit trail
            var scanActivity = new ScanningActivity
            {
                BarcodeValue = barcode,
                Action = "SCAN_BOX",
                UserId = scannedBy,
                Timestamp = DateTime.Now,
                Result = "SUCCESS"
            };

            _context.ScanningActivities.Add(scanActivity);
            await _context.SaveChangesAsync();

            // Send realtime update via SignalR
            await SendProgressUpdateAsync(sessionId, barcode);

            // Send barcode scanned notification to all clients in session
            await _hubContext.Clients.Group($"Session_{sessionId}")
                .SendAsync("BarcodeScanned", new { 
                    sessionId, 
                    barcode, 
                    scannedBy, 
                    barcodeType = "BOX",
                    timestamp = DateTime.Now 
                });

            // Broadcast barcode state change
            await _hubContext.Clients.Group($"Session_{sessionId}")
                .SendAsync("BarcodeStateChanged", new { 
                    sessionId, 
                    barcode, 
                    action = "scanned", 
                    by = scannedBy, 
                    timestamp = DateTime.Now 
                });

            var result = new ScanResultDto
            {
                BarcodeValue = barcode,
                ScanType = ScanItemType.Box,
                Message = "Box scanned successfully",
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            };

            return Result<ScanResultDto>.Success(result);
        }

        /// <summary>
        /// Universal scan method for Box, Pallet, or PCS items
        /// Determines scan type based on barcode content
        /// PHASE 2: Enhanced with PO Context support
        /// </summary>
        public async Task<Result<ScanResultDto>> ScanItemBarcodeAsync(int sessionId, string barcode, string scannedBy, int? selectedPOId = null)
        {
            try
            {
                // Log PO Context for debugging
                if (selectedPOId.HasValue)
                {
                    Console.WriteLine($"🎯 HIERARCHICAL SCAN: Session={sessionId}, PO={selectedPOId.Value}, Barcode={barcode}");
                }

                // Determine scan type based on barcode content
                var scanType = DetermineScanType(barcode);
                
                switch (scanType)
                {
                    case ScanItemType.Box:
                        return await ScanBoxBarcodeAsync(sessionId, barcode, scannedBy);
                    
                    case ScanItemType.Pallet:
                        return await ScanPalletItemAsync(sessionId, barcode, scannedBy, selectedPOId);
                    
                    case ScanItemType.Pcs:
                        return await ScanPcsItemAsync(sessionId, barcode, scannedBy, selectedPOId);
                    
                    default:
                        return Result<ScanResultDto>.Failure("Invalid barcode format. Barcode must contain 'BOX', 'PALLET', or start with '%Q' for PCS");
                }
            }
            catch (Exception ex)
            {
                return Result<ScanResultDto>.Failure($"Error processing scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Scan a pallet item - simplified for any barcode containing "PALLET"
        /// PHASE 2: Enhanced with PO Context tracking
        /// </summary>
        private async Task<Result<ScanResultDto>> ScanPalletItemAsync(int sessionId, string barcode, string scannedBy, int? selectedPOId = null)
        {
            // Basic validation - must be locked to a session first
            var userLockResult = await CheckUserLockAsync(scannedBy);
            if (!userLockResult.IsSuccess)
                return Result<ScanResultDto>.Failure("Please scan Master QR first to lock to a session");

            // Get PO data for this session to validate pallet quantity
            var poData = await GetPODataForSessionAsync(sessionId);
            if (!poData.Any())
                return Result<ScanResultDto>.Failure("No PO data found for this session");

            // If PO is selected, validate it exists in this session
            if (selectedPOId.HasValue)
            {
                var selectedPO = poData.FirstOrDefault(po => po.POId == selectedPOId.Value);
                if (selectedPO == null)
                    return Result<ScanResultDto>.Failure($"Selected PO {selectedPOId.Value} not found in this session");
                
                Console.WriteLine($"🎯 PALLET SCAN with PO Context: Session={sessionId}, PO={selectedPOId.Value}, Barcode={barcode}");
            }

            // Calculate total pallets and validate session has pallets
            var totalPallets = poData.Sum(po => po.QtyPallet);
            if (totalPallets == 0)
            {
                Console.WriteLine($"🚫 PALLET SCAN REJECTED: Session {sessionId} has no pallets in PO data (QtyPallet=0)");
                return Result<ScanResultDto>.Failure("This session does not contain any pallets. Pallet scanning is not allowed for this shipment.");
            }

            var scannedPallets = await GetScannedItemCountAsync(sessionId, ScanItemType.Pallet);

            // Relaxed validation - allow scanning if we haven't reached max pallets
            if (scannedPallets >= totalPallets)
                return Result<ScanResultDto>.Failure($"All pallets already scanned ({scannedPallets}/{totalPallets})");

            // Check if this specific barcode was already scanned (to prevent duplicates)
            var alreadyScanned = await IsItemAlreadyScannedAsync(barcode);
            if (alreadyScanned)
                return Result<ScanResultDto>.Failure("This pallet barcode has already been scanned");

            // Record the scan with PO Context - PHASE 2 Implementation
            var scanResult = await RecordItemScanAsync(sessionId, barcode, ScanItemType.Pallet, scannedBy, selectedPOId);
            if (!scanResult.IsSuccess)
                return Result<ScanResultDto>.Failure(scanResult.Error!);

            // Send real-time update
            await SendProgressUpdateAsync(sessionId, barcode);

            Console.WriteLine($"✅ PALLET SCAN SUCCESS: Session {sessionId}, POContext={selectedPOId}, Barcode {barcode}, Progress {scannedPallets + 1}/{totalPallets}");

            return Result<ScanResultDto>.Success(new ScanResultDto
            {
                BarcodeValue = barcode,
                ScanType = ScanItemType.Pallet,
                Message = $"Pallet scanned successfully ({scannedPallets + 1}/{totalPallets})" + (selectedPOId.HasValue ? $" [PO {selectedPOId.Value}]" : ""),
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            });
        }

        /// <summary>
        /// Scan a PCS item - simplified for any barcode starting with "%Q"
        /// PHASE 2: Enhanced with PO Context tracking
        /// </summary>
        private async Task<Result<ScanResultDto>> ScanPcsItemAsync(int sessionId, string barcode, string scannedBy, int? selectedPOId = null)
        {
            // Basic validation - must be locked to a session first
            var userLockResult = await CheckUserLockAsync(scannedBy);
            if (!userLockResult.IsSuccess)
                return Result<ScanResultDto>.Failure("Please scan Master QR first to lock to a session");

            // Get PO data for this session to validate pcs quantity
            var poData = await GetPODataForSessionAsync(sessionId);
            if (!poData.Any())
                return Result<ScanResultDto>.Failure("No PO data found for this session");

            // If PO is selected, validate it exists in this session
            if (selectedPOId.HasValue)
            {
                var selectedPO = poData.FirstOrDefault(po => po.POId == selectedPOId.Value);
                if (selectedPO == null)
                    return Result<ScanResultDto>.Failure($"Selected PO {selectedPOId.Value} not found in this session");
                
                Console.WriteLine($"🎯 PCS SCAN with PO Context: Session={sessionId}, PO={selectedPOId.Value}, Barcode={barcode}");
            }

            // Calculate total pcs and validate session has PCS
            var totalPcs = poData.Sum(po => po.QtyPcs);
            if (totalPcs == 0)
            {
                Console.WriteLine($"🚫 PCS SCAN REJECTED: Session {sessionId} has no PCS in PO data (QtyPcs=0)");
                return Result<ScanResultDto>.Failure("This session does not contain any PCS items. PCS scanning is not allowed for this shipment.");
            }

            var scannedPcs = await GetScannedItemCountAsync(sessionId, ScanItemType.Pcs);

            // Relaxed validation - allow scanning if we haven't reached max PCS
            if (scannedPcs >= totalPcs)
                return Result<ScanResultDto>.Failure($"All PCS items already scanned ({scannedPcs}/{totalPcs})");

            // Check if this specific barcode was already scanned (to prevent duplicates)
            var alreadyScanned = await IsItemAlreadyScannedAsync(barcode);
            if (alreadyScanned)
                return Result<ScanResultDto>.Failure("This PCS barcode has already been scanned");

            // Record the scan with PO Context - PHASE 2 Implementation
            var scanResult = await RecordItemScanAsync(sessionId, barcode, ScanItemType.Pcs, scannedBy, selectedPOId);
            if (!scanResult.IsSuccess)
                return Result<ScanResultDto>.Failure(scanResult.Error!);

            // Send real-time update
            await SendProgressUpdateAsync(sessionId, barcode);

            Console.WriteLine($"✅ PCS SCAN SUCCESS: Session {sessionId}, POContext={selectedPOId}, Barcode {barcode}, Progress {scannedPcs + 1}/{totalPcs}");

            return Result<ScanResultDto>.Success(new ScanResultDto
            {
                BarcodeValue = barcode,
                ScanType = ScanItemType.Pcs,
                Message = $"PCS item scanned successfully ({scannedPcs + 1}/{totalPcs})" + (selectedPOId.HasValue ? $" [PO {selectedPOId.Value}]" : ""),
                Timestamp = DateTime.Now,
                ScannedBy = scannedBy
            });
        }



        public async Task<ScanProgressDto> GetScanProgressAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new ScanProgressDto();

            // Get PO data to calculate quantities
            var poData = await GetPODataForSessionAsync(sessionId);
            
            // Calculate totals from PO data
            var totalBoxes = poData.Sum(po => po.QtyBox);
            var totalPallets = poData.Sum(po => po.QtyPallet);
            var totalPcs = poData.Sum(po => po.QtyPcs);
            
            // Calculate scanned counts
            var scannedBoxes = await GetScannedItemCountAsync(sessionId, ScanItemType.Box);
            var scannedPallets = await GetScannedItemCountAsync(sessionId, ScanItemType.Pallet);
            var scannedPcs = await GetScannedItemCountAsync(sessionId, ScanItemType.Pcs);
            
            // Legacy barcode system compatibility
            var totalBarcodes = await _barcodeService.GetTotalBarcodeCountAsync(sessionId);
            var scannedCount = await _barcodeService.GetScannedBarcodeCountAsync(sessionId);
            var masterScanned = await IsMasterQRScannedAsync(sessionId);

            // Calculate overall progress including all types
            var totalItems = totalBoxes + totalPallets + totalPcs;
            var scannedItems = scannedBoxes + scannedPallets + scannedPcs;
            var overallProgress = totalItems > 0 ? (double)scannedItems / totalItems * 100 : 0;

            return new ScanProgressDto
            {
                SessionId = sessionId,
                // Legacy fields for backward compatibility
                TotalBarcodes = Math.Max(totalBarcodes, totalItems),
                ScannedCount = Math.Max(scannedCount, scannedItems),
                ProgressPercentage = Math.Max(
                    totalBarcodes > 0 ? (double)scannedCount / totalBarcodes * 100 : 0,
                    overallProgress
                ),
                IsMasterScanned = masterScanned,
                LastScanTime = await GetLastScanTimeAsync(sessionId),
                
                // Enhanced tracking fields
                TotalBoxes = totalBoxes,
                ScannedBoxes = scannedBoxes,
                TotalPallets = totalPallets,
                ScannedPallets = scannedPallets,
                TotalPcs = totalPcs,
                ScannedPcs = scannedPcs,
                
                // Enhanced completion logic: use the higher value for accuracy
                CanComplete = masterScanned && 
                            ((totalItems > 0 && scannedItems >= totalItems) ||
                             (totalBarcodes > 0 && scannedCount >= totalBarcodes))
            };
        }

        public async Task<List<BarcodeItemDto>> GetBarcodeListAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new List<BarcodeItemDto>();

            var barcodes = await _barcodeService.GetBarcodesForSessionAsync(sessionId);
            
            return barcodes.Select(b => new BarcodeItemDto
            {
                BarcodeValue = b.BarcodeValue,
                ModelProduct = b.ModelProduct,
                BoxNumber = b.BoxNumber,
                IsScanned = b.ScannedDate.HasValue,
                ScannedTime = b.ScannedDate
            }).ToList();
        }

        public async Task<List<ScanHistoryDto>> GetScanHistoryAsync()
        {
            var activities = await _context.ScanningActivities
                .Where(sa => sa.Action != "AREA_ASSIGN") // Exclude area assignments from history
                .OrderByDescending(sa => sa.Timestamp)
                .Take(100)
                .ToListAsync();

            return activities.Select(a => new ScanHistoryDto
            {
                ActivityId = a.ActivityId,
                BarcodeValue = a.BarcodeValue,
                Action = a.Action,
                UserId = a.UserId,
                Timestamp = a.Timestamp,
                Result = a.Result,
                ErrorMessage = a.ErrorMessage
            }).ToList();
        }

        public async Task<ScanSessionDetailDto?> GetSessionDetailAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            // Get master barcode for this session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return null;

            var activities = await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(masterBarcode.BarcodeValue) || 
                           sa.BarcodeValue == masterBarcode.BarcodeValue)
                .Where(sa => sa.Action != "AREA_ASSIGN") // Exclude area assignments
                .OrderByDescending(sa => sa.Timestamp)
                .ToListAsync();

            return new ScanSessionDetailDto
            {
                SessionId = sessionId,
                FileName = session.FileName,
                QRIdentity = masterBarcode.BarcodeValue,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                TotalBoxes = session.TotalBoxes,
                ScanActivities = activities.Select(a => new ScanHistoryDto
                {
                    ActivityId = a.ActivityId,
                    BarcodeValue = a.BarcodeValue,
                    Action = a.Action,
                    UserId = a.UserId,
                    Timestamp = a.Timestamp,
                    Result = a.Result,
                    ErrorMessage = a.ErrorMessage
                }).ToList()
            };
        }

        public async Task<Result<bool>> CompleteScanAsync(int sessionId, string completedBy)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
            
            if (session == null)
                return Result<bool>.Failure("Session not found");

            // Check if already completed (idempotency)
            if (session.Status == "SCAN_COMPLETED")
            {
                Console.WriteLine($"🔄 Session {sessionId} already completed, skipping duplicate completion");
                return Result<bool>.Success(true);
            }

            var canComplete = await CanCompleteScanAsync(sessionId);
            if (!canComplete)
                return Result<bool>.Failure("Cannot complete scan. Not all items are scanned or master QR not scanned.");

            // Update session status
            session.Status = "SCAN_COMPLETED";
            
            // Update all POMaster status to COMPLETED
            foreach (var poMaster in session.POMasters)
            {
                poMaster.Status = "COMPLETED";
                Console.WriteLine($"✅ Updated POMaster {poMaster.NoPO} status to COMPLETED");
            }
            
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ AUTO-COMPLETE: Session {sessionId} completed by {completedBy}");

            // Send SignalR notification to all users in the session
            await _hubContext.Clients.Group($"Session_{sessionId}")
                .SendAsync("SessionUnlocked", new { 
                    sessionId, 
                    unlockedBy = completedBy, 
                    timestamp = DateTime.Now 
                });

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CheckUserLockAsync(string userId)
        {
            // Check if user has scanned a master QR for any active (not completed) session
            var userMasterScan = await _context.ScanningActivities
                .Where(sa => sa.UserId == userId && sa.Action == "SCAN_MASTER")
                .Join(_context.BarcodeRegistries,
                    sa => sa.BarcodeValue,
                    br => br.BarcodeValue,
                    (sa, br) => new { sa, br })
                .Where(joined => joined.br.BarcodeType == "MASTER" && joined.br.IsActive)
                .Join(_context.UploadSessions,
                    joined => joined.br.SessionId,
                    us => us.SessionId,
                    (joined, us) => new { joined.sa, joined.br, us })
                .Where(final => final.us.Status != "SCAN_COMPLETED")
                .OrderByDescending(final => final.sa.Timestamp)
                .FirstOrDefaultAsync();

            return userMasterScan != null 
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("User is not locked to any session");
        }

        public async Task<Result<string>> GetUserLockedSessionAsync(string userId)
        {
            var userMasterScan = await _context.ScanningActivities
                .Where(sa => sa.UserId == userId && sa.Action == "SCAN_MASTER")
                .Join(_context.BarcodeRegistries,
                    sa => sa.BarcodeValue,
                    br => br.BarcodeValue,
                    (sa, br) => new { sa, br })
                .Where(joined => joined.br.BarcodeType == "MASTER" && joined.br.IsActive)
                .Join(_context.UploadSessions,
                    joined => joined.br.SessionId,
                    us => us.SessionId,
                    (joined, us) => new { joined.sa, joined.br, us })
                .Where(final => final.us.Status != "SCAN_COMPLETED")
                .OrderByDescending(final => final.sa.Timestamp)
                .Select(final => final.sa.BarcodeValue)
                .FirstOrDefaultAsync();

            return userMasterScan != null 
                ? Result<string>.Success(userMasterScan)
                : Result<string>.Failure("User is not locked to any session");
        }

        // Helper methods
        private List<BarcodeDto> GenerateBarcodeList(UploadSession session)
        {
            var barcodes = new List<BarcodeDto>();
            
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    var barcode = $"QR_{session.SessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{i:D3}";
                    barcodes.Add(new BarcodeDto
                    {
                        BarcodeValue = barcode,
                        ModelProduct = poMaster.ModelProduk,
                        BoxNumber = i
                    });
                }
            }

            return barcodes;
        }

        private List<string> GenerateBarcodesForSession(UploadSession session)
        {
            var barcodes = new List<string>();
            
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    barcodes.Add($"QR_{session.SessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{i:D3}");
                }
            }

            return barcodes;
        }

        private async Task<List<string>> GetScannedBarcodesAsync(int sessionId)
        {
            // Get master barcode for this session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null) return new List<string>();

            return await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(masterBarcode.BarcodeValue) && sa.Action == "SCAN_BOX")
                .Select(sa => sa.BarcodeValue)
                .ToListAsync();
        }

        private async Task<bool> IsMasterQRScannedAsync(int sessionId)
        {
            // Get master barcode for this session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null) return false;

            return masterBarcode.Status == "SCANNED";
        }

        private string GetScanStatus(int sessionId)
        {
            var masterScanned = IsMasterQRScannedAsync(sessionId).Result;
            var progress = GetScanProgressAsync(sessionId).Result;

            if (progress.CanComplete) return "Completed";
            if (progress.ScannedCount > 0) return "In Progress";
            if (masterScanned) return "Ready";
            return "Pending";
        }



        private async Task<DateTime?> GetLastScanTimeAsync(int sessionId)
        {
            // Get master barcode for this session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null) return null;

            return await _context.ScanningActivities
                .Where(sa => sa.BarcodeValue.StartsWith(masterBarcode.BarcodeValue))
                .OrderByDescending(sa => sa.Timestamp)
                .Select(sa => (DateTime?)sa.Timestamp)
                .FirstOrDefaultAsync();
        }

        private DateTime? GetScanTime(string barcode, List<string> scannedBarcodes)
        {
            if (!scannedBarcodes.Contains(barcode)) return null;

            return _context.ScanningActivities
                .Where(sa => sa.BarcodeValue == barcode && sa.Action == "SCAN_BOX")
                .Select(sa => (DateTime?)sa.Timestamp)
                .FirstOrDefault();
        }

        private async Task<bool> CanCompleteScanAsync(int sessionId)
        {
            var progress = await GetScanProgressAsync(sessionId);
            
            // Enhanced completion logic: check all types are complete AND master is scanned
            var canComplete = progress.IsMasterScanned && progress.IsAllComplete;
            
            Console.WriteLine($"🔍 COMPLETION CHECK Session {sessionId}:");
            Console.WriteLine($"  📊 Master Scanned: {progress.IsMasterScanned}");
            Console.WriteLine($"  📦 Box: {progress.ScannedBoxes}/{progress.TotalBoxes} (Complete: {progress.IsBoxComplete})");
            Console.WriteLine($"  🚛 Pallet: {progress.ScannedPallets}/{progress.TotalPallets} (Complete: {progress.IsPalletComplete})");
            Console.WriteLine($"  🔢 PCS: {progress.ScannedPcs}/{progress.TotalPcs} (Complete: {progress.IsPcsComplete})");
            Console.WriteLine($"  ✅ All Complete: {progress.IsAllComplete}");
            Console.WriteLine($"  🎯 Can Complete: {canComplete}");
            
            return canComplete;
        }

        // Helper method to force QR generation for testing
        public async Task<Result<string>> GenerateQRForSessionAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return Result<string>.Failure("Session not found");

            if (!session.POMasters.Any())
                return Result<string>.Failure("Session has no PO data to scan");

            // Check if master barcode already exists
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode != null)
            {
                return Result<string>.Success(masterBarcode.BarcodeValue);
            }

            // Generate barcodes if they don't exist
            if (string.IsNullOrEmpty(session.IdentityQRCode))
            {
                session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
                session.Status = "QR_GENERATED";
                _context.UploadSessions.Update(session);
                await _context.SaveChangesAsync();

                // Generate barcodes for this session
                await _barcodeService.GenerateBarcodesForSessionAsync(sessionId, "System");

                // Get the newly created master barcode
                masterBarcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                            b.BarcodeType == "MASTER" && 
                                            b.IsActive);

                if (masterBarcode != null)
                {
                    return Result<string>.Success(masterBarcode.BarcodeValue);
                }
            }

            return Result<string>.Failure("Failed to generate master QR code");
        }

        #region Helper Methods for Enhanced Scanning

        /// <summary>
        /// Determines scan type based on barcode content
        /// </summary>
        private ScanItemType DetermineScanType(string barcode)
        {
            var upperBarcode = barcode.ToUpperInvariant();
            
            if (upperBarcode.Contains("PALLET"))
                return ScanItemType.Pallet;
            
            if (upperBarcode.StartsWith("%Q"))
                return ScanItemType.Pcs;
            
            if (upperBarcode.Contains("BOX"))
                return ScanItemType.Box;
            
            // Default to Box if no specific type found
            return ScanItemType.Box;
        }

        /// <summary>
        /// Validates user lock and session consistency
        /// </summary>
        private async Task<Result<bool>> ValidateUserLockAndSession(int sessionId, string scannedBy)
        {
            // Get master barcode to check lock session
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return Result<bool>.Failure("Master QR code not found for this session");

            // Check if user is locked to this session
            var userLockResult = await CheckUserLockAsync(scannedBy);
            if (!userLockResult.IsSuccess)
                return Result<bool>.Failure("You must scan Master QR first to lock to a session");

            var lockedSessionResult = await GetUserLockedSessionAsync(scannedBy);
            if (!lockedSessionResult.IsSuccess || lockedSessionResult.Value != masterBarcode.BarcodeValue)
                return Result<bool>.Failure("You can only scan items from your locked session");

            // Check if master QR was scanned first
            var masterScanned = await IsMasterQRScannedAsync(sessionId);
            if (!masterScanned)
                return Result<bool>.Failure("Please scan Master QR first");

            return Result<bool>.Success(true);
        }

        /// <summary>
        /// Gets PO data for a specific session
        /// </summary>
        private async Task<List<POMaster>> GetPODataForSessionAsync(int sessionId)
        {
            return await _context.POMasters
                .Where(po => po.SourceSessionId == sessionId)
                .ToListAsync();
        }

        /// <summary>
        /// Gets count of scanned items for a specific type and session
        /// </summary>
        private async Task<int> GetScannedItemCountAsync(int sessionId, ScanItemType itemType)
        {
            var actionName = GetActionNameForType(itemType);
            var sessionAreaTag = $"Session_{sessionId}";
            
            if (itemType == ScanItemType.Box)
            {
                // For BOX items, use the new barcode format: QR_{sessionId}_
                var sessionPrefix = $"QR_{sessionId}_";
                
                // Count from BarcodeRegistry (more accurate for BOX items)
                var scannedBoxCount = await _context.BarcodeRegistries
                    .CountAsync(b => b.SessionId == sessionId && 
                                   b.BarcodeType == "BOX" && 
                                   b.IsActive && 
                                   b.Status == "SCANNED");
                
                Console.WriteLine($"🔍 BOX SCAN COUNT: Session {sessionId} has {scannedBoxCount} scanned boxes");
                return scannedBoxCount;
            }
            else
            {
                // For PALLET and PCS items, use the AssignedArea field to track session
                return await _context.ScanningActivities
                    .CountAsync(sa => sa.Action == actionName && 
                                    sa.Result == "SUCCESS" &&
                                    sa.AssignedArea == sessionAreaTag);
            }
        }

        /// <summary>
        /// Gets action name for scan type
        /// </summary>
        private string GetActionNameForType(ScanItemType itemType)
        {
            return itemType switch
            {
                ScanItemType.Box => "SCAN_BOX",
                ScanItemType.Pallet => "SCAN_PALLET",
                ScanItemType.Pcs => "SCAN_PCS",
                _ => "SCAN_ITEM"
            };
        }

        /// <summary>
        /// Checks if a specific barcode has already been scanned
        /// </summary>
        private async Task<bool> IsItemAlreadyScannedAsync(string barcode)
        {
            return await _context.ScanningActivities
                .AnyAsync(sa => sa.BarcodeValue == barcode && 
                              sa.Result == "SUCCESS" &&
                              (sa.Action == "SCAN_BOX" || sa.Action == "SCAN_PALLET" || sa.Action == "SCAN_PCS"));
        }

        /// <summary>
        /// Records an item scan in the database
        /// </summary>
        private async Task<Result<bool>> RecordItemScanAsync(int sessionId, string barcode, ScanItemType itemType, string scannedBy, int? selectedPOId = null)
        {
            try
            {
                var scanActivity = new ScanningActivity
                {
                    BarcodeValue = barcode,
                    Action = GetActionNameForType(itemType),
                    UserId = scannedBy,
                    Timestamp = DateTime.Now,
                    Result = "SUCCESS",
                    // Store session info using AssignedArea field for session tracking
                    AssignedArea = $"Session_{sessionId}",
                    // Store PO Context for hierarchical lock tracking - PHASE 1 Implementation
                    POContext = selectedPOId.HasValue ? $"PO_{selectedPOId.Value}" : null
                };

                _context.ScanningActivities.Add(scanActivity);
                await _context.SaveChangesAsync();

                Console.WriteLine($"📊 SCAN RECORDED: Session={sessionId}, Type={itemType}, POContext={scanActivity.POContext}, Barcode={barcode}");

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to record scan: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Get recent scanned items for a specific session - Simplified
        /// </summary>
        public async Task<RecentScansResponseDto> GetRecentScansAsync(int sessionId, int limit = 500)
        {
            try
            {
                // Get session information
                var session = await _context.UploadSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return new RecentScansResponseDto();

                // Ensure session has master (optional for recent view)
                var hasMaster = await _context.BarcodeRegistries
                    .AnyAsync(b => b.SessionId == sessionId && b.BarcodeType == "MASTER" && b.IsActive);

                // Get ALL BOX barcodes for the locked session
                var allSessionBarcodes = await _context.BarcodeRegistries
                    .Include(b => b.POMaster)
                    .Where(b => b.SessionId == sessionId && 
                               (b.BarcodeType == "BOX" || b.BarcodeValue.Contains("_BOX_")) && 
                               b.IsActive)
                    .OrderBy(b => b.BarcodeValue)
                    .ToListAsync();

                // Debug info to verify BOX count
                var expectedBoxCount = await _context.POMasters
                    .Where(p => p.SourceSessionId == sessionId)
                    .SumAsync(p => p.QtyBox);

                if (allSessionBarcodes.Count != expectedBoxCount)
                {
                    // Auto-fix missing barcodes if count doesn't match
                    await _barcodeService.AutoFixMissingBarcodesAsync(sessionId, "recent_scan_auto_fix");
                    
                    // Re-query after fix
                    allSessionBarcodes = await _context.BarcodeRegistries
                        .Include(b => b.POMaster)
                        .Where(b => b.SessionId == sessionId && 
                                   (b.BarcodeType == "BOX" || b.BarcodeValue.Contains("_BOX_")) && 
                                   b.IsActive)
                        .OrderBy(b => b.BarcodeValue)
                        .ToListAsync();
                }

                // Get scanning activities for this session
                var sessionPrefix = $"QR_{sessionId}_";
                var scanningActivities = await _context.ScanningActivities
                    .Where(sa => sa.Result == "SUCCESS" && sa.Action != "SCAN_MASTER" && 
                                (sa.BarcodeValue.StartsWith(sessionPrefix) || 
                                 sa.AssignedArea == $"Session_{sessionId}"))
                    .ToListAsync();

                // Categorize scans by type
                var boxScans = scanningActivities.Where(sa => 
                    sa.Action == "SCAN_BOX" || 
                    sa.BarcodeValue.Contains("_BOX_")).ToList();
                var palletScans = scanningActivities.Where(sa => 
                    sa.Action == "SCAN_PALLET" || 
                    sa.BarcodeValue.Contains("_PALLET_")).ToList();
                var pcsScans = scanningActivities.Where(sa => 
                    sa.Action == "SCAN_PCS" || 
                    sa.BarcodeValue.StartsWith("%Q")).ToList();
                Console.WriteLine($"📦 Found {pcsScans.Count} PCS scans for session {sessionId}");
                Console.WriteLine($"� Found {boxScans.Count} BOX scans for session {sessionId}");

                // Build scan list - BOX items show all (scanned & pending), PALLET/PCS only if scanned
                var enrichedScans = new List<RecentScanDto>();
                int sequenceNumber = 1;
                
                // Show ALL BOX items (both scanned and pending) for visibility
                foreach (var barcode in allSessionBarcodes)
                {
                    // Check if this barcode has been scanned (improved detection)
                    var scanActivity = boxScans
                        .Where(sa => sa.BarcodeValue == barcode.BarcodeValue)
                        .OrderByDescending(sa => sa.Timestamp)
                        .FirstOrDefault();

                    var isScanned = scanActivity != null;
                    
                    var recentScan = new RecentScanDto
                    {
                        BarcodeValue = barcode.BarcodeValue,
                        ItemType = "BOX",
                        ScannedAt = scanActivity?.Timestamp ?? DateTime.MinValue,
                        ScannedBy = scanActivity?.UserId ?? "",
                        SequenceNumber = sequenceNumber++,
                        
                        // Enhanced information from barcode registry and PO
                        PONumber = barcode?.POMaster?.NoPO ?? "N/A",
                        ModelProduct = barcode?.ModelProduct ?? 
                                     barcode?.POMaster?.ModelProduk ?? 
                                     (barcode?.BarcodeType ?? "UNKNOWN"),
                        Description = barcode?.POMaster?.ShipmentDetail ?? "",
                        Quantity = barcode?.POMaster?.QtyTotal,
                        Status = isScanned ? "SCANNED" : "PENDING",
                        Container = barcode?.POMaster?.Container ?? "",
                        ShipmentDetail = barcode?.POMaster?.ShipmentDetail ?? "",
                        IsCompleted = isScanned
                    };

                    enrichedScans.Add(recentScan);
                }

                // Add only actual pallet scans recorded in ScanningActivities
                foreach (var palletScan in palletScans)
                {
                    if (!enrichedScans.Any(es => es.BarcodeValue.Equals(palletScan.BarcodeValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        enrichedScans.Add(new RecentScanDto
                        {
                            BarcodeValue = palletScan.BarcodeValue,
                            ItemType = "PALLET",
                            ScannedAt = palletScan.Timestamp,
                            ScannedBy = palletScan.UserId ?? "",
                            SequenceNumber = sequenceNumber++,
                            PONumber = "",
                            ModelProduct = "PALLET",
                            Description = "",
                            Quantity = null,
                            Status = "SCANNED",
                            Container = "",
                            ShipmentDetail = "",
                            IsCompleted = true
                        });
                    }
                }

                // Add only actual PCS scans recorded in ScanningActivities
                foreach (var pcsScan in pcsScans)
                {
                    if (!enrichedScans.Any(es => es.BarcodeValue.Equals(pcsScan.BarcodeValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        enrichedScans.Add(new RecentScanDto
                        {
                            BarcodeValue = pcsScan.BarcodeValue,
                            ItemType = "PCS",
                            ScannedAt = pcsScan.Timestamp,
                            ScannedBy = pcsScan.UserId ?? "",
                            SequenceNumber = sequenceNumber++,
                            PONumber = "",
                            ModelProduct = "PCS",
                            Description = "",
                            Quantity = null,
                            Status = "SCANNED",
                            Container = "",
                            ShipmentDetail = "",
                            IsCompleted = true
                        });
                    }
                }

                // Accurate totals by SourceSessionId from PO Master
                var poQuery = _context.POMasters.Where(po => po.SourceSessionId == sessionId);
                var poTotals = await poQuery
                    .GroupBy(_ => 1)
                    .Select(g => new {
                        QtyBox = g.Sum(x => x.QtyBox),
                        QtyPallet = g.Sum(x => x.QtyPallet),
                        QtyPcs = g.Sum(x => x.QtyPcs),
                        QtyTotal = g.Sum(x => x.QtyTotal)
                    })
                    .FirstOrDefaultAsync();

                // Scanned counts per type (updated calculation)
                var boxScanned = boxScans.Count;
                var palletScanned = palletScans.Count;
                var pcsScanned = pcsScans.Count;

                // Compose credible counts for header badge using PO totals
                var totalCount = (poTotals?.QtyBox ?? 0) + (poTotals?.QtyPallet ?? 0) + (poTotals?.QtyPcs ?? 0);
                var scannedCount = boxScanned + palletScanned + pcsScanned;
                var pendingCount = Math.Max(0, totalCount - scannedCount);

                // Get today's scan count  
                var today = DateTime.Today;
                var totalToday = await _context.ScanningActivities
                    .CountAsync(sa => sa.Timestamp >= today && 
                                    sa.Result == "SUCCESS" && 
                                    sa.Action != "SCAN_MASTER");

                // Get last scan time
                var lastScannedItem = enrichedScans.Where(s => s.IsCompleted).OrderByDescending(s => s.ScannedAt).FirstOrDefault();
                var lastScanTime = lastScannedItem?.ScannedAt.ToString("HH:mm:ss") ?? "N/A";

                Console.WriteLine($"🎯 DEBUG: Returning {enrichedScans.Count} scans (Scanned: {scannedCount}, Pending: {pendingCount})");

                return new RecentScansResponseDto
                {
                    RecentScans = enrichedScans
                        .OrderByDescending(s => s.ScannedAt == DateTime.MinValue ? DateTime.MinValue : s.ScannedAt)
                        .Take(limit)
                        .ToList(),
                    TotalCount = totalCount,
                    ScannedCount = scannedCount,
                    PendingCount = pendingCount,
                    TotalScannedToday = totalToday,
                    SessionInfo = $"{session.FileName} ({session.SheetName})",
                    LastScanTime = lastScanTime,
                    Stats = new RecentStatsDto
                    {
                        BoxTotal = poTotals?.QtyBox ?? 0,
                        PalletTotal = poTotals?.QtyPallet ?? 0,
                        PcsTotal = poTotals?.QtyPcs ?? 0,
                        BoxScanned = boxScanned,
                        PalletScanned = palletScanned,
                        PcsScanned = pcsScanned
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recent scans: {ex.Message}");
                return new RecentScansResponseDto
                {
                    RecentScans = new List<RecentScanDto>(),
                    TotalCount = 0,
                    TotalScannedToday = 0,
                    SessionInfo = "Error loading session",
                    LastScanTime = "N/A"
                };
            }
        }

        /// <summary>
        /// Helper method to get item type from action name
        /// </summary>
        private string GetItemTypeFromAction(string action)
        {
            return action switch
            {
                "SCAN_BOX" => "BOX",
                "SCAN_PALLET" => "PALLET", 
                "SCAN_PCS" => "PCS",
                _ => "BOX"
            };
        }

        private async Task SendProgressUpdateAsync(int sessionId, string scannedBarcode)
        {
            try
            {
                // Extract PO information from barcode to get the specific PO that was updated
                var barcodeRegistry = await _context.BarcodeRegistries
                    .Include(b => b.POMaster)
                    .FirstOrDefaultAsync(b => b.BarcodeValue == scannedBarcode && b.SessionId == sessionId);

                if (barcodeRegistry?.POId != null)
                {
                    // Get updated progress for the specific PO
                    var poProgress = await _barcodeService.GetProgressByPOIdAsync(sessionId, barcodeRegistry.POId.Value);
                    
                    if (poProgress != null)
                    {
                        // Send update to all clients in the session group
                        await _hubContext.Clients.Group($"Session_{sessionId}")
                            .SendAsync("ProgressUpdate", new
                            {
                                sessionId = sessionId,
                                poId = barcodeRegistry.POId.Value,
                                progress = poProgress,
                                lastScannedBarcode = scannedBarcode
                            });
                    }
                }

                // Also send overall session progress
                var overallProgress = await _barcodeService.GetProgressByPOAsync(sessionId);
                await _hubContext.Clients.Group($"Session_{sessionId}")
                    .SendAsync("SessionProgressUpdate", new
                    {
                        sessionId = sessionId,
                        allProgress = overallProgress,
                        lastScannedBarcode = scannedBarcode
                    });

                // 🚀 AUTO-UNLOCK: Check if session is now complete after this scan
                await CheckAndAutoCompleteSessionAsync(sessionId, scannedBarcode);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the scan operation
                Console.WriteLine($"Error sending progress update: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if session should be auto-completed and triggers completion if ready
        /// </summary>
        private async Task CheckAndAutoCompleteSessionAsync(int sessionId, string lastScannedBarcode)
        {
            try
            {
                Console.WriteLine($"🔍 AUTO-COMPLETE CHECK: Starting for session {sessionId} after scanning {lastScannedBarcode}");
                
                // Get current session status to avoid unnecessary work
                var session = await _context.UploadSessions
                    .Select(s => new { s.SessionId, s.Status })
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    Console.WriteLine($"❌ AUTO-COMPLETE: Session {sessionId} not found");
                    return; // Session not found
                }

                if (session.Status == "SCAN_COMPLETED")
                {
                    Console.WriteLine($"⏭️ AUTO-COMPLETE: Session {sessionId} already completed, skipping");
                    return; // Session already completed
                }

                Console.WriteLine($"📊 AUTO-COMPLETE: Session {sessionId} status is '{session.Status}', checking completion...");

                // Check if all scanning is complete
                var canComplete = await CanCompleteScanAsync(sessionId);
                if (canComplete)
                {
                    Console.WriteLine($"🎯 AUTO-COMPLETE TRIGGERED: Session {sessionId} is 100% complete after scanning {lastScannedBarcode}");
                    
                    // Auto-complete the session
                    var completionResult = await CompleteScanAsync(sessionId, "System Auto-Complete");
                    
                    if (completionResult.IsSuccess)
                    {
                        Console.WriteLine($"✅ AUTO-COMPLETE SUCCESS: Session {sessionId} automatically completed");
                        
                        // Send enhanced completion notification to all clients
                        await _hubContext.Clients.Group($"Session_{sessionId}")
                            .SendAsync("SessionAutoCompleted", new
                            {
                                sessionId = sessionId,
                                completedBy = "System Auto-Complete",
                                lastScannedBarcode = lastScannedBarcode,
                                timestamp = DateTime.Now,
                                message = "All items scanned successfully! Session completed automatically."
                            });
                    }
                    else
                    {
                        Console.WriteLine($"❌ AUTO-COMPLETE FAILED: Session {sessionId} - {completionResult.Error}");
                    }
                }
                else
                {
                    Console.WriteLine($"⏳ Session {sessionId} not yet complete after scanning {lastScannedBarcode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in auto-completion check for session {sessionId}: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }
    }
}
