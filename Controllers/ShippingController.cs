using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Services;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.ViewModels;
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

        // =============================================
        // Simple Dashboard (requested minimal executive view)
        // URL: /Shipping/SimpleDashboard
        // =============================================
        [HttpGet]
        public async Task<IActionResult> SimpleDashboard(string? country = null, string? model = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var vm = await BuildSimpleDashboardAsync(country, model, startDate, endDate);
                return View("SimpleDashboard", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading simple dashboard");
                return View("SimpleDashboard", new SimpleShippingDashboardViewModel());
            }
        }

    [HttpGet("/Shipping/simple-chart-data")]
    public async Task<JsonResult> SimpleChartData(string type, string? country = null, string? model = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var vm = await BuildSimpleDashboardAsync(country, model, startDate, endDate);
                switch (type.ToLower())
                {
                    case "pie-country-share":
                        return Json(new
                        {
                            labels = vm.CountryShares.Keys,
                            data = vm.CountryShares.Values.Select(v => Math.Round(v,2))
                        });
                    case "bar-country-volume":
                        return Json(new
                        {
                            labels = vm.CountryVolumes.Keys,
                            data = vm.CountryVolumes.Values
                        });
                    default:
                        return Json(new { error = "Unknown chart type" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating simple chart data: {Type}", type);
                return Json(new { error = ex.Message });
            }
        }

        private async Task<SimpleShippingDashboardViewModel> BuildSimpleDashboardAsync(string? country, string? model, DateTime? startDate, DateTime? endDate)
        {
            // Normalize date range defaults (last 30 days)
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            // Base query for barcodes (BOX assumed primary outbound unit)
            var query = _context.BarcodeRegistries
                .AsNoTracking()
                .Include(b => b.POMaster)
                .Where(b => b.BarcodeType == "BOX");

            if (startDate.HasValue)
                query = query.Where(b => b.GeneratedDate.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(b => b.GeneratedDate.Date <= endDate.Value.Date);
            if (!string.IsNullOrEmpty(country))
                query = query.Where(b => (b.POMaster!.Country ?? "").ToLower() == country.ToLower());
            if (!string.IsNullOrEmpty(model))
                query = query.Where(b => b.ModelProduct == model);

            var items = await query
                .Select(b => new {
                    b.BarcodeValue,
                    b.ModelProduct,
                    Country = b.POMaster!.Country ?? (b.POMaster!.ShipmentDetail ?? "") ,
                    b.Status,
                    b.GeneratedDate,
                    b.ScannedDate,
                    b.ScannedBy
                })
                .ToListAsync();

            int total = items.Count;
            int scanned = items.Count(i => i.Status == "SCANNED");
            int shipped = scanned; // assumption: scanned == shipped for now

            // Aggregate by country
            var countryGroups = items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Country) ? "Unknown" : NormalizeCountry(i.Country))
                .Select(g => new { Country = g.Key, Total = g.Count(), Scanned = g.Count(x => x.Status == "SCANNED") })
                .OrderByDescending(g => g.Total)
                .ToList();

            var countryVolumes = countryGroups.ToDictionary(g => g.Country, g => g.Total);
            double grandTotal = countryGroups.Sum(c => (double)c.Total);
            var countryShares = countryGroups.ToDictionary(g => g.Country, g => grandTotal > 0 ? g.Total / grandTotal * 100 : 0);

            var topCountries = countryGroups.Take(5)
                .Select(g => new CountrySummaryRow { Country = g.Country, Total = g.Total, Scanned = g.Scanned })
                .ToList();

        var recentActivities = items
                .OrderByDescending(i => i.GeneratedDate)
                .Take(15)
                .Select(i => new OutboundItemDto
                {
                    BarcodeValue = i.BarcodeValue,
                    ModelProduct = i.ModelProduct ?? string.Empty,
            Country = string.IsNullOrWhiteSpace(i.Country) ? "Unknown" : NormalizeCountry(i.Country),
                    Status = i.Status == "SCANNED" ? "Scanned" : "Generated",
                    GeneratedDate = i.GeneratedDate,
                    ScannedDate = i.ScannedDate,
                    ScannedBy = i.ScannedBy
                })
                .ToList();

            // Load available filters (without current filters for broad options)
            var availableCountries = await _context.BarcodeRegistries
                .Include(b => b.POMaster)
                .Where(b => b.POMaster != null && b.POMaster.Country != null && b.POMaster.Country != "")
                .Select(b => b.POMaster!.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var availableModels = await _context.BarcodeRegistries
                .Where(b => b.ModelProduct != null && b.ModelProduct != "")
                .Select(b => b.ModelProduct!)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return new SimpleShippingDashboardViewModel
            {
                TotalItems = total,
                ScannedItems = scanned,
                ShippedItems = shipped,
                SelectedCountry = country,
                SelectedModel = model,
                StartDate = startDate,
                EndDate = endDate,
                CountryVolumes = countryVolumes,
                CountryShares = countryShares,
                TopCountries = topCountries,
                RecentActivities = recentActivities,
                AvailableCountries = availableCountries,
                AvailableModels = availableModels
            };
        }

        private static string NormalizeCountry(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Unknown";
            var lower = raw.Trim().ToLower();
            // Basic normalization map
            return lower switch
            {
                "sg" or "singapore" => "Singapore",
                "my" or "malaysia" => "Malaysia",
                "id" or "indonesia" => "Indonesia",
                "vn" or "vietnam" => "Vietnam",
                "ph" or "philippines" => "Philippines",
                "th" or "thailand" => "Thailand",
                "jp" or "japan" => "Japan",
                "kr" or "korea" or "south korea" => "South Korea",
                "cn" or "china" => "China",
                _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower)
            };
        }

        /// <summary>
        /// API endpoint for real-time dashboard updates
        /// </summary>
        [HttpGet]
        [HttpGet]
        public async Task<JsonResult> GetDashboardStats(string? country = null, DateTime? startDate = null, DateTime? endDate = null, string? shipmentType = null)
        {
            try
            {
                var viewModel = await BuildShippingMonitoringViewModelAsync(country, startDate, endDate, shipmentType);
                
                return Json(new
                {
                    success = true,
                    totalSessions = viewModel.TotalSessions,
                    totalQtyTarget = viewModel.TotalQtyTarget,
                    totalQtyScanned = viewModel.TotalQtyScanned,
                    totalQtyRemaining = viewModel.TotalQtyRemaining,
                    overallProgress = viewModel.TotalQtyTarget > 0 ? (double)viewModel.TotalQtyScanned / viewModel.TotalQtyTarget * 100 : 0,
                    sessionDetails = viewModel.SessionDetails?.Select(s => new
                    {
                        sessionId = s.SessionId,
                        fileName = s.FileName,
                        country = s.Country,
                        totalQtyTarget = s.TotalQtyTarget,
                        totalQtyScanned = s.TotalQtyScanned,
                        totalQtyRemaining = s.TotalQtyRemaining,
                        progressPercentage = s.ProgressPercentage,
                        lastScanDate = s.LastScanDate?.ToString("dd/MM/yyyy HH:mm")
                    }).ToList(),
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// SHIPPING MONITORING DASHBOARD - Real-time tracking
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard(string? country = null, DateTime? startDate = null, DateTime? endDate = null, string? shipmentType = null)
        {
            try
            {
                var viewModel = await BuildShippingMonitoringViewModelAsync(country, startDate, endDate, shipmentType);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shipping monitoring dashboard");
                return View(new ShippingMonitoringViewModel());
            }
        }

        /// <summary>
        /// Executive Dashboard - Focus on barang keluar with real metrics
        /// </summary>
        public async Task<IActionResult> DashboardOld(string? selectedModel = null, DateTime? startDate = null, DateTime? endDate = null)
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
                        ,
                    // Choose latest scanned date if available, otherwise latest generated date
                    ShipmentDate = g.Where(x => x.ScannedDate.HasValue)
                                .Select(x => x.ScannedDate)
                                .OrderByDescending(d => d)
                                .FirstOrDefault()
                                ?? g.Select(x => (DateTime?)x.GeneratedDate)
                                    .OrderByDescending(d => d)
                                    .FirstOrDefault()
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
        
    // Legacy dashboard chart data
    [HttpGet("/Shipping/legacy-chart-data")]
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

        /// <summary>
        /// 🚀 BUILD SHIPPING MONITORING VIEW MODEL - Like PO Details Breakdown concept
        /// Smart QTY calculation using ModelConfiguration (Pallet/Box to Pieces conversion)
        /// </summary>
        private async Task<ShippingMonitoringViewModel> BuildShippingMonitoringViewModelAsync(string? country, DateTime? startDate, DateTime? endDate, string? shipmentType = null)
        {
            // Set default date range (last 30 days)
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today.AddDays(1); // Include today
            
            // Get all sessions in date range, filter out NULL shipment types
            var sessionsQuery = _context.UploadSessions
                .Include(s => s.POMasters)
                .Where(s => s.UploadDate >= startDate && s.UploadDate < endDate)
                .Where(s => !string.IsNullOrEmpty(s.ShipmentType)); // Filter out NULL shipment types
                
            if (!string.IsNullOrEmpty(country))
            {
                sessionsQuery = sessionsQuery.Where(s => s.POMasters.Any(p => p.Country == country));
            }
            
            if (!string.IsNullOrEmpty(shipmentType))
            {
                sessionsQuery = sessionsQuery.Where(s => s.ShipmentType == shipmentType);
            }
            
            var sessions = await sessionsQuery.ToListAsync();
            
            // Get model configurations for smart calculation
            var modelConfigs = await _context.ModelConfigurations.ToListAsync();
            
            var sessionDetails = new List<SessionMonitoringInfo>();
            
            foreach (var session in sessions)
            {
                try
                {
                    // Use same logic as QR Management - get data via QRManagementService
                    var qrData = await GetQRDataForDashboard(session.SessionId, modelConfigs);
                    
                    if (qrData == null) continue;
                    
                    // Get country from first PO (sessions are usually single country)
                    var sessionCountry = session.POMasters.FirstOrDefault()?.Country ?? "Unknown";
                    
                    // Use QR Management calculations (already includes ModelConfig conversions)
                    var totalQtyTarget = qrData.TotalQty; // This is sum of QtyTotal from POMasters
                    var totalQtyScanned = CalculateScannedQtyWithModelConfig(qrData, modelConfigs, session.ShipmentType);
                    var totalQtyRemaining = totalQtyTarget - totalQtyScanned;
                    
                    var progressPercentage = totalQtyTarget > 0 ? (double)totalQtyScanned / totalQtyTarget * 100 : 0;
                    
                    // Determine status based on progress
                    var status = progressPercentage >= 100 ? "COMPLETED" :
                                progressPercentage > 0 ? "IN PROGRESS" : "PENDING";
                    
                    var statusClass = status switch
                    {
                        "COMPLETED" => "success",
                        "IN PROGRESS" => "warning",
                        _ => "secondary"
                    };
                    
                    var progressClass = progressPercentage switch
                    {
                        >= 100 => "success",
                        >= 75 => "info",
                        >= 50 => "warning",
                        _ => "danger"
                    };
                    
                    // Calculate scanning velocity (items per hour)
                    var scanningVelocity = CalculateScanningVelocity(session, totalQtyScanned);
                    
                    // Estimate completion time
                    var estimatedCompletion = EstimateCompletionTime(totalQtyRemaining, scanningVelocity);
                    
                    sessionDetails.Add(new SessionMonitoringInfo
                    {
                        SessionId = session.SessionId,
                        FileName = session.FileName ?? "Unknown",
                        Country = sessionCountry,
                        ShipmentType = session.ShipmentType ?? "Unknown",
                        CreatedDate = session.UploadDate,
                        LastScanDate = qrData.LastScannedDate,
                        
                        // Basic PO information
                        TotalPOs = session.POMasters.Count,
                        
                        // QTY calculations using same logic as QR Management
                        TotalQtyTarget = totalQtyTarget,
                        TotalQtyScanned = totalQtyScanned,
                        TotalQtyRemaining = totalQtyRemaining,
                        ProgressPercentage = Math.Round(progressPercentage, 1),
                        
                        // Raw breakdown from QR Management data
                        TotalPallets = qrData.TotalPallets,
                        ScannedPallets = qrData.ScannedPallets,
                        TotalBoxes = qrData.TotalBoxes,
                        ScannedBoxes = qrData.ScannedBoxes,
                        TotalPcs = qrData.TotalPcs,
                        ScannedPcs = qrData.ScannedPcs,
                        
                        // Status indicators
                        Status = status,
                        StatusClass = statusClass,
                        ProgressClass = progressClass,
                        
                        // Performance metrics
                        ScanningVelocity = scanningVelocity,
                        EstimatedCompletion = estimatedCompletion,
                        
                        // No model breakdown for now - keep it simple like QR Management
                        ModelBreakdowns = new List<ModelBreakdown>()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing session {SessionId}", session.SessionId);
                    // Continue with other sessions
                }
            }
            
            // Get available countries for filter
            var availableCountries = await _context.POMasters
                .Where(p => !string.IsNullOrEmpty(p.Country))
                .Select(p => p.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            
            // Get available shipment types for filter
            var availableShipmentTypes = await _context.UploadSessions
                .Where(s => !string.IsNullOrEmpty(s.ShipmentType))
                .Select(s => s.ShipmentType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
            
            // Calculate summary statistics
            var totalSessions = sessionDetails.Count;
            var completedSessions = sessionDetails.Count(s => s.Status == "COMPLETED");
            var inProgressSessions = sessionDetails.Count(s => s.Status == "IN PROGRESS");
            var pendingSessions = sessionDetails.Count(s => s.Status == "PENDING");
            var overallCompletionRate = totalSessions > 0 ? (double)completedSessions / totalSessions * 100 : 0;
            
            return new ShippingMonitoringViewModel
            {
                SelectedCountry = country,
                SelectedShipmentType = shipmentType,
                StartDate = startDate,
                EndDate = endDate?.AddDays(-1), // Adjust back for display
                
                TotalSessions = totalSessions,
                CompletedSessions = completedSessions,
                InProgressSessions = inProgressSessions,
                PendingSessions = pendingSessions,
                OverallCompletionRate = Math.Round(overallCompletionRate, 1),
                
                SessionDetails = sessionDetails.OrderByDescending(s => s.CreatedDate).ToList(),
                AvailableCountries = availableCountries,
                AvailableShipmentTypes = availableShipmentTypes,
                
                TotalQtyTarget = sessionDetails.Sum(s => s.TotalQtyTarget),
                TotalQtyScanned = sessionDetails.Sum(s => s.TotalQtyScanned),
                TotalQtyRemaining = sessionDetails.Sum(s => s.TotalQtyRemaining),
                
                LastUpdateTime = DateTime.Now
            };
        }
        
        /// <summary>
        /// Calculate scanning velocity (items per hour)
        /// </summary>
        private double CalculateScanningVelocity(UploadSession session, int totalScanned)
        {
            var timeElapsed = DateTime.Now - session.UploadDate;
            var hoursElapsed = Math.Max(timeElapsed.TotalHours, 0.1); // Prevent division by zero
            
            return totalScanned / hoursElapsed;
        }
        
        /// <summary>
        /// Estimate completion time based on current velocity
        /// </summary>
        private TimeSpan? EstimateCompletionTime(int remaining, double velocity)
        {
            if (remaining <= 0 || velocity <= 0) return null;
            
            var hoursToComplete = remaining / velocity;
            return TimeSpan.FromHours(hoursToComplete);
        }

        /// <summary>
        /// Get QR data for dashboard using same logic as QR Management
        /// </summary>
        private async Task<QRManagementDto?> GetQRDataForDashboard(int sessionId, List<ModelConfiguration> modelConfigs)
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return null;

                // Get PO Master data for this session
                var poMasters = await _context.POMasters
                    .Where(po => po.SourceSessionId == sessionId)
                    .ToListAsync();

                if (!poMasters.Any())
                    return null;

                // Calculate totals from PO Masters (same as QR Management)
                var totalPallets = poMasters.Sum(po => po.QtyPallet);
                var totalBoxes = poMasters.Sum(po => po.QtyBox);
                var totalPcs = poMasters.Sum(po => po.QtyPcs);
                var totalQty = poMasters.Sum(po => po.QtyTotal);

                // Get ACCURATE scan count from POItemRegistries (hierarchical lock data) - same as QR Management
                var scannedItemsFromRegistry = await _context.POItemRegistries
                    .Where(pir => poMasters.Select(po => po.POId).Contains(pir.POId) &&
                                 pir.Status == "SCANNED" &&
                                 pir.IsActive)
                    .ToListAsync();

                // Use POItemRegistries data if available (hierarchical lock system)
                int actualScannedBoxes = 0, actualScannedPallets = 0, actualScannedPcs = 0;
                if (scannedItemsFromRegistry.Count > 0)
                {
                    actualScannedBoxes = scannedItemsFromRegistry.Count(pir => pir.ItemType == "BOX");
                    actualScannedPallets = scannedItemsFromRegistry.Count(pir => pir.ItemType == "PALLET");
                    actualScannedPcs = scannedItemsFromRegistry.Count(pir => pir.ItemType == "PCS");
                }
                else
                {
                    // Fallback to scanning service data
                    var scanProgress = await _scanService.GetScanProgressAsync(sessionId);
                    if (scanProgress != null)
                    {
                        actualScannedBoxes = scanProgress.ScannedBoxes;
                        actualScannedPallets = scanProgress.ScannedPallets;
                        actualScannedPcs = scanProgress.ScannedPcs;
                    }
                }

                // Get last scanned date from ScanningActivities
                var lastScanned = await _context.ScanningActivities
                    .Where(sa => sa.Result == "SUCCESS")
                    .OrderByDescending(sa => sa.Timestamp)
                    .FirstOrDefaultAsync();

                return new QRManagementDto
                {
                    SessionId = sessionId,
                    FileName = session.FileName ?? "",
                    Country = session.Country,
                    ShipmentDate = session.ShipmentDate,
                    TotalBoxes = totalBoxes,
                    TotalPallets = totalPallets,
                    TotalPcs = totalPcs,
                    TotalQty = totalQty,
                    ScannedBoxes = actualScannedBoxes,
                    ScannedPallets = actualScannedPallets,
                    ScannedPcs = actualScannedPcs,
                    LastScannedDate = lastScanned?.Timestamp
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate scanned qty with model configuration conversion (same as QR Management)
        /// </summary>
        private int CalculateScannedQtyWithModelConfig(QRManagementDto qrData, List<ModelConfiguration> modelConfigs, string? shipmentType)
        {
            try
            {
                // Get session's PO masters to determine models
                var poMasters = _context.POMasters
                    .Where(po => po.SourceSessionId == qrData.SessionId)
                    .ToList();

                var totalScannedQty = 0;

                // Group by model and calculate
                var modelGroups = poMasters.GroupBy(po => po.ModelProduk);
                
                foreach (var modelGroup in modelGroups)
                {
                    var modelName = modelGroup.Key;
                    
                    // Get model configuration for this shipment type
                    var modelConfig = modelConfigs.FirstOrDefault(mc => 
                        mc.ModelName == modelName && mc.Type == (shipmentType ?? "LOOSE"));
                    
                    if (modelConfig != null)
                    {
                        // Convert scanned pallets and boxes to qty using model config
                        var scannedPalletQty = qrData.ScannedPallets * modelConfig.PcsPerPallet;
                        var scannedBoxQty = qrData.ScannedBoxes * modelConfig.PcsPerBox;
                        var scannedPcsQty = qrData.ScannedPcs; // PCS is 1:1
                        
                        totalScannedQty += scannedPalletQty + scannedBoxQty + scannedPcsQty;
                    }
                    else
                    {
                        // Fallback: if no model config, treat as 1:1
                        totalScannedQty += qrData.ScannedPallets + qrData.ScannedBoxes + qrData.ScannedPcs;
                    }
                }

                return totalScannedQty;
            }
            catch (Exception)
            {
                // Fallback calculation without model config
                return qrData.ScannedPallets + qrData.ScannedBoxes + qrData.ScannedPcs;
            }
        }
    }
}
