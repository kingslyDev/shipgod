using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using System.Security.Claims;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireScanner)]
    public class ScanController : Controller
    {
        private readonly IScanningService _scanService;
        private readonly IExcelProcessingService _excelService;

        public ScanController(IScanningService scanService, IExcelProcessingService excelService)
        {
            _scanService = scanService;
            _excelService = excelService;
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
        public async Task<IActionResult> ScanItem(int sessionId, string barcode)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                
                // Validate input
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    return Json(new { success = false, message = "Barcode cannot be empty" });
                }
                
                // Enhanced debug logging
                Console.WriteLine($"=== SCAN ITEM DEBUG ===");
                Console.WriteLine($"SessionId: {sessionId}");
                Console.WriteLine($"Barcode: {barcode}");
                Console.WriteLine($"User: {userName}");
                Console.WriteLine($"Barcode Length: {barcode.Length}");
                Console.WriteLine($"Contains PALLET: {barcode.ToUpperInvariant().Contains("PALLET")}");
                Console.WriteLine($"Contains PCS: {barcode.ToUpperInvariant().Contains("PCS")}");
                Console.WriteLine($"Contains BOX: {barcode.ToUpperInvariant().Contains("BOX")}");
                
                var result = await _scanService.ScanItemBarcodeAsync(sessionId, barcode, userName);
                
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
            
            if (upperBarcode.Contains("PCS"))
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
    }
}
