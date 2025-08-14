using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Data;
using System.Security.Claims;

namespace ShipmentFinishGood.Controllers
{
    [Authorize(Policy = PolicyNames.RequireInputer)]
    public class POController : Controller
    {
        private readonly IExcelProcessingService _excelService;
        private readonly AppDbContext _context;

        public POController(IExcelProcessingService excelService, AppDbContext context)
        {
            _excelService = excelService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var poSessions = await _excelService.GetAllPOSessionsAsync();
            return View(poSessions);
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
        public async Task<IActionResult> UpdateMode([FromBody] UpdateModeRequest request)
        {
            var preview = await _excelService.GetPreviewAsync(request.SessionId);
            if (preview == null)
            {
                return Json(new { success = false, message = "Session not found" });
            }

            // Recalculate all data with new shipment type
            var processedDataByCountry = new Dictionary<string, List<ProcessedPOData>>();
            
            foreach (var countryData in preview.ProcessedDataByCountry)
            {
                var rawDataForCountry = preview.RawData
                    .Where(r => r.Country == countryData.Key)
                    .ToList();
                    
                var recalculatedData = await _excelService.CalculateProcessedDataAsync(rawDataForCountry, request.ShipmentType);
                processedDataByCountry[countryData.Key] = recalculatedData;
            }
            
            // Return all recalculated data grouped by country
            return Json(new { 
                success = true, 
                data = processedDataByCountry.SelectMany(kvp => 
                    kvp.Value.Select(p => new {
                        country = kvp.Key,
                        noPO = p.NoPO,
                        model = p.Model,
                        totalQty = p.TotalQty,
                        qtyPallet = p.QtyPallet,
                        qtyBox = p.QtyBox,
                        qtyPcs = p.QtyPcs,
                        container = p.Container,
                        noInvoice = p.NoInvoice,
                        shipmentDetail = p.ShipmentDetail
                    })
                ).ToList(),
                dataByCountry = processedDataByCountry.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(p => new {
                        noPO = p.NoPO,
                        model = p.Model,
                        totalQty = p.TotalQty,
                        qtyPallet = p.QtyPallet,
                        qtyBox = p.QtyBox,
                        qtyPcs = p.QtyPcs,
                        container = p.Container,
                        noInvoice = p.NoInvoice,
                        shipmentDetail = p.ShipmentDetail
                    }).ToList()
                )
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
                    return RedirectToAction("Index", "Final", new { sessionId = request.SessionId });
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

        [HttpPost]
        public async Task<IActionResult> UpdateRowData([FromBody] UpdateRowDataRequest request)
        {
            try
            {
                // Get the session data
                var session = await _context.UploadSessions
                    .Include(s => s.Details)
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session == null)
                {
                    return Json(new { success = false, message = "Session not found" });
                }

                // Get all details for this country and find the specific row
                var countryDetails = session.Details
                    .Where(d => d.Country == request.Country)
                    .OrderBy(d => d.RowIndex)
                    .ToList();

                if (request.RowIndex >= countryDetails.Count)
                {
                    return Json(new { success = false, message = "Row index out of range" });
                }

                var rowData = countryDetails[request.RowIndex];

                // Update the raw data (what we can)
                rowData.OriginalPO = request.RowData.NoPO;
                rowData.OriginalQty = request.RowData.TotalQty;

                // Recalculate quantities for this single row
                var recalculatedData = await _excelService.CalculateSingleRowAsync(
                    rowData.Model ?? "", 
                    request.RowData.TotalQty, 
                    request.ShipmentType
                );

                // Save changes to session detail
                await _context.SaveChangesAsync();

                // Return updated row data with calculated values
                return Json(new { 
                    success = true, 
                    message = "Row data updated successfully",
                    updatedRowData = new {
                        noPO = request.RowData.NoPO,
                        model = rowData.Model,
                        totalQty = request.RowData.TotalQty,
                        qtyPallet = recalculatedData.QtyPallet,
                        qtyBox = recalculatedData.QtyBox,
                        qtyPcs = recalculatedData.QtyPcs,
                        noInvoice = request.RowData.NoInvoice,
                        container = request.RowData.Container,
                        shipmentDetail = request.RowData.ShipmentDetail
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitCountry([FromBody] CountrySubmissionRequest request)
        {
            try
            {
                // Validate country not already submitted
                var isAlreadySubmitted = await _context.UploadSessions
                    .AnyAsync(s => s.ParentSessionId == request.SessionId && 
                                  s.Country == request.Country && 
                                  s.Status == "PROCESSED");

                if (isAlreadySubmitted)
                {
                    return Json(new CountrySubmissionResult
                    {
                        Success = false,
                        Message = $"Country {request.Country} sudah di-submit sebelumnya!"
                    });
                }

                // Submit country data
                var success = await _excelService.SubmitCountryDataAsync(request, User.Identity?.Name ?? "System");
                
                if (success)
                {
                    // Get the new session for QR generation
                    var newSession = await _context.UploadSessions
                        .Where(s => s.ParentSessionId == request.SessionId && s.Country == request.Country)
                        .OrderByDescending(s => s.SessionId)
                        .FirstAsync();

                    // Generate QR for this country session
                    var qrIdentity = await GenerateQRIdentityAsync(newSession.SessionId);
                    
                    // Get remaining countries
                    var remainingCountries = await GetRemainingCountries(request.SessionId);
                    
                    return Json(new CountrySubmissionResult
                    {
                        Success = true,
                        Country = request.Country,
                        NewSessionId = newSession.SessionId,
                        QRIdentity = qrIdentity,
                        Message = $"Country {request.Country} berhasil di-submit!",
                        RemainingCountries = remainingCountries,
                        AllCountriesSubmitted = remainingCountries.Count == 0
                    });
                }

                return Json(new CountrySubmissionResult
                {
                    Success = false,
                    Message = "Gagal memproses data country"
                });
            }
            catch (Exception ex)
            {
                return Json(new CountrySubmissionResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        private async Task<string> GenerateQRIdentityAsync(int sessionId)
        {
            string qrIdentity;
            do {
                qrIdentity = $"QR{DateTime.Now:yyyyMMddHHmmss}{DateTime.Now.Millisecond:000}";
                await Task.Delay(1); // Ensure different milliseconds
            } while (await _context.UploadSessions.AnyAsync(s => s.IdentityQRCode == qrIdentity));

            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IdentityQRCode = qrIdentity;
                await _context.SaveChangesAsync();
            }

            return qrIdentity;
        }

        private async Task<List<string>> GetRemainingCountries(int sessionId)
        {
            var parentSession = await _context.UploadSessions
                .Include(s => s.ChildSessions)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (parentSession == null || string.IsNullOrEmpty(parentSession.Countries))
                return new List<string>();

            var allCountries = parentSession.Countries.Split(',').ToList();
            var submittedCountries = parentSession.ChildSessions
                .Where(c => c.Status == "PROCESSED")
                .Select(c => c.Country)
                .ToList();

            return allCountries.Where(c => !submittedCountries.Contains(c)).ToList();
        }
    }
}
