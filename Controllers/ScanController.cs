using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Repositories;
using System.Security.Claims;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireScanner)]
    public class ScanController : Controller
    {
        private readonly IScanningService _scanService;
        private readonly IExcelProcessingService _excelService;
        private readonly IBarcodeService _barcodeService;
        private readonly IPOValidationService _poValidationService; // Phase 2 addition
        private readonly AppDbContext _context;

        public ScanController(
            IScanningService scanService, 
            IExcelProcessingService excelService, 
            IBarcodeService barcodeService, 
            IPOValidationService poValidationService, // Phase 2 addition
            AppDbContext context)
        {
            _scanService = scanService;
            _excelService = excelService;
            _barcodeService = barcodeService;
            _poValidationService = poValidationService; // Phase 2 addition
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeSessions = await _scanService.GetActiveSessionsAsync();
            
            // Check if current user is locked to a session
            var userName = User.Identity?.Name ?? "Unknown";
            var userLockResult = await _scanService.CheckUserLockAsync(userName);
            
            if (userLockResult.IsSuccess)
            {
                var lockedSessionResult = await _scanService.GetUserLockedSessionAsync(userName);
                if (lockedSessionResult.IsSuccess)
                {
                    ViewBag.UserLockedSession = lockedSessionResult.Value;
                    TempData["Info"] = $"You are currently locked to session: {lockedSessionResult.Value}. Complete this session to unlock.";
                }
            }
            
            return View(activeSessions);
        }

        [HttpGet]
        public async Task<IActionResult> StartScan(int sessionId)
        {
            var scanData = await _scanService.GetScanSessionAsync(sessionId);
            if (scanData == null)
            {
                TempData["Error"] = "Session not found or QR not generated yet.";
                return RedirectToAction("Index");
            }

            return View(scanData);
        }

        [HttpPost]
        public async Task<IActionResult> ScanMasterQR(int sessionId, string qrCode)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _scanService.ScanMasterQRAsync(sessionId, qrCode, userName);
                
                if (result.IsSuccess)
                {
                    return Json(new { 
                        success = true, 
                        message = "Master QR scanned successfully!", 
                        data = result.Value 
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Error });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error scanning Master QR: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ScanBoxBarcode(int sessionId, string barcode)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                
                // Debug logging - let's see what's happening
                var session = await _excelService.GetAllPOSessionsAsync();
                var currentSession = session.FirstOrDefault(s => s.SessionId == sessionId);
                
                var result = await _scanService.ScanBoxBarcodeAsync(sessionId, barcode, userName);
                
                if (result.IsSuccess)
                {
                    // Get updated progress after successful scan
                    var progress = await _scanService.GetScanProgressAsync(sessionId);
                    
                    return Json(new { 
                        success = true, 
                        message = "Box scanned successfully!", 
                        data = new {
                            BarcodeValue = result.Value?.BarcodeValue ?? barcode,
                            ScanType = result.Value?.ScanTypeString ?? "BOX_BARCODE",
                            Message = result.Value?.Message ?? "Box scanned successfully",
                            Timestamp = result.Value?.Timestamp ?? DateTime.Now,
                            ScannedBy = result.Value?.ScannedBy ?? userName,
                            // Add progress data
                            scannedCount = progress.ScannedCount,
                            totalBarcodes = progress.TotalBarcodes,
                            progressPercentage = progress.ProgressPercentage,
                            canComplete = progress.CanComplete
                        }
                    });
                }
                else
                {
                    // Enhanced error response with debug info
                    return Json(new { 
                        success = false, 
                        message = result.Error,
                        debug = new {
                            sessionId = sessionId,
                            barcode = barcode,
                            sessionQR = currentSession?.QRIdentity,
                            barcodeStartsWithQR = barcode.StartsWith(currentSession?.QRIdentity ?? ""),
                            sessionExists = currentSession != null,
                            sessionStatus = currentSession?.Status,
                            activeSessions = session.Count()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error scanning box: {ex.Message}" });
            }
        }

        /// <summary>
        /// Universal scan endpoint for Box, Pallet, or PCS items
        /// Automatically determines item type based on barcode content
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ScanItem(int sessionId, string barcode, int? selectedPOId = null)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                
                // Validate input
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    return Json(new { success = false, message = "Barcode cannot be empty" });
                }
                
                // Enhanced debug logging with PO Context
                Console.WriteLine($"=== SCAN ITEM DEBUG ===");
                Console.WriteLine($"SessionId: {sessionId}");
                Console.WriteLine($"Barcode: {barcode}");
                Console.WriteLine($"User: {userName}");
                Console.WriteLine($"Selected PO: {selectedPOId}");
                Console.WriteLine($"Barcode Length: {barcode.Length}");
                Console.WriteLine($"Contains PALLET: {barcode.ToUpperInvariant().Contains("PALLET")}");
                Console.WriteLine($"Starts with %Q (PCS): {barcode.ToUpperInvariant().StartsWith("%Q")}");
                Console.WriteLine($"Contains BOX: {barcode.ToUpperInvariant().Contains("BOX")}");
                
                // PHASE 2: Pass selectedPOId to service for hierarchical lock tracking
                var result = await _scanService.ScanItemBarcodeAsync(sessionId, barcode, userName, selectedPOId);
                
                Console.WriteLine($"Scan Result Success: {result.IsSuccess}");
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"Scan Error: {result.Error}");
                }
                Console.WriteLine($"=== END SCAN ITEM DEBUG ===");
                
                if (result.IsSuccess)
                {
                    // Get updated progress after successful scan
                    var progress = await _scanService.GetScanProgressAsync(sessionId);
                    
                    return Json(new { 
                        success = true, 
                        message = result.Value?.Message ?? "Item scanned successfully!", 
                        data = new {
                            BarcodeValue = result.Value?.BarcodeValue ?? barcode,
                            ScanType = result.Value?.ScanTypeString ?? "ITEM",
                            Message = result.Value?.Message ?? "Item scanned successfully",
                            Timestamp = result.Value?.Timestamp ?? DateTime.Now,
                            ScannedBy = result.Value?.ScannedBy ?? userName,
                            
                            // Enhanced progress data
                            scannedCount = progress.ScannedCount,
                            totalBarcodes = progress.TotalBarcodes,
                            progressPercentage = progress.ProgressPercentage,
                            
                            // Individual type progress
                            boxProgress = new {
                                scanned = progress.ScannedBoxes,
                                total = progress.TotalBoxes,
                                percentage = progress.BoxProgressPercentage,
                                complete = progress.IsBoxComplete
                            },
                            palletProgress = new {
                                scanned = progress.ScannedPallets,
                                total = progress.TotalPallets,
                                percentage = progress.PalletProgressPercentage,
                                complete = progress.IsPalletComplete
                            },
                            pcsProgress = new {
                                scanned = progress.ScannedPcs,
                                total = progress.TotalPcs,
                                percentage = progress.PcsProgressPercentage,
                                complete = progress.IsPcsComplete
                            },
                            
                            canComplete = progress.CanComplete,
                            isAllComplete = progress.IsAllComplete
                        }
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = result.Error,
                        debug = new {
                            sessionId = sessionId,
                            barcode = barcode,
                            barcodeType = DetermineItemTypeFromBarcode(barcode)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error scanning item: {ex.Message}" });
            }
        }

        /// <summary>
        /// Helper method to determine item type from barcode for debugging
        /// </summary>
        private string DetermineItemTypeFromBarcode(string barcode)
        {
            var upperBarcode = barcode.ToUpperInvariant();
            
            if (upperBarcode.Contains("PALLET"))
                return "PALLET";
            
            if (upperBarcode.StartsWith("%Q"))
                return "PCS";
            
            if (upperBarcode.Contains("BOX"))
                return "BOX";
            
            return "UNKNOWN";
        }



        [HttpGet]
        public async Task<IActionResult> GetScanProgress(int sessionId)
        {
            try
            {
                var progress = await _scanService.GetScanProgressAsync(sessionId);
                return Json(new { success = true, data = progress });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBarcodeList(int sessionId)
        {
            try
            {
                var barcodes = await _scanService.GetBarcodeListAsync(sessionId);
                return Json(new { success = true, data = barcodes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResolveSessionByBarcode(string barcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                    return Json(new { success = false, message = "Barcode is required" });

                var registry = await _barcodeService.GetBarcodeByValueAsync(barcode);
                if (registry == null)
                    return Json(new { success = false, message = "Barcode not found" });

                var sessionId = registry.SessionId;
                
                // Get the master QR from BarcodeRegistry instead of IdentityQRCode
                var masterBarcodeRegistry = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                            b.BarcodeType == "MASTER" && 
                                            b.IsActive);

                var qrIdentity = masterBarcodeRegistry?.BarcodeValue ?? registry.Session?.IdentityQRCode ?? string.Empty;
                var fileName = registry.Session?.FileName ?? string.Empty;

                if (string.IsNullOrEmpty(qrIdentity))
                    return Json(new { success = false, message = "Master QR not found for this barcode" });

                // Return additional debug info for troubleshooting
                return Json(new { 
                    success = true, 
                    data = new { 
                        sessionId, 
                        qrIdentity, 
                        fileName,
                        barcodeType = registry.BarcodeType,
                        status = registry.Status
                    } 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugSession(int sessionId)
        {
            try
            {
                var scanSession = await _scanService.GetScanSessionAsync(sessionId);
                var poSessions = await _excelService.GetAllPOSessionsAsync();
                var currentSession = poSessions.FirstOrDefault(s => s.SessionId == sessionId);
                
                return Json(new { 
                    success = true, 
                    data = new {
                        scanSession = scanSession,
                        poSession = currentSession,
                        validBarcodes = scanSession?.BarcodeList?.Take(10), // Show first 10 for debugging
                        qrIdentity = scanSession?.QRIdentity
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> History()
        {
            var history = await _scanService.GetScanHistoryAsync();
            return View(history);
        }

        public async Task<IActionResult> SessionDetail(int sessionId)
        {
            var detail = await _scanService.GetSessionDetailAsync(sessionId);
            if (detail == null)
            {
                TempData["Error"] = "Session detail not found.";
                return RedirectToAction("Index");
            }

            return View(detail);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteScan(int sessionId)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _scanService.CompleteScanAsync(sessionId, userName);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, message = "Scanning completed successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Error ?? "Failed to complete scan." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error completing scan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckUserLock()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var userLockResult = await _scanService.CheckUserLockAsync(userName);
                
                if (userLockResult.IsSuccess)
                {
                    var lockedSessionResult = await _scanService.GetUserLockedSessionAsync(userName);
                    return Json(new { 
                        success = true, 
                        isLocked = true,
                        lockedSession = lockedSessionResult.IsSuccess ? lockedSessionResult.Value : null
                    });
                }
                else
                {
                    return Json(new { success = true, isLocked = false });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQRForSession(int sessionId)
        {
            try
            {
                var result = await _scanService.GenerateQRForSessionAsync(sessionId);
                
                if (result.IsSuccess)
                {
                    return Json(new { success = true, qrIdentity = result.Value, message = "QR generated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Error });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser()
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                
                // Find user's current lock and remove all scanning activities that created the lock
                var userLockResult = await _scanService.GetUserLockedSessionAsync(userName);
                if (userLockResult.IsSuccess)
                {
                    // This is a simple unlock - in production you might want more validation
                    // For now, we'll complete the session to unlock the user
                    var sessions = await _scanService.GetActiveSessionsAsync();
                    var lockedSession = sessions.FirstOrDefault(s => s.QRIdentity == userLockResult.Value);
                    
                    if (lockedSession != null)
                    {
                        var completeResult = await _scanService.CompleteScanAsync(lockedSession.SessionId, userName);
                        if (completeResult.IsSuccess)
                        {
                            return Json(new { success = true, message = "User unlocked successfully!" });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Failed to unlock user: " + completeResult.Error });
                        }
                    }
                }
                
                return Json(new { success = false, message = "User is not locked to any session" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/scan/active-sessions")]
        public async Task<IActionResult> GetActiveSessionsJson()
        {
            try
            {
                var sessions = await _scanService.GetActiveSessionsAsync();
                
                var sessionData = sessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    fileName = s.FileName,
                    qrIdentity = s.QRIdentity,
                    shipmentType = s.ShipmentType,
                    shipmentDate = s.ShipmentDate,
                    totalBoxes = s.TotalBoxes,
                    totalPOs = s.TotalPOs,
                    createdDate = s.CreatedDate,
                    createdBy = s.CreatedBy,
                    status = s.Status
                }).ToList();

                return Json(new { success = true, data = sessionData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentScans(int sessionId, int limit = 10)
        {
            try
            {
                var recentScans = await _scanService.GetRecentScansAsync(sessionId, limit);
                return Json(new { success = true, data = recentScans });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== PHASE 2: HIERARCHICAL LOCK VALIDATION ENDPOINTS =====

        /// <summary>
        /// Validates if a session is still active and scannable
        /// Critical for localStorage sync and zero-failure operation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ValidateSessionState(int sessionId)
        {
            try
            {
                var result = await _poValidationService.ValidateSessionStateAsync(sessionId);
                
                if (result.IsSuccess && result.Value != null)
                {
                    return Json(new { 
                        success = true, 
                        data = result.Value,
                        sessionId = sessionId,
                        status = result.Value.Status,
                        canScan = result.Value.CanScan,
                        completionReason = result.Value.CompletionReason
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = result.Error ?? "Unknown validation error",
                        sessionId = sessionId 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    sessionId = sessionId 
                });
            }
        }

        /// <summary>
        /// Validates if a PO is still active and has scannable items
        /// Essential for PO context validation in hierarchical lock
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ValidatePOState(int poId)
        {
            try
            {
                var result = await _poValidationService.ValidatePOStateAsync(poId);
                
                if (result.IsSuccess && result.Value != null)
                {
                    return Json(new { 
                        success = true, 
                        data = result.Value,
                        poId = poId,
                        isCompleted = result.Value.IsCompleted,
                        canContinueScanning = result.Value.CanContinueScanning,
                        nextAvailableItemType = result.Value.NextAvailableItemType
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = result.Error ?? "Unknown validation error",
                        poId = poId 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    poId = poId 
                });
            }
        }

        /// <summary>
        /// Gets all available POs for a session that can be scanned
        /// Used for PO selection UI in hierarchical lock system
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailablePOs(int sessionId)
        {
            try
            {
                var result = await _poValidationService.GetAvailablePOsAsync(sessionId);
                
                if (result.IsSuccess && result.Value != null)
                {
                    return Json(new { 
                        success = true, 
                        data = result.Value,
                        sessionId = sessionId,
                        totalPOs = result.Value.Count,
                        availablePOs = result.Value.Count(po => po.IsAvailable)
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = result.Error ?? "Unknown validation error",
                        sessionId = sessionId 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = ex.Message,
                    sessionId = sessionId 
                });
            }
        }

        /// <summary>
        /// Enhanced recent scans with PO filtering capability
        /// Supports both session-wide and PO-specific views
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecentScansEnhanced(int sessionId, int? poId = null, int limit = 50)
        {
            try
            {
                RecentScansResponseDto recentScans;
                
                if (poId.HasValue)
                {
                    // PO-specific recent scans for hierarchical lock context
                    recentScans = await GetPOSpecificRecentScansAsync(sessionId, poId.Value, limit);
                }
                else
                {
                    // Session-wide recent scans (existing functionality)
                    recentScans = await _scanService.GetRecentScansAsync(sessionId, limit);
                }
                
                return Json(new { 
                    success = true, 
                    data = recentScans,
                    sessionId = sessionId,
                    poId = poId,
                    isFiltered = poId.HasValue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ===== PRIVATE HELPER METHODS FOR PHASE 2 =====

        /// <summary>
        /// Gets recent scans specific to a PO for hierarchical lock context
        /// Shows all BOX items (scanned + pending) and actual PALLET/PCS scans
        /// </summary>
        private async Task<RecentScansResponseDto> GetPOSpecificRecentScansAsync(int sessionId, int poId, int limit)
        {
            var po = await _context.POMasters.FindAsync(poId);
            if (po == null)
            {
                return new RecentScansResponseDto
                {
                    RecentScans = new List<RecentScanDto>(),
                    SessionInfo = "PO not found"
                };
            }

            var enrichedScans = new List<RecentScanDto>();

            // 1. BOX items for this PO (all boxes, scanned + pending)
            var boxBarcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && 
                           b.POId == poId && 
                           b.BarcodeType == "BOX" && 
                           b.IsActive)
                .OrderBy(b => b.BoxNumber)
                .ToListAsync();

            foreach (var box in boxBarcodes)
            {
                enrichedScans.Add(new RecentScanDto
                {
                    BarcodeValue = box.BarcodeValue,
                    ItemType = "BOX",
                    PONumber = po.NoPO,
                    ModelProduct = box.ModelProduct ?? po.ModelProduk,
                    IsCompleted = box.Status == "SCANNED",
                    ScannedAt = box.ScannedDate ?? DateTime.MinValue,
                    ScannedBy = box.ScannedBy ?? "",
                    Status = box.Status,
                    SequenceNumber = box.BoxNumber ?? 0
                });
            }

            // 2. PALLET items for this PO (only scanned ones)
            var palletScans = await _context.ScanningActivities
                .Where(sa => sa.Action == "SCAN_PALLET" && 
                            sa.Result == "SUCCESS" &&
                            sa.POContext == $"PO_{poId}")
                .OrderByDescending(sa => sa.Timestamp)
                .ToListAsync();

            foreach (var pallet in palletScans)
            {
                enrichedScans.Add(new RecentScanDto
                {
                    BarcodeValue = pallet.BarcodeValue,
                    ItemType = "PALLET",
                    PONumber = po.NoPO,
                    ModelProduct = po.ModelProduk,
                    IsCompleted = true,
                    ScannedAt = pallet.Timestamp,
                    ScannedBy = pallet.UserId ?? "",
                    Status = "SCANNED"
                });
            }

            // 3. PCS items for this PO (only scanned ones)
            var pcsScans = await _context.ScanningActivities
                .Where(sa => sa.Action == "SCAN_PCS" && 
                            sa.Result == "SUCCESS" &&
                            sa.POContext == $"PO_{poId}")
                .OrderByDescending(sa => sa.Timestamp)
                .ToListAsync();

            foreach (var pcs in pcsScans)
            {
                enrichedScans.Add(new RecentScanDto
                {
                    BarcodeValue = pcs.BarcodeValue,
                    ItemType = "PCS",
                    PONumber = po.NoPO,
                    ModelProduct = po.ModelProduk,
                    IsCompleted = true,
                    ScannedAt = pcs.Timestamp,
                    ScannedBy = pcs.UserId ?? "",
                    Status = "SCANNED"
                });
            }

            // Calculate statistics
            var totalItems = po.QtyBox + po.QtyPallet + po.QtyPcs;
            var scannedItems = enrichedScans.Count(s => s.IsCompleted);

            return new RecentScansResponseDto
            {
                RecentScans = enrichedScans.Take(limit).ToList(),
                TotalCount = totalItems,
                ScannedCount = scannedItems,
                PendingCount = totalItems - scannedItems,
                SessionInfo = $"PO: {po.NoPO} ({po.ModelProduk})",
                Stats = new RecentStatsDto
                {
                    BoxTotal = po.QtyBox,
                    PalletTotal = po.QtyPallet,
                    PcsTotal = po.QtyPcs,
                    BoxScanned = boxBarcodes.Count(b => b.Status == "SCANNED"),
                    PalletScanned = palletScans.Count,
                    PcsScanned = pcsScans.Count
                }
            };
        }
    }
}
