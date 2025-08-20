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
    [Authorize(Policy = PolicyNames.RequireInputer)]
    public class FinalController : Controller
    {
        private readonly IExcelProcessingService _excelService;
        private readonly IFinalProcessingService _finalService;
        private readonly IBarcodeService _barcodeService;
        private readonly ISmartBarcodeManager _smartBarcodeManager;
        private readonly AppDbContext _context;

        public FinalController(
            IExcelProcessingService excelService, 
            IFinalProcessingService finalService, 
            IBarcodeService barcodeService,
            ISmartBarcodeManager smartBarcodeManager,
            AppDbContext context)
        {
            _excelService = excelService;
            _finalService = finalService;
            _barcodeService = barcodeService;
            _smartBarcodeManager = smartBarcodeManager;
            _context = context;
        }

        public async Task<IActionResult> Index(int id)
        {
            var finalData = await _finalService.GetFinalDataAsync(id);
            if (finalData == null)
            {
                TempData["Error"] = "Session not found or no data available.";
                return RedirectToAction("Index", "PO");
            }

            return View(finalData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRow([FromBody] UpdateRowCommand cmd)
        {
            try
            {
                if (cmd == null)
                    return Json(new { success = false, message = "Invalid payload" });

                var result = await _finalService.UpdateFinalRowAsync(cmd.SessionId, cmd.RowIndex, cmd.Request);
                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result.Value });
                }
                else
                {
                    return Json(new { success = false, message = result.Error });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating row: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMetadata([FromBody] SaveMetadataCommand cmd)
        {
            try
            {
                if (cmd == null)
                    return Json(new { success = false, message = "Invalid payload" });

                var userName = User.Identity?.Name ?? "Unknown";
                var success = await _finalService.SaveMetadataAsync(cmd.SessionId, cmd.Metadata, userName);
                
                if (success)
                {
                    TempData["Success"] = "Metadata saved successfully.";
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to save metadata." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error saving metadata: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateQR(int sessionId)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var qrData = await _finalService.GenerateQRIdentityAsync(sessionId, userName);
                
                if (qrData != null)
                {
                    TempData["Success"] = "QR Identity generated successfully.";
                    return RedirectToAction("Manage", "QR", new { sessionId });
                }
                else
                {
                    TempData["Error"] = "Failed to generate QR Identity.";
                    return RedirectToAction("Index", new { id = sessionId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating QR: {ex.Message}";
                return RedirectToAction("Index", new { id = sessionId });
            }
        }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddRow([FromBody] AddRowCommand cmd)
            {
                try
                {
                    if (cmd == null)
                        return Json(new { success = false, message = "Invalid payload" });

                    var userName = User.Identity?.Name ?? "Unknown";
                    var result = await _finalService.AddFinalRowAsync(cmd.SessionId, cmd.Request, userName);
                    if (result.IsSuccess)
                    {
                        return Json(new { success = true, data = result.Value });
                    }
                    else
                    {
                        return Json(new { success = false, message = result.Error });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Error adding row: {ex.Message}" });
                }
            }

        [HttpGet]
        public async Task<IActionResult> GetProgress(int sessionId)
        {
            try
            {
                var progressData = await _barcodeService.GetProgressByPOAsync(sessionId);
                return Json(new { success = true, data = progressData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProgressByPO(int sessionId, int poId)
        {
            try
            {
                var progressData = await _barcodeService.GetProgressByPOIdAsync(sessionId, poId);
                if (progressData != null)
                {
                    return Json(new { success = true, data = progressData });
                }
                else
                {
                    return Json(new { success = false, message = "PO not found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 🧠 SMART AUTO-CALCULATION ENGINE ENDPOINT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SmartUpdate([FromBody] SmartUpdateCommand cmd)
        {
            try 
            {
                if (cmd == null)
                    return Json(new { success = false, message = "Invalid payload" });

                // 🎯 SET OPERATOR ID FROM CURRENT USER
                cmd.Request.OperatorId = User.Identity?.Name ?? "Unknown";

                var result = await _finalService.SmartUpdateRowAsync(
                    cmd.SessionId, 
                    cmd.RowIndex, 
                    cmd.Request);
                    
                if (result.IsSuccess)
                {
                    return Json(new { 
                        success = true, 
                        data = result.UpdatedRow,
                        barcodeChanges = new {
                            added = result.BarcodeChanges?.NewBarcodes?.Count ?? 0,
                            removed = result.BarcodeChanges?.RemovedBarcodes?.Count ?? 0,
                            newBarcodes = result.BarcodeChanges?.NewBarcodes,
                            removedBarcodes = result.BarcodeChanges?.RemovedBarcodes
                        },
                        notifications = result.Notifications,
                        metadata = new {
                            calculationStrategy = result.Metadata?.CalculationStrategy,
                            boxCountChanged = result.Metadata?.BoxCountChanged ?? false,
                            updateTimestamp = DateTime.Now
                        }
                    });
                }
                
                return Json(new { success = false, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Smart Engine Error: {ex.Message}" });
            }
        }

        // 🔍 DEBUG ENDPOINT FOR BARCODE STATUS
        [HttpGet]
        public async Task<IActionResult> DebugBarcodes(int sessionId, string modelName)
        {
            try
            {
                // Get session info
                var session = await _context.UploadSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);
                
                if (session == null)
                    return Json(new { success = false, message = "Session not found" });

                if (string.IsNullOrEmpty(session.IdentityQRCode))
                    return Json(new { success = false, message = "QR Identity not generated yet" });

                var debugInfo = await _smartBarcodeManager.GetBarcodeStatusDebugInfo(session.IdentityQRCode, modelName);
                
                return Json(new { 
                    success = true, 
                    debugInfo = debugInfo,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Debug Error: {ex.Message}" });
            }
        }
    }
}
