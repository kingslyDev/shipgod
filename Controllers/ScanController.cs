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
                    return Json(new { 
                        success = true, 
                        message = "Box scanned successfully!", 
                        data = result.Value 
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
                            sessionExists = currentSession != null
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error scanning box: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignArea(int sessionId, string area)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _scanService.AssignAreaAsync(sessionId, area, userName);
                
                if (result.IsSuccess)
                {
                    TempData["Success"] = $"Session assigned to Area {area} successfully!";
                    return Json(new { success = true, message = $"Assigned to Area {area}" });
                }
                else
                {
                    return Json(new { success = false, message = result.Error });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error assigning area: {ex.Message}" });
            }
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
                    TempData["Success"] = "Scanning completed successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = result.Error;
                    return RedirectToAction("StartScan", new { sessionId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error completing scan: {ex.Message}";
                return RedirectToAction("StartScan", new { sessionId });
            }
        }
    }
}
