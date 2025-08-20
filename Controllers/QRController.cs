using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireInputer)]
    public class QRController : Controller
    {
        private readonly IQRManagementService _qrService;
        private readonly IFinalProcessingService _finalService;

        public QRController(IQRManagementService qrService, IFinalProcessingService finalService)
        {
            _qrService = qrService;
            _finalService = finalService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQRIdentity(int sessionId)
        {
            try
            {
                var qrData = await _qrService.GetQRDataAsync(sessionId);
                return Json(new { qrIdentity = qrData?.QRIdentity ?? "Not Generated" });
            }
            catch (Exception)
            {
                return Json(new { qrIdentity = "Error Loading" });
            }
        }

        public async Task<IActionResult> Manage(int id)
        {
            var qrData = await _qrService.GetQRDataAsync(id);
            if (qrData == null)
            {
                TempData["Error"] = "QR data not found. Please generate QR first.";
                return RedirectToAction("Index", "Final", new { id });
            }

            return View(qrData);
        }

        public async Task<IActionResult> DownloadPDF(int sessionId)
        {
            try
            {
                var pdfData = await _qrService.GenerateBarcodesPDFAsync(sessionId);
                if (pdfData != null)
                {
                    var fileName = $"Barcodes_Session_{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    return File(pdfData, "application/pdf", fileName);
                }
                else
                {
                    TempData["Error"] = "Failed to generate PDF barcodes.";
                    return RedirectToAction("Manage", new { id = sessionId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction("Manage", new { id = sessionId });
            }
        }

        public async Task<IActionResult> PrintQR(int sessionId)
        {
            var qrData = await _qrService.GetQRDataAsync(sessionId);
            if (qrData == null)
            {
                TempData["Error"] = "QR data not found.";
                return RedirectToAction("Manage", new { id = sessionId });
            }

            return View("PrintQR", qrData);
        }

        [HttpGet]
        public async Task<IActionResult> GetQRImage(int sessionId)
        {
            try
            {
                var qrImageData = await _qrService.GetQRImageAsync(sessionId);
                if (qrImageData != null)
                {
                    return File(qrImageData, "image/png");
                }
                else
                {
                    return NotFound("QR image not found.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating QR image: {ex.Message}");
            }
        }
    }
}
