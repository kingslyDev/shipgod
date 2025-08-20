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

        [HttpGet]
        public async Task<IActionResult> GetScanningStats(int sessionId)
        {
            try
            {
                var qrData = await _qrService.GetQRDataAsync(sessionId);
                if (qrData == null)
                    return Json(new { success = false, message = "QR data not found" });

                return Json(new
                {
                    success = true,
                    totalScanned = qrData.TotalScanned,
                    scannedBoxes = qrData.ScannedBoxes,
                    scannedPallets = qrData.ScannedPallets,
                    scannedPcs = qrData.ScannedPcs,
                    totalBoxes = qrData.TotalBoxes,
                    totalPallets = qrData.TotalPallets,
                    totalPcs = qrData.TotalPcs,
                    totalItems = qrData.TotalBoxes + qrData.TotalPallets + qrData.TotalPcs,
                    scanPercentage = qrData.ScanPercentage,
                    canComplete = qrData.CanComplete,
                    lastScannedDate = qrData.LastScannedDate?.ToString("dd/MM/yyyy HH:mm:ss"),
                    lastScannedBy = qrData.LastScannedBy,
                    poSummaries = qrData.POSummaries.Select(po => new
                    {
                        poNumber = po.PONumber,
                        scannedCount = po.ScannedCount,
                        qtyBox = po.QtyBox,
                        scannedPercentage = po.ScannedPercentage
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
