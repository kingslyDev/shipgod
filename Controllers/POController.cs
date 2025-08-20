using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Data;
using System.Security.Claims;
using ShipmentFinishGood.Utilities;

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

        public IActionResult Index()
        {
            // For the initial page load, return empty data since we'll load via AJAX
            var emptyList = new List<POSessionSummaryDto>();
            return View(emptyList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaginatedSessions(
            int page = 1, 
            int pageSize = 12,
            string dateRange = "",
            string shipmentType = "",
            string country = "",
            string startDate = "",
            string endDate = "")
        {
            try
            {
                var query = _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Where(s => s.POMasters.Any(p => !string.IsNullOrEmpty(p.Country))) // Only sessions with country data
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(shipmentType))
                {
                    query = query.Where(s => s.ShipmentType == shipmentType);
                }

                // Apply date filters
                if (!string.IsNullOrEmpty(dateRange))
                {
                    var now = DateTime.Now;
                    switch (dateRange.ToLower())
                    {
                        case "today":
                            query = query.Where(s => s.UploadDate.Date == now.Date);
                            break;
                        case "week":
                            var weekStart = now.AddDays(-(int)now.DayOfWeek);
                            query = query.Where(s => s.UploadDate >= weekStart);
                            break;
                        case "month":
                            var monthStart = new DateTime(now.Year, now.Month, 1);
                            query = query.Where(s => s.UploadDate >= monthStart);
                            break;
                        case "custom":
                            if (DateTime.TryParse(startDate, out var start))
                                query = query.Where(s => s.UploadDate >= start);
                            if (DateTime.TryParse(endDate, out var end))
                                query = query.Where(s => s.UploadDate <= end.AddDays(1));
                            break;
                    }
                }

                // Country filter
                if (!string.IsNullOrEmpty(country))
                {
                    query = query.Where(s => s.Country == country || 
                                           (s.Countries != null && s.Countries.Contains(country)) ||
                                           s.POMasters.Any(p => p.Country == country));
                }

                // Get total count for pagination
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // Apply pagination and convert to DTOs
                var sessions = await query
                    .OrderByDescending(s => s.UploadDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var sessionDtos = new List<POSessionSummaryDto>();
                foreach (var session in sessions)
                {
                    // Additional check: only add sessions that have PO Masters with country data
                    if (!session.POMasters.Any(p => !string.IsNullOrEmpty(p.Country)))
                        continue;

                    var scanCounts = await GetScanCountsAsync(session.SessionId);
                    
                    // Get the first valid country from POMasters
                    var sessionCountry = session.POMasters
                        .Where(p => !string.IsNullOrEmpty(p.Country))
                        .Select(p => p.Country)
                        .FirstOrDefault();

                    // Skip if no country found
                    if (string.IsNullOrEmpty(sessionCountry))
                        continue;
                    
                    sessionDtos.Add(new POSessionSummaryDto
                    {
                        SessionId = session.SessionId,
                        FileName = session.FileName,
                        ShipmentType = session.ShipmentType,
                        ShipmentDate = session.ShipmentDate,
                        Status = session.Status,
                        QRIdentity = session.IdentityQRCode,
                        CreatedDate = session.UploadDate,
                        CreatedBy = session.UploadedBy,
                        TotalPOs = session.POMasters.Count,
                        TotalQty = session.POMasters.Sum(p => p.QtyTotal),
                        TotalBoxes = session.POMasters.Sum(p => p.QtyBox),
                        TotalPallets = session.POMasters.Sum(p => p.QtyPallet),
                        TotalItemsToScan = scanCounts.TotalItems,
                        ScannedItems = scanCounts.ScannedItems,
                        // Add country field to DTO
                        Country = sessionCountry
                    });
                }

                // Calculate summary
                var summary = new
                {
                    TotalSessions = totalItems,
                    TotalPOs = sessionDtos.Sum(s => s.TotalPOs),
                    TotalBoxes = sessionDtos.Sum(s => s.TotalBoxes),
                    CompletedSessions = sessionDtos.Count(s => s.ScanProgress >= 100)
                };

                return Json(new
                {
                    sessions = sessionDtos,
                    totalPages = totalPages,
                    currentPage = page,
                    totalItems = totalItems,
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private async Task<decimal> CalculateScanProgressAsync(int sessionId)
        {
            var totalItems = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId)
                .CountAsync();

            if (totalItems == 0) return 0;

            var scannedItems = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId && br.Status == "SCANNED")
                .CountAsync();

            return Math.Round((decimal)scannedItems / totalItems * 100, 1);
        }

        private async Task<(int TotalItems, int ScannedItems)> GetScanCountsAsync(int sessionId)
        {
            var totalItems = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId)
                .CountAsync();

            var scannedItems = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId && br.Status == "SCANNED")
                .CountAsync();

            return (totalItems, scannedItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _context.POMasters
                    .Where(p => !string.IsNullOrEmpty(p.Country))
                    .Select(p => p.Country)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Json(countries);
            }
            catch (Exception)
            {
                return Json(new List<string>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile excelFile)
        {
            // Input validation
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
                
                // Use new unified file processing method
                var uploadResult = await _excelService.ProcessFileUploadAsync(excelFile, userName);
                
                // Handle different scenarios based on upload result
                if (uploadResult.IsExistingFile)
                {
                    if (uploadResult.HasRemainingCountries)
                    {
                        // File exists with remaining countries - resume processing
                        TempData["Info"] = uploadResult.Message;
                        return RedirectToAction("Preview", new { id = uploadResult.SessionId });
                    }
                    else
                    {
                        // File completely processed - redirect to index
                        TempData["Warning"] = uploadResult.Message;
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    // New file processing
                    if (uploadResult.PreviewData != null)
                    {
                        TempData["Success"] = uploadResult.Message;
                        return View("Preview", uploadResult.PreviewData);
                    }
                    else
                    {
                        // Error in processing
                        TempData["Error"] = uploadResult.Message;
                        return RedirectToAction("Create");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unexpected error: {ex.Message}";
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
            .Where(r => CountryNormalizer.Normalize(r.Country) == CountryNormalizer.Normalize(countryData.Key))
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
                var normalizedReqCountry = ShipmentFinishGood.Utilities.CountryNormalizer.Normalize(request.Country);
                var countryDetails = session.Details
                    .Where(d => ShipmentFinishGood.Utilities.CountryNormalizer.Normalize(d.Country) == normalizedReqCountry)
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
                // Precompute normalized country (cannot use local functions inside EF queries)
                string normalizedCountry = CountryNormalizer.Normalize(request.Country);
                // Validate country not already submitted
                var isAlreadySubmitted = await _context.UploadSessions
                    .AnyAsync(s => s.ParentSessionId == request.SessionId && 
                                  s.Country == normalizedCountry && 
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
                        .Where(s => s.ParentSessionId == request.SessionId && s.Country == normalizedCountry)
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

            var allCountries = parentSession.Countries
                .Split(',')
                .Select(c => ShipmentFinishGood.Utilities.CountryNormalizer.Normalize(c))
                .ToList();
            var submittedCountries = parentSession.ChildSessions
                .Where(c => c.Status == "PROCESSED")
                .Select(c => ShipmentFinishGood.Utilities.CountryNormalizer.Normalize(c.Country))
                .ToList();

            return allCountries.Where(c => !submittedCountries.Contains(c)).ToList();
        }

        /// <summary>
        /// Delete session and all related data (clean delete)
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Find the session with all related data
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .Include(s => s.ChildSessions)
                        .ThenInclude(cs => cs.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == id);

                if (session == null)
                {
                    return Json(new { success = false, message = "Session not found" });
                }

                // Delete all child sessions and their related data
                foreach (var childSession in session.ChildSessions.ToList())
                {
                    await DeleteSessionDataAsync(childSession.SessionId);
                }

                // Delete main session data
                await DeleteSessionDataAsync(session.SessionId);

                // Remove the main session
                _context.UploadSessions.Remove(session);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Session deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting session: {ex.Message}" });
            }
        }

        /// <summary>
        /// Helper method to delete all data related to a specific session
        /// </summary>
        private async Task DeleteSessionDataAsync(int sessionId)
        {
            // Delete scanning activities related to barcodes from this session
            var barcodeValues = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId)
                .Select(br => br.BarcodeValue)
                .ToListAsync();

            if (barcodeValues.Any())
            {
                var scanningActivities = await _context.ScanningActivities
                    .Where(sa => barcodeValues.Contains(sa.BarcodeValue))
                    .ToListAsync();

                _context.ScanningActivities.RemoveRange(scanningActivities);
            }

            // Delete barcode registries
            var barcodeRegistries = await _context.BarcodeRegistries
                .Where(br => br.SessionId == sessionId)
                .ToListAsync();

            _context.BarcodeRegistries.RemoveRange(barcodeRegistries);

            // Delete PO Masters
            var poMasters = await _context.POMasters
                .Where(pm => pm.SourceSessionId == sessionId)
                .ToListAsync();

            _context.POMasters.RemoveRange(poMasters);

            // Delete session details if exists
            var sessionDetails = await _context.UploadSessionDetails
                .Where(usd => usd.SessionId == sessionId)
                .ToListAsync();

            if (sessionDetails.Any())
            {
                _context.UploadSessionDetails.RemoveRange(sessionDetails);
            }
        }
    }
}
