using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ShipmentFinishGood.Controllers
{
    public class ShippingController : Controller
    {
        private readonly IExcelProcessingService _excelService;
        private readonly IScanningService _scanService;
        private readonly IBarcodeService _barcodeService;
        private readonly AppDbContext _context;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(
            IExcelProcessingService excelService,
            IScanningService scanService,
            IBarcodeService barcodeService,
            AppDbContext context,
            ILogger<ShippingController> logger)
        {
            _excelService = excelService;
            _scanService = scanService;
            _barcodeService = barcodeService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Executive Dashboard - Focus on barang keluar with real metrics
        /// </summary>
        public async Task<IActionResult> Dashboard(string? selectedModel = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Get comprehensive executive dashboard data
                var dashboardData = await GetOutboundTrackingDataAsync(null, startDate, endDate, null);
                
                // Add available models for filter
                var availableModels = await _context.BarcodeRegistries
                    .Where(b => !string.IsNullOrEmpty(b.ModelProduct))
                    .Select(b => b.ModelProduct!)
                    .Distinct()
                    .ToListAsync();
                
                dashboardData.AvailableModels = availableModels;
                
                // Filter by model if specified
                if (!string.IsNullOrEmpty(selectedModel))
                {
                    // Filter country model stats by selected model
                    dashboardData.CountryModelStats = dashboardData.CountryModelStats
                        .Where(s => s.Model == selectedModel)
                        .ToList();
                }
                
                // Set filter values for the view
                ViewBag.SelectedModel = selectedModel ?? "";
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
                
                return View("DashboardSimple", dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                // Return empty dashboard in case of error
                return View("DashboardSimple", new ShippingDashboardDto());
            }
        }

        /// <summary>
        /// API endpoint for chart data - real-time updates
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetChartData(string chartType, string? country = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var dashboardData = await GetOutboundTrackingDataAsync(country, fromDate, toDate, null);
                
                switch (chartType.ToLower())
                {
                    case "status":
                        return Json(new
                        {
                            labels = new[] { "Generated", "Scanned", "Shipped", "Pending" },
                            data = new[] {
                                dashboardData.TotalItemsGenerated,
                                dashboardData.TotalItemsScanned,
                                dashboardData.TotalItemsShipped,
                                dashboardData.TotalItemsPending
                            },
                            backgroundColor = new[] { "#28a745", "#17a2b8", "#ffc107", "#dc3545" }
                        });
                        
                    case "country":
                        return Json(new
                        {
                            labels = dashboardData.CountryDistribution.Keys.ToArray(),
                            data = dashboardData.CountryDistribution.Values.ToArray(),
                            backgroundColor = GenerateColors(dashboardData.CountryDistribution.Count)
                        });
                        
                    case "hourly":
                        return Json(new
                        {
                            labels = dashboardData.HourlyTrends.Keys.ToArray(),
                            data = dashboardData.HourlyTrends.Values.ToArray()
                        });
                        
                    case "models":
                        return Json(new
                        {
                            labels = dashboardData.TopModels.Keys.Take(10).ToArray(),
                            data = dashboardData.TopModels.Values.Take(10).ToArray(),
                            backgroundColor = GenerateColors(10)
                        });
                        
                    default:
                        return Json(new { error = "Unknown chart type" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get comprehensive executive dashboard data with real-time metrics
        /// </summary>
        private async Task<ShippingDashboardDto> GetOutboundTrackingDataAsync(string? country, DateTime? fromDate, DateTime? toDate, string? status)
        {
            // Get all sessions and their data
            var allSessions = await _excelService.GetAllPOSessionsAsync();
            var activeSessions = await _scanService.GetActiveSessionsAsync();
            
            var outboundItems = new List<OutboundItemDto>();
            var countries = new HashSet<string>();
            var sessionMetrics = new List<SessionMetric>();
            
            foreach (var session in allSessions)
            {
                try
                {
                    // Get session barcode data for real tracking
                    var sessionBarcodes = await _barcodeService.GetBarcodeRegistriesForSessionAsync(session.SessionId);
                    var sessionProgress = activeSessions.FirstOrDefault(a => a.SessionId == session.SessionId);
                    
                    var sessionBoxes = sessionBarcodes.Where(b => b.BarcodeType == "BOX").ToList();
                    var scannedBoxes = sessionBoxes.Count(b => b.Status == "SCANNED");
                    
                    // Calculate session metrics
                    sessionMetrics.Add(new SessionMetric
                    {
                        SessionId = session.SessionId,
                        FileName = session.FileName,
                        Status = session.Status,
                        TotalItems = sessionBoxes.Count,
                        ScannedItems = scannedBoxes,
                        CompletionRate = sessionBoxes.Count > 0 ? (double)scannedBoxes / sessionBoxes.Count * 100 : 0,
                        CreatedDate = session.CreatedDate,
                        Country = session.POs.FirstOrDefault()?.Country ?? "Unknown"
                    });
                    
                    foreach (var poMaster in session.POs)
                    {
                        // Extract destination country
                        var destinationCountry = ExtractDestinationCountry(poMaster);
                        if (!string.IsNullOrEmpty(destinationCountry))
                        {
                            countries.Add(destinationCountry);
                        }
                        
                        // Get barcodes for this session (BOX type for outbound tracking)
                        var relevantBarcodes = sessionBoxes.Where(b => b.SessionId == session.SessionId).ToList();
                        
                        foreach (var barcode in relevantBarcodes)
                        {
                            var outboundItem = new OutboundItemDto
                            {
                                BarcodeValue = barcode.BarcodeValue,
                                PONumber = poMaster.NoPO,
                                LineNumber = barcode.BoxNumber?.ToString() ?? "0",
                                ModelProduct = poMaster.ModelProduk,
                                Quantity = 1, // Each barcode represents 1 box
                                Country = destinationCountry,
                                GeneratedDate = barcode.GeneratedDate,
                                ScannedDate = barcode.ScannedDate,
                                Status = DetermineOutboundStatus(barcode),
                                Area = DetermineAreaFromStatus(barcode),
                                ScannedBy = barcode.ScannedBy ?? "",
                                ShipDate = barcode.Status == "SCANNED" ? barcode.ScannedDate?.ToString("yyyy-MM-dd") : null
                            };
                            
                            // Apply filters
                            if (!PassesFilters(outboundItem, country, fromDate, toDate, status))
                                continue;
                            
                            outboundItems.Add(outboundItem);
                        }
                    }
                }
                catch
                {
                    // Skip problematic sessions
                    continue;
                }
            }
            
            // Calculate executive metrics
            var totalGenerated = outboundItems.Count;
            var totalScanned = outboundItems.Count(x => x.ScannedDate.HasValue);
            var totalShipped = outboundItems.Count(x => x.Status == "Scanned"); // Scanned = ready for shipping
            var todayItems = outboundItems.Where(x => x.GeneratedDate.Date == DateTime.Today).ToList();
            
            return new ShippingDashboardDto
            {
                // Main KPIs
                TotalItemsGenerated = totalGenerated,
                TotalItemsScanned = totalScanned,
                TotalItemsShipped = totalShipped,
                TotalItemsPending = totalGenerated - totalScanned,
                
                // Legacy compatibility
                TotalOrders = totalGenerated,
                CompletedShipments = totalShipped,
                PendingShipments = totalGenerated - totalScanned,
                ProcessingShipments = totalScanned,
                
                // Today's performance
                TodayGenerated = todayItems.Count,
                TodayScanned = todayItems.Count(x => x.ScannedDate.HasValue),
                TodayCompletionRate = todayItems.Count > 0 ? (double)todayItems.Count(x => x.ScannedDate.HasValue) / todayItems.Count * 100 : 0,
                
                // Efficiency metrics
                OverallEfficiency = totalGenerated > 0 ? (double)totalScanned / totalGenerated * 100 : 0,
                ActiveSessions = activeSessions.Count,
                CompletedSessions = sessionMetrics.Count(s => s.CompletionRate >= 100),
                
                // Business intelligence
                CountryDistribution = outboundItems
                    .Where(x => !string.IsNullOrEmpty(x.Country))
                    .GroupBy(x => x.Country)
                    .ToDictionary(g => g.Key, g => g.Count()),
                    
                HourlyTrends = GenerateHourlyTrends(outboundItems),
                TopModels = outboundItems
                    .GroupBy(x => x.ModelProduct)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                
                // Session details for drill-down
                SessionMetrics = sessionMetrics.OrderByDescending(s => s.CreatedDate).ToList(),
                RecentItems = outboundItems.OrderByDescending(x => x.GeneratedDate).Take(50).ToList(),
                OutboundItems = outboundItems.OrderByDescending(x => x.GeneratedDate).ToList(),
                
                // Legacy recent shipments
                RecentShipments = outboundItems
                    .Take(10)
                    .Select(item => new ShipmentSummaryDto
                    {
                        SessionId = item.BarcodeValue,
                        FileName = item.PONumber,
                        Status = item.Status,
                        TotalBoxes = 1,
                        ShipmentDate = item.ScannedDate,
                        CreatedDate = item.GeneratedDate
                    }).ToList(),
                
                // Filter options
                AvailableCountries = countries.OrderBy(c => c).ToList(),
                
                // Country model statistics
                CountryModelStats = outboundItems
                    .Where(x => !string.IsNullOrEmpty(x.Country) && !string.IsNullOrEmpty(x.ModelProduct))
                    .GroupBy(x => new { x.Country, x.ModelProduct })
                    .Select(g => new CountryModelStat
                    {
                        Country = g.Key.Country,
                        Model = g.Key.ModelProduct,
                        TotalShipped = g.Count(x => x.Status == "Scanned"),
                        CompletionRate = g.Count() > 0 ? (double)g.Count(x => x.ScannedDate.HasValue) / g.Count() * 100 : 0,
                        AvgLeadTime = g.Where(x => x.ScannedDate.HasValue)
                                      .Select(x => (x.ScannedDate!.Value - x.GeneratedDate).TotalHours)
                                      .DefaultIfEmpty(0)
                                      .Average()
                    })
                    .OrderBy(s => s.Country)
                    .ThenBy(s => s.Model)
                    .ToList(),
                    
                // Scanner metrics
                ActiveScanners = activeSessions.Count,
                TotalScanners = 15, // Total available scanners (could be from config)
                AverageLeadTime = outboundItems
                    .Where(x => x.ScannedDate.HasValue)
                    .Select(x => (x.ScannedDate!.Value - x.GeneratedDate).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average()
            };
        }
        
        private string ExtractDestinationCountry(POMasterDto poMaster)
        {
            // Try Country field first
            if (!string.IsNullOrEmpty(poMaster.Country))
                return poMaster.Country;
                
            // Extract from shipment details
            if (!string.IsNullOrEmpty(poMaster.ShipmentDetail))
            {
                var detail = poMaster.ShipmentDetail.ToLower();
                
                // Country detection based on shipment patterns
                var countryMappings = new Dictionary<string, string>
                {
                    { "singapore", "Singapore" }, { "sg", "Singapore" },
                    { "malaysia", "Malaysia" }, { "my", "Malaysia" },
                    { "indonesia", "Indonesia" }, { "id", "Indonesia" },
                    { "thailand", "Thailand" }, { "th", "Thailand" },
                    { "vietnam", "Vietnam" }, { "vn", "Vietnam" },
                    { "philippines", "Philippines" }, { "ph", "Philippines" },
                    { "japan", "Japan" }, { "jp", "Japan" },
                    { "korea", "South Korea" }, { "kr", "South Korea" },
                    { "china", "China" }, { "cn", "China" },
                    { "taiwan", "Taiwan" }, { "tw", "Taiwan" }
                };
                
                foreach (var mapping in countryMappings)
                {
                    if (detail.Contains(mapping.Key))
                        return mapping.Value;
                }
            }
            
            return "International";
        }
        
        private string DetermineOutboundStatus(BarcodeRegistry barcode)
        {
            // Simplified outbound status based on barcode registry
            return barcode.Status switch
            {
                "GENERATED" => "Generated",
                "SCANNED" => "Scanned", 
                "CANCELLED" => "Cancelled",
                _ => "Unknown"
            };
        }
        
        private string DetermineAreaFromStatus(BarcodeRegistry barcode)
        {
            // Determine shipping area based on scan status and timing
            if (barcode.Status == "SCANNED" && barcode.ScannedDate.HasValue)
            {
                // Simple area assignment based on time or could be enhanced with actual area logic
                var hour = barcode.ScannedDate.Value.Hour;
                return hour < 12 ? "Area A" : hour < 18 ? "Area B" : "Area C";
            }
            return "";
        }
        
        private Dictionary<string, int> GenerateHourlyTrends(List<OutboundItemDto> items)
        {
            var trends = new Dictionary<string, int>();
            var today = DateTime.Today;
            
            for (int hour = 0; hour < 24; hour++)
            {
                var hourStart = today.AddHours(hour);
                var hourEnd = hourStart.AddHours(1);
                
                var count = items.Count(x => x.ScannedDate.HasValue && 
                                           x.ScannedDate.Value >= hourStart && 
                                           x.ScannedDate.Value < hourEnd);
                                           
                trends[$"{hour:D2}:00"] = count;
            }
            
            return trends;
        }
        
        private bool PassesFilters(OutboundItemDto item, string? country, DateTime? fromDate, DateTime? toDate, string? status)
        {
            if (!string.IsNullOrEmpty(country) && !item.Country.Equals(country, StringComparison.OrdinalIgnoreCase))
                return false;
                
            if (fromDate.HasValue && item.GeneratedDate.Date < fromDate.Value.Date)
                return false;
                
            if (toDate.HasValue && item.GeneratedDate.Date > toDate.Value.Date)
                return false;
                
            if (!string.IsNullOrEmpty(status) && !item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                return false;
            
            return true;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType)
        {
            try
            {
                switch (chartType?.ToLower())
                {
                    case "country":
                        return Json(await GetCountryChartData());
                    case "status":
                        return Json(await GetStatusChartData());
                    case "hourly":
                        return Json(await GetHourlyChartData());
                    case "models":
                        return Json(await GetModelsChartData());
                    default:
                        return BadRequest("Invalid chart type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data for type: {ChartType}", chartType);
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<object> GetCountryChartData()
        {
            // Join dengan POMaster untuk mendapatkan Country
            var countryData = await _context.BarcodeRegistries
                .Where(b => b.GeneratedDate.Date == DateTime.Today)
                .Join(_context.POMasters, b => b.POId, p => p.POId, (b, p) => new { b, p })
                .GroupBy(x => x.p.Country ?? "Unknown")
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var colors = new[] { "#007bff", "#28a745", "#ffc107", "#dc3545", "#6c757d" };
            
            return new
            {
                labels = countryData.Select(x => x.Country).ToArray(),
                data = countryData.Select(x => x.Count).ToArray(),
                backgroundColor = colors.Take(countryData.Count).ToArray()
            };
        }

        private async Task<object> GetStatusChartData()
        {
            var statusData = await _context.BarcodeRegistries
                .Where(b => b.GeneratedDate.Date == DateTime.Today)
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var colors = new Dictionary<string, string>
            {
                { "GENERATED", "#ffc107" },
                { "SCANNED", "#28a745" },
                { "CANCELLED", "#dc3545" }
            };

            return new
            {
                labels = statusData.Select(x => x.Status).ToArray(),
                data = statusData.Select(x => x.Count).ToArray(),
                backgroundColor = statusData.Select(x => colors.GetValueOrDefault(x.Status, "#6c757d")).ToArray()
            };
        }

        private async Task<object> GetHourlyChartData()
        {
            var hourlyData = await _context.ScanningActivities
                .Where(s => s.Timestamp.Date == DateTime.Today)
                .GroupBy(s => s.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            // Fill in missing hours with 0
            var allHours = Enumerable.Range(0, 24)
                .Select(h => new { 
                    Hour = h, 
                    Count = hourlyData.FirstOrDefault(x => x.Hour == h)?.Count ?? 0 
                })
                .ToList();

            return new
            {
                labels = allHours.Select(x => $"{x.Hour:00}:00").ToArray(),
                data = allHours.Select(x => x.Count).ToArray()
            };
        }

        private async Task<object> GetModelsChartData()
        {
            var modelData = await _context.BarcodeRegistries
                .Where(b => b.GeneratedDate.Date == DateTime.Today && b.Status == "SCANNED")
                .GroupBy(b => b.ModelProduct)
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var colors = new[] { "#007bff", "#28a745", "#ffc107", "#dc3545", "#6c757d", 
                               "#e83e8c", "#fd7e14", "#20c997", "#6f42c1", "#17a2b8" };

            return new
            {
                labels = modelData.Select(x => x.Model ?? "Unknown").ToArray(),
                data = modelData.Select(x => x.Count).ToArray(),
                backgroundColor = colors.Take(modelData.Count).ToArray()
            };
        }
        
        private string[] GenerateColors(int count)
        {
            var colors = new[]
            {
                "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF",
                "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF6384"
            };
            
            return colors.Take(Math.Min(count, colors.Length)).ToArray();
        }
    }
}
