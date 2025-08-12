using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using System.Security.Claims;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireInputer)]
    public class POController : Controller
    {
        private readonly IExcelProcessingService _excelService;

        public POController(IExcelProcessingService excelService)
        {
            _excelService = excelService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("Create");
            }

            if (!excelFile.FileName.EndsWith(".xlsx") && !excelFile.FileName.EndsWith(".xls"))
            {
                TempData["Error"] = "Please upload a valid Excel file (.xlsx or .xls).";
                return RedirectToAction("Create");
            }

            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var preview = await _excelService.ProcessExcelFileAsync(excelFile, userName);
                
                TempData["Success"] = "Excel file processed successfully. Please review the data below.";
                return View("Preview", preview);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing file: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        public async Task<IActionResult> Preview(int id)
        {
            var preview = await _excelService.GetPreviewAsync(id);
            if (preview == null)
            {
                TempData["Error"] = "Preview not found.";
                return RedirectToAction("Create");
            }

            return View(preview);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMode(int sessionId, string shipmentType)
        {
            var preview = await _excelService.GetPreviewAsync(sessionId);
            if (preview == null)
            {
                return Json(new { success = false, message = "Session not found" });
            }

            var processedData = await _excelService.CalculateProcessedDataAsync(preview.RawData, shipmentType);
            
            return Json(new { 
                success = true, 
                data = processedData.Select(p => new {
                    noPO = p.NoPO,
                    model = p.Model,
                    totalQty = p.TotalQty,
                    qtyPallet = p.QtyPallet,
                    qtyBox = p.QtyBox,
                    qtyPcs = p.QtyPcs
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> Submit(CalculationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var preview = await _excelService.GetPreviewAsync(request.SessionId);
                return View("Preview", preview);
            }

            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var success = await _excelService.SubmitProcessedDataAsync(request, userName);
                
                if (success)
                {
                    TempData["Success"] = "PO data has been successfully processed and saved.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Failed to process PO data.";
                    var preview = await _excelService.GetPreviewAsync(request.SessionId);
                    return View("Preview", preview);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error submitting data: {ex.Message}";
                var preview = await _excelService.GetPreviewAsync(request.SessionId);
                return View("Preview", preview);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePO(int poId, ProcessedPOData updatedData)
        {
            var success = await _excelService.UpdatePODataAsync(poId, updatedData);
            
            if (success)
            {
                return Json(new { success = true, message = "PO updated successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update PO" });
            }
        }
    }
}
