using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using System.Security.Claims;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireInputer)]
    public class FinalController : Controller
    {
        private readonly IExcelProcessingService _excelService;
        private readonly IFinalProcessingService _finalService;

        public FinalController(IExcelProcessingService excelService, IFinalProcessingService finalService)
        {
            _excelService = excelService;
            _finalService = finalService;
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
    }
}
