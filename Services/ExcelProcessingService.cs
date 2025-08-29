using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using System.Security.Cryptography;
using ShipmentFinishGood.Utilities;

namespace ShipmentFinishGood.Services
{
    public class ExcelProcessingService : IExcelProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IModelConfigurationService _modelConfigService;
        private readonly IBarcodeService _barcodeService;

        public ExcelProcessingService(AppDbContext context, IModelConfigurationService modelConfigService, IBarcodeService barcodeService)
        {
            _context = context;
            _modelConfigService = modelConfigService;
            _barcodeService = barcodeService;
        }

        public async Task<UploadPreviewDto> ProcessExcelFileAsync(IFormFile file, string uploadedBy)
        {
            var fileHash = await GenerateFileHashAsync(file);
            
            var duplicateCheck = await _context.UploadSessions
                .Where(s => s.FileHash == fileHash && 
                           s.UploadedBy == uploadedBy && 
                           s.UploadDate >= DateTime.Now.AddDays(-1) &&
                           s.Status != "DELETED")
                .FirstOrDefaultAsync();

            if (duplicateCheck != null)
            {
                throw new InvalidOperationException($"File '{file.FileName}' dengan konten yang sama sudah diupload pada {duplicateCheck.UploadDate:dd/MM/yyyy HH:mm}. Session ID: {duplicateCheck.SessionId}");
            }

            var (rawData, formatInfo) = ParseExcelFileWithFormatDetection(file);
            // Parsing completed: rawData & formatInfo prepared
            
            var dataByCountry = rawData
                .GroupBy(r => CountryNormalizer.NormalizeOrUnknown(r.Country))
                .ToDictionary(g => g.Key, g => g.ToList());

            var processedDataByCountry = new Dictionary<string, List<ProcessedPOData>>();
            foreach (var countryGroup in dataByCountry)
            {
                var processedData = await CalculateProcessedDataAsync(countryGroup.Value, "LOOSE");
                processedDataByCountry[countryGroup.Key] = processedData;
            }

            var session = new UploadSession
            {
                FileName = file.FileName,
                SheetName = "Sheet1",
                Status = "PREVIEW",
                UploadDate = DateTime.Now,
                UploadedBy = uploadedBy,
                SheetIdentifier = GenerateSheetIdentifier(),
                FileHash = fileHash,
                Countries = string.Join(",", dataByCountry.Keys)
            };
            
            _context.UploadSessions.Add(session);
            await _context.SaveChangesAsync();

            foreach (var row in rawData)
            {
                var detail = new UploadSessionDetail
                {
                    SessionId = session.SessionId,
                    OriginalPO = row.NoPO,
                    Model = row.Model,
                    OriginalQty = row.Qty,
                    RowIndex = row.RowIndex,
                    Country = row.Country
                };
                _context.UploadSessionDetails.Add(detail);
            }
            await _context.SaveChangesAsync();

            var allProcessedData = processedDataByCountry.Values.SelectMany(x => x).ToList();

            return new UploadPreviewDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                RawData = rawData,
                ProcessedData = allProcessedData,
                ProcessedDataByCountry = processedDataByCountry,
                PendingCountries = dataByCountry.Keys.ToList(),
                SubmittedCountries = new List<string>(),
                FormatInfo = formatInfo
            };
        }

        private (List<ExcelRowData> rawData, ExcelFormatInfo formatInfo) ParseExcelFileWithFormatDetection(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            var formatType = DetectFormatType(workbook);

            // Parse data FIRST so we can build an accurate sheet/country summary (avoid phantom names like "USA" / "Bucharest")
            List<ExcelRowData> rawData = formatType switch
            {
                "STUFFING_PLAN" => ParseStuffingPlanFormat(workbook),
                "OLD_SINGLE_SHEET" => ParseSingleSheetFormat(workbook),
                _ => ParseMultiSheetFormat(workbook)
            };

            var detectedCountries = rawData
                .Select(r => r.Country)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Cast<string>()
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Only count sheets that actually produced data rows. This removes empty/hidden/template sheets.
            // UI previously showed workbook.Worksheets.Count causing inflated counts (e.g. 10) and unrelated names.
            var effectiveSheetCount = detectedCountries.Count;

            var formatInfo = new ExcelFormatInfo
            {
                FormatType = formatType,
                TotalSheets = effectiveSheetCount,
                SheetNames = detectedCountries, // Use validated country list instead of raw workbook sheet names
                Description = GetFormatDescription(formatType, effectiveSheetCount),
                DetectedCountries = detectedCountries,
                TotalRows = rawData.Count
            };
            // Parse completed summary prepared in formatInfo

            return (rawData, formatInfo);
        }

        private string DetectFormatType(IXLWorkbook workbook)
        {
            try
            {
                if (IsStuffingPlanFormat(workbook))
                {
                    return "STUFFING_PLAN";
                }
                
                if (IsOldFormat(workbook))
                {
                    return "OLD_SINGLE_SHEET";
                }
                
                return "MULTI_SHEET";
            }
            catch (Exception)
            {
                return "MULTI_SHEET";
            }
        }

        private bool IsStuffingPlanFormat(IXLWorkbook workbook)
        {
            try
            {
                // Enhanced detection for stuffing plan format
                var stuffingPlanIndicators = new[]
                {
                    "STUFFING PLAN", "STUFFING_PLAN", "Export Department", "EXPORT DEPARTMENT",
                    "Shipment to", "SHIPMENT TO", "N.W (Kg)", "G.W (Kg)", "Volume (M3)", "Ready :"
                };

                var dataHeaders = new[] { "NO PO", "NO MODEL", "QTY" };

                foreach (var worksheet in workbook.Worksheets)
                {
                    var lastRow = Math.Min(20, worksheet.LastRowUsed()?.RowNumber() ?? 0);
                    
                    // Check for stuffing plan indicators
                    bool hasStuffingPlanIndicator = false;
                    int headerMatchCount = 0;
                    
                    for (int row = 1; row <= lastRow; row++)
                    {
                        for (int col = 1; col <= 15; col++)
                        {
                            var cellValue = worksheet.Cell(row, col).GetString().Trim();
                            
                            // Check for stuffing plan indicators
                            if (stuffingPlanIndicators.Any(indicator => 
                                cellValue.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
                            {
                                hasStuffingPlanIndicator = true;
                            }
                            
                            // Check for required data headers
                            if (dataHeaders.Any(header => 
                                cellValue.Equals(header, StringComparison.OrdinalIgnoreCase)))
                            {
                                headerMatchCount++;
                            }
                        }
                    }
                    
                    // If we have stuffing plan indicators OR at least 2 of the 3 required headers
                    if (hasStuffingPlanIndicator || headerMatchCount >= 2)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

    private List<ExcelRowData> ParseStuffingPlanFormat(IXLWorkbook workbook)
        {
            var rawData = new List<ExcelRowData>();
            int globalRowIndex = 1;

            foreach (var worksheet in workbook.Worksheets)
            {
                // COUNTRY: Extract from sheet name (requirement #4)
                var countryFromSheetName = CountryNormalizer.Normalize(worksheet.Name);
                var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                
                
                if (lastRowUsed <= 1) continue;

                var dataStartRow = FindDataStartRow(worksheet);
                if (dataStartRow == -1) continue;

        var (poCol, modelCol, qtyCol, countryCol) = FindColumnIndexes(worksheet, dataStartRow);
                if (poCol == -1 || modelCol == -1 || qtyCol == -1) continue;

                // Inheritance variables for NO PO and NO MODEL (requirements #1 & #2)
                var currentPO = string.Empty;
                var currentModel = string.Empty;
                // Inheritance for COUNTRY: prefer header column if present, otherwise fallback to sheet name
                var currentCountry = countryFromSheetName;

                var parsedRows = 0;
                for (int row = dataStartRow + 1; row <= lastRowUsed; row++)
                {
                    globalRowIndex++;
                    
                    var noPOCell = worksheet.Cell(row, poCol).GetString().Trim();
                    var modelCell = worksheet.Cell(row, modelCol).GetString().Trim();
                    var qtyCell = worksheet.Cell(row, qtyCol).GetString().Trim();
                    // Update country early even if this is a group-only row
                    if (countryCol != -1)
                    {
                        var countryCell = worksheet.Cell(row, countryCol).GetString().Trim();
                        if (!string.IsNullOrEmpty(countryCell))
                        {
                            currentCountry = CountryNormalizer.Normalize(countryCell);
                        }
                    }

                    // Skip completely empty rows
                    if (string.IsNullOrEmpty(noPOCell) && 
                        string.IsNullOrEmpty(modelCell) && 
                        string.IsNullOrEmpty(qtyCell))
                        continue;

                    // NO PO: Inheritance rule - if empty, inherit from row above (requirement #1)
                    if (!string.IsNullOrEmpty(noPOCell))
                        currentPO = noPOCell;

                    // NO MODEL: Inheritance rule - if empty, inherit from row above (requirement #2)
                    if (!string.IsNullOrEmpty(modelCell))
                        currentModel = modelCell;

                    // QTY: Always has unique value per row (requirement #3)
                    if (!ParseQuantity(qtyCell, out int qty))
                        continue;

                    // Only add valid rows with all required data
            if (!string.IsNullOrEmpty(currentPO) && 
                        !string.IsNullOrEmpty(currentModel) && 
                        qty > 0)
                    {
                        rawData.Add(new ExcelRowData
                        {
                            NoPO = currentPO,
                Country = currentCountry,
                            Model = currentModel,
                            Qty = qty,
                            RowIndex = globalRowIndex
                        });
            parsedRows++;
                    }
                }
                
            }

            return rawData;
        }

        private int FindDataStartRow(IXLWorksheet worksheet)
        {
            // Look for exact header indicators, NOT "LCL" which is shipment type
            var headerIndicators = new[] { "NO PO", "NO MODEL", "QTY" };
            
            var lastRow = Math.Min(20, worksheet.LastRowUsed()?.RowNumber() ?? 0);
            for (int row = 1; row <= lastRow; row++)
            {
                int matchCount = 0;
                
                // Check if this row contains all 3 required headers
                for (int col = 1; col <= 30; col++)
                {
                    var cellValue = worksheet.Cell(row, col).GetString().Trim().ToUpper();
                    
                    if (headerIndicators.Any(indicator => 
                        cellValue.Equals(indicator, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchCount++;
                    }
                }
                
                // If we found at least 2 of the 3 headers in this row, it's likely the header row
                if (matchCount >= 2)
                {
                    return row;
                }
            }
            
            return -1;
        }

        private (int poCol, int modelCol, int qtyCol, int countryCol) FindColumnIndexes(IXLWorksheet worksheet, int headerRow)
        {
            int poCol = -1, modelCol = -1, qtyCol = -1, countryCol = -1;
            
            // Search for exact column headers in stuffing plan format
            for (int col = 1; col <= 30; col++)
            {
                var headerValue = worksheet.Cell(headerRow, col).GetString().Trim().ToUpper();
                
                // Read column header
                
                // NO PO column detection - be more specific (requirement #1)
                if (headerValue.Equals("NO PO", StringComparison.OrdinalIgnoreCase))
                    poCol = col;
                    
                // NO MODEL column detection - be more specific (requirement #2) 
                else if (headerValue.Equals("NO MODEL", StringComparison.OrdinalIgnoreCase))
                    modelCol = col;
                    
                // QTY column detection - be more specific (requirement #3)
                else if (headerValue.Equals("QTY", StringComparison.OrdinalIgnoreCase))
                    qtyCol = col;

                // COUNTRY column detection - support common synonyms
                else if (headerValue.Equals("COUNTRY", StringComparison.OrdinalIgnoreCase) ||
                         headerValue.Contains("DESTINATION COUNTRY"))
                    countryCol = col;
            }
            
            // If exact matches not found, try partial matches
            if (poCol == -1 || modelCol == -1 || qtyCol == -1 || countryCol == -1)
            {
                for (int col = 1; col <= 30; col++)
                {
                    var headerValue = worksheet.Cell(headerRow, col).GetString().Trim().ToUpper();
                    
                    if (poCol == -1 && headerValue.Contains("PO") && !headerValue.Contains("MODEL"))
                        poCol = col;
                    else if (modelCol == -1 && headerValue.Contains("MODEL"))
                        modelCol = col;
                    else if (qtyCol == -1 && headerValue.Contains("QTY"))
                        qtyCol = col;
                    else if (countryCol == -1 && (headerValue.Contains("COUNTRY") || headerValue.Contains("DESTINATION")))
                        countryCol = col;
                }
            }
            
            // Final fallback - but warn about it
            if (poCol == -1)
            {
                poCol = 1;
            }
            if (modelCol == -1)
            {
                modelCol = 2;
            }
            if (qtyCol == -1)
            {
                qtyCol = 3;
            }
            
            
            return (poCol, modelCol, qtyCol, countryCol);
        }

        private bool ParseQuantity(string qtyCell, out int qty)
        {
            qty = 0;
            
            if (string.IsNullOrEmpty(qtyCell)) return false;
            
            // Handle specific formats from Stuffing Plan:
            // Examples: "1.149 Sets", "383 SP", "2.700", "63", "129"
            var cleanQty = qtyCell
                .Replace(" Sets", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" SP", "", StringComparison.OrdinalIgnoreCase)  
                .Replace("Sets", "", StringComparison.OrdinalIgnoreCase)
                .Replace("SP", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Trim();
            
            // Handle decimal notation with dots (e.g., "1.149" -> 1149)
            if (cleanQty.Contains("."))
            {
                cleanQty = cleanQty.Replace(".", "");
            }
            
            // Handle comma notation if any (e.g., "1,149" -> 1149) 
            if (cleanQty.Contains(","))
            {
                cleanQty = cleanQty.Replace(",", "");
            }
            
            // Try to parse as integer
            if (int.TryParse(cleanQty, out qty))
                return qty > 0;
            
            // Fallback: try decimal parsing for edge cases
            if (decimal.TryParse(cleanQty, out decimal decimalQty))
            {
                qty = (int)Math.Round(decimalQty);
                return qty > 0;
            }
            
            return false;
        }

        private string GetFormatDescription(string formatType, int sheetCount)
        {
            return formatType switch
            {
                "STUFFING_PLAN" => $"Stuffing Plan format: {sheetCount} sheets with structured data (PO | Model | Qty)",
                "OLD_SINGLE_SHEET" => "Legacy single-sheet format: 1 sheet with 4 columns (Country | PO | Model | Qty)",
                "MULTI_SHEET" => $"Multi-sheet format: {sheetCount} sheets with 3 columns per sheet (PO | Model | Qty)",
                _ => $"Unknown format: {sheetCount} sheets"
            };
        }

        private bool IsOldFormat(IXLWorkbook workbook)
        {
            try
            {
                if (workbook.Worksheets.Count != 1) return false;
                
                var worksheet = workbook.Worksheet(1);
                var headerCell = worksheet.Cell(1, 1).GetString().Trim().ToUpper();
                
                if (headerCell.Contains("COUNTRY") || headerCell.Contains("NEGARA") || 
                    headerCell.Contains("NATION") || headerCell.Contains("PAIS") ||
                    headerCell.Contains("PAÍSES") || headerCell.Contains("LAND"))
                {
                    return true;
                }

                try
                {
                    var col1 = worksheet.Cell(2, 1).GetString().Trim();
                    var col2 = worksheet.Cell(2, 2).GetString().Trim();
                    var col3 = worksheet.Cell(2, 3).GetString().Trim();
                    var col4 = worksheet.Cell(2, 4).GetString().Trim();
                    
                    if (!string.IsNullOrEmpty(col1) && !string.IsNullOrEmpty(col2) && 
                        !string.IsNullOrEmpty(col3) && !string.IsNullOrEmpty(col4))
                    {
                        var potentialCountry = col1.ToUpper();
                        var commonCountries = new[] { "AUSTRALIA", "CANADA", "GERMANY", "USA", "UK", "FRANCE", 
                                                    "JAPAN", "SINGAPORE", "MALAYSIA", "INDONESIA", "THAILAND",
                                                    "PHILIPPINES", "VIETNAM", "INDIA", "CHINA", "KOREA", "BRAZIL" };
                        
                        return commonCountries.Any(country => potentialCountry.Contains(country));
                    }
                }
                catch
                {
                    // Continue with other checks
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private List<ExcelRowData> ParseSingleSheetFormat(IXLWorkbook workbook)
        {
            var rawData = new List<ExcelRowData>();
            var worksheet = workbook.Worksheet(1);
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var currentPO = string.Empty;
            var currentCountry = string.Empty;

            for (int row = 2; row <= lastRowUsed; row++)
            {
                var countryCell = worksheet.Cell(row, 1).GetString().Trim();
                var noPOCell = worksheet.Cell(row, 2).GetString().Trim();
                var modelCell = worksheet.Cell(row, 3).GetString().Trim();
                var qtyCell = worksheet.Cell(row, 4).GetString().Trim();

                if (string.IsNullOrEmpty(modelCell) && string.IsNullOrEmpty(qtyCell))
                    continue;

                if (!string.IsNullOrEmpty(countryCell))
                    currentCountry = CountryNormalizer.Normalize(countryCell);

                if (!string.IsNullOrEmpty(noPOCell))
                    currentPO = noPOCell;

                if (!ParseQuantity(qtyCell, out int qty))
                    continue;

                rawData.Add(new ExcelRowData
                {
                    NoPO = currentPO,
                    Country = currentCountry,
                    Model = modelCell,
                    Qty = qty,
                    RowIndex = row
                });
            }

            return rawData;
        }

        private List<ExcelRowData> ParseMultiSheetFormat(IXLWorkbook workbook)
        {
            var rawData = new List<ExcelRowData>();
            int globalRowIndex = 1;

            foreach (var worksheet in workbook.Worksheets)
            {
                var countryFromSheetName = CountryNormalizer.Normalize(worksheet.Name);
                var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                var currentPO = string.Empty;

                if (lastRowUsed <= 1) continue;

                for (int row = 2; row <= lastRowUsed; row++)
                {
                    globalRowIndex++;
                    
                    var noPOCell = worksheet.Cell(row, 1).GetString().Trim();
                    var modelCell = worksheet.Cell(row, 2).GetString().Trim();
                    var qtyCell = worksheet.Cell(row, 3).GetString().Trim();

                    if (string.IsNullOrEmpty(modelCell) && string.IsNullOrEmpty(qtyCell))
                        continue;

                    if (!string.IsNullOrEmpty(noPOCell))
                        currentPO = noPOCell;

                    if (!ParseQuantity(qtyCell, out int qty))
                        continue;

                    rawData.Add(new ExcelRowData
                    {
                        NoPO = currentPO,
                        Country = countryFromSheetName,
                        Model = modelCell,
                        Qty = qty,
                        RowIndex = globalRowIndex
                    });
                }
            }

            return rawData;
        }

        public async Task<List<ProcessedPOData>> CalculateProcessedDataAsync(List<ExcelRowData> rawData, string shipmentType)
        {
            var modelConfigs = await _modelConfigService.GetAllAsync();
            var configDict = modelConfigs
                .GroupBy(m => m.ModelName)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<ProcessedPOData> groupedData;

            if (shipmentType == "LOOSE")
            {
                groupedData = rawData
                    .GroupBy(r => r.Model)
                    .Select(g => new ProcessedPOData
                    {
                        NoPO = string.Join(", ", g.Select(x => x.NoPO).Distinct()),
                        Country = g.Select(x => x.Country).FirstOrDefault() ?? "",
                        Model = g.Key ?? "",
                        TotalQty = g.Sum(x => x.Qty)
                    })
                    .ToList();
            }
            else
            {
                groupedData = rawData
                    .GroupBy(r => new { r.NoPO, r.Model })
                    .Select(g => new ProcessedPOData
                    {
                        NoPO = g.Key.NoPO ?? "",
                        Country = g.Select(x => x.Country).FirstOrDefault() ?? "",
                        Model = g.Key.Model ?? "",
                        TotalQty = g.Sum(x => x.Qty)
                    })
                    .ToList();
            }

            foreach (var item in groupedData)
            {
                CalculateBreakdown(item, configDict, shipmentType);
            }

            return groupedData.OrderBy(x => x.NoPO).ThenBy(x => x.Model).ToList();
        }

        public async Task<ProcessedPOData> CalculateSingleRowAsync(string model, int totalQty, string shipmentType)
        {
            var modelConfigs = await _modelConfigService.GetAllAsync();
            var configDict = modelConfigs
                .GroupBy(m => m.ModelName)
                .ToDictionary(g => g.Key, g => g.ToList());

            var processedData = new ProcessedPOData
            {
                Model = model,
                TotalQty = totalQty
            };

            CalculateBreakdown(processedData, configDict, shipmentType);
            return processedData;
        }

        private void CalculateBreakdown(ProcessedPOData item, Dictionary<string, List<ModelConfiguration>> configDict, string shipmentType)
        {
            if (!configDict.TryGetValue(item.Model, out var configs) || configs.Count == 0)
            {
                item.QtyPcs = item.TotalQty;
                return;
            }

            var configForType = configs.FirstOrDefault(c => string.Equals(c.Type, shipmentType, StringComparison.OrdinalIgnoreCase))
                               ?? configs.First();

            var palletConfig = configs.FirstOrDefault(c => string.Equals(c.Type, "PALLET", StringComparison.OrdinalIgnoreCase));
            var pcsPerPallet = string.Equals(shipmentType, "PALLET", StringComparison.OrdinalIgnoreCase)
                ? (palletConfig?.PcsPerPallet ?? configForType.PcsPerPallet)
                : configForType.PcsPerPallet;

            var pcsPerBox = configForType.PcsPerBox;
            var remainingQty = item.TotalQty;

            if (pcsPerPallet > 0)
            {
                item.QtyPallet = remainingQty / pcsPerPallet;
                remainingQty = remainingQty % pcsPerPallet;
            }

            if (pcsPerBox > 0)
            {
                item.QtyBox = remainingQty / pcsPerBox;
                remainingQty = remainingQty % pcsPerBox;
            }

            item.QtyPcs = remainingQty;
        }

        public async Task<bool> SubmitProcessedDataAsync(CalculationRequest request, string createdBy)
        {
            var session = await _context.UploadSessions.FindAsync(request.SessionId);
            if (session == null) return false;

            session.ShipmentType = request.ShipmentType;
            session.ShipmentDate = request.ShipmentDate;
            session.Status = "PROCESSED";

            foreach (var item in request.ProcessedData)
            {
                var poMaster = new POMaster
                {
                    NoPO = item.NoPO,
                    ModelProduk = item.Model,
                    QtyTotal = item.TotalQty,
                    QtyPallet = item.QtyPallet,
                    QtyBox = item.QtyBox,
                    QtyPcs = item.QtyPcs,
                    Container = item.Container,
                    NoInvoice = item.NoInvoice,
                    ShipmentDetail = item.ShipmentDetail,
                    SourceSessionId = request.SessionId,
                    ShipmentMethod = request.ShipmentType,
                    CreatedBy = createdBy,
                    Country = CountryNormalizer.Normalize(item.Country)
                };

                _context.POMasters.Add(poMaster);
            }

            await _context.SaveChangesAsync();

            session.IdentityQRCode = $"QR_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
            _context.UploadSessions.Update(session);
            await _context.SaveChangesAsync();

            await _barcodeService.GenerateBarcodesForSessionAsync(session.SessionId, createdBy);
            return true;
        }

        public async Task<bool> SubmitCountryDataAsync(CountrySubmissionRequest request, string createdBy)
        {
            var normalizedRequestCountry = CountryNormalizer.Normalize(request.Country);

            var existingCountrySubmission = await _context.UploadSessions
                .Where(s => s.ParentSessionId == request.SessionId && 
                            s.Country == normalizedRequestCountry && 
                            s.Status == "PROCESSED")
                .FirstOrDefaultAsync();

            if (existingCountrySubmission != null)
            {
                throw new InvalidOperationException($"Country {request.Country} sudah di-submit sebelumnya!");
            }

            var parentSession = await _context.UploadSessions.FindAsync(request.SessionId);
            if (parentSession == null) return false;

            var newSession = new UploadSession
            {
                FileName = $"{parentSession.FileName} - {request.Country}",
                SheetName = parentSession.SheetName,
                ShipmentType = request.ShipmentType,
                ShipmentDate = request.ShipmentDate,
                Status = "PROCESSED",
                UploadedBy = parentSession.UploadedBy,
                Country = CountryNormalizer.Normalize(request.Country),
                ParentSessionId = request.SessionId,
                SheetIdentifier = GenerateSheetIdentifier()
            };

            _context.UploadSessions.Add(newSession);
            await _context.SaveChangesAsync();

            foreach (var item in request.ProcessedData)
            {
                var poMaster = new POMaster
                {
                    NoPO = item.NoPO,
                    ModelProduk = item.Model,
                    QtyTotal = item.TotalQty,
                    QtyPallet = item.QtyPallet,
                    QtyBox = item.QtyBox,
                    QtyPcs = item.QtyPcs,
                    Container = item.Container,
                    NoInvoice = item.NoInvoice,
                    ShipmentDetail = item.ShipmentDetail,
                    SourceSessionId = newSession.SessionId,
                    ShipmentMethod = request.ShipmentType,
                    CreatedBy = createdBy,
                    Country = CountryNormalizer.Normalize(request.Country)
                };

                _context.POMasters.Add(poMaster);
            }

            await _context.SaveChangesAsync();

            newSession.IdentityQRCode = $"QR_{newSession.SessionId}_{DateTime.Now:yyyyMMddHHmmss}";
            _context.UploadSessions.Update(newSession);
            await _context.SaveChangesAsync();

            await _barcodeService.GenerateBarcodesForSessionAsync(newSession.SessionId, createdBy);
            return true;
        }

        public async Task<UploadPreviewDto?> GetPreviewAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.Details)
                .Include(s => s.ChildSessions)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return null;

            var submittedCountries = session.ChildSessions
                .Where(c => c.Status == "PROCESSED" && !string.IsNullOrEmpty(c.Country))
                .Select(c => CountryNormalizer.Normalize(c.Country!))
                .ToList();

            var rawData = session.Details.Select(d => new ExcelRowData
            {
                NoPO = d.OriginalPO,
                Country = d.Country,
                Model = d.Model,
                Qty = d.OriginalQty,
                RowIndex = d.RowIndex
            }).ToList();

            var dataByCountry = rawData
                .GroupBy(r => CountryNormalizer.NormalizeOrUnknown(r.Country))
                .ToDictionary(g => g.Key, g => g.ToList());

            var processedDataByCountry = new Dictionary<string, List<ProcessedPOData>>();
            foreach (var countryGroup in dataByCountry.Where(c => !submittedCountries.Contains(c.Key)))
            {
                var processedData = await CalculateProcessedDataAsync(countryGroup.Value, session.ShipmentType ?? "LOOSE");
                processedDataByCountry[countryGroup.Key] = processedData;
            }

            var allProcessedData = processedDataByCountry.Values.SelectMany(x => x).ToList();
            var pendingCountries = dataByCountry.Keys.Where(c => !submittedCountries.Contains(c)).ToList();

            return new UploadPreviewDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                RawData = rawData,
                ProcessedData = allProcessedData,
                ProcessedDataByCountry = processedDataByCountry,
                PendingCountries = pendingCountries,
                SubmittedCountries = submittedCountries,
                ShipmentType = session.ShipmentType,
                ShipmentDate = session.ShipmentDate
            };
        }

        public async Task<bool> UpdatePODataAsync(int poId, ProcessedPOData updatedData)
        {
            var poMaster = await _context.POMasters.FindAsync(poId);
            if (poMaster == null) return false;

            var modelConfigs = await _modelConfigService.GetAllAsync();
            var config = modelConfigs.FirstOrDefault(m => m.ModelName == updatedData.Model);

            if (config != null)
            {
                var tempData = new ProcessedPOData
                {
                    Model = updatedData.Model,
                    TotalQty = updatedData.TotalQty
                };

                var configDict = modelConfigs
                    .GroupBy(m => m.ModelName)
                    .ToDictionary(g => g.Key, g => g.ToList());
                CalculateBreakdown(tempData, configDict, poMaster.ShipmentMethod ?? "LOOSE");

                poMaster.QtyTotal = tempData.TotalQty;
                poMaster.QtyPallet = tempData.QtyPallet;
                poMaster.QtyBox = tempData.QtyBox;
                poMaster.QtyPcs = tempData.QtyPcs;
            }

            poMaster.Container = updatedData.Container;
            poMaster.NoInvoice = updatedData.NoInvoice;
            poMaster.ShipmentDetail = updatedData.ShipmentDetail;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<POMaster>> GetPOMastersBySessionAsync(int sessionId)
        {
            return await _context.POMasters
                .Where(p => p.SourceSessionId == sessionId)
                .ToListAsync();
        }

        public async Task<List<POSessionSummaryDto>> GetAllPOSessionsAsync()
        {
            var sessions = await _context.UploadSessions
                .Include(s => s.POMasters)
                .ThenInclude(p => p.Details)
                .Where(s => s.POMasters.Any())
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();

            return sessions.Select(session => 
            {
                var totalItemsToScan = session.POMasters.Sum(p => p.QtyBox + p.QtyPallet + (p.QtyPcs > 0 ? 1 : 0));
                var scannedItems = session.POMasters.SelectMany(p => p.Details).Count(d => d.ScannedDate != null);

                return new POSessionSummaryDto
                {
                    SessionId = session.SessionId,
                    FileName = session.FileName,
                    SheetName = session.SheetName,
                    ShipmentType = session.ShipmentType,
                    ShipmentDate = session.ShipmentDate,
                    Status = session.Status,
                    QRIdentity = session.IdentityQRCode,
                    CreatedDate = session.UploadDate,
                    CreatedBy = session.UploadedBy,
                    Country = session.POMasters.FirstOrDefault(p => !string.IsNullOrEmpty(p.Country))?.Country,
                    
                    TotalPOs = session.POMasters.Count,
                    TotalQty = session.POMasters.Sum(p => p.QtyTotal),
                    TotalBoxes = session.POMasters.Sum(p => p.QtyBox),
                    TotalPallets = session.POMasters.Sum(p => p.QtyPallet),
                    TotalPcs = session.POMasters.Sum(p => p.QtyPcs),
                    
                    TotalItemsToScan = totalItemsToScan,
                    ScannedItems = scannedItems,
                    
                    POs = session.POMasters.Select(p => new POMasterDto
                    {
                        POId = p.POId,
                        NoPO = p.NoPO,
                        ModelProduk = p.ModelProduk,
                        QtyTotal = p.QtyTotal,
                        QtyPallet = p.QtyPallet,
                        QtyBox = p.QtyBox,
                        QtyPcs = p.QtyPcs,
                        Container = p.Container,
                        NoInvoice = p.NoInvoice,
                        ShipmentDetail = p.ShipmentDetail,
                        Status = p.Status,
                        ShipmentMethod = p.ShipmentMethod,
                        CreatedDate = p.CreatedDate,
                        CreatedBy = p.CreatedBy,
                        Country = p.Country,
                        SourceSessionId = p.SourceSessionId,
                        FileName = session.FileName,
                        QRIdentity = session.IdentityQRCode
                    }).ToList()
                };
            }).ToList();
        }

        public async Task<UploadSession?> GetExistingSessionByHashAsync(string fileHash)
        {
            try
            {
                return await _context.UploadSessions
                    .Include(s => s.ChildSessions)
                    .Include(s => s.Details)
                    .Where(s => s.FileHash == fileHash && 
                               s.Status != "DELETED" && 
                               s.ParentSessionId == null)
                    .OrderByDescending(s => s.UploadDate)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<string>> GetRemainingCountriesAsync(int sessionId)
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.Details)
                    .Include(s => s.ChildSessions)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null) return new List<string>();

                var allCountries = session.Details
                    .Where(d => !string.IsNullOrEmpty(d.Country))
                    .Select(d => CountryNormalizer.Normalize(d.Country!))
                    .Distinct()
                    .ToList();

                var submittedCountries = session.ChildSessions
                    .Where(c => c.Status == "PROCESSED" && !string.IsNullOrEmpty(c.Country))
                    .Select(c => CountryNormalizer.Normalize(c.Country!))
                    .ToList();

                return allCountries.Except(submittedCountries).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<bool> HasAnySubmittedCountriesAsync(int sessionId)
        {
            try
            {
                return await _context.UploadSessions
                    .AnyAsync(s => s.ParentSessionId == sessionId && 
                                  s.Status == "PROCESSED" && 
                                  !string.IsNullOrEmpty(s.Country));
            }
            catch
            {
                return false;
            }
        }

        public async Task<FileUploadResult> ProcessFileUploadAsync(IFormFile file, string uploadedBy)
        {
            try
            {
                var fileHash = await GenerateFileHashAsync(file);
                var existingSession = await GetExistingSessionByHashAsync(fileHash);
                
                if (existingSession != null)
                {
                    var remainingCountries = await GetRemainingCountriesAsync(existingSession.SessionId);
                    var hasSubmitted = await HasAnySubmittedCountriesAsync(existingSession.SessionId);
                    
                    var submittedCountries = existingSession.ChildSessions
                        .Where(c => c.Status == "PROCESSED" && !string.IsNullOrEmpty(c.Country))
                        .Select(c => c.Country!)
                        .ToList();

                    if (remainingCountries.Any())
                    {
                        var previewData = await GetPreviewAsync(existingSession.SessionId);
                        
                        return new FileUploadResult
                        {
                            IsExistingFile = true,
                            HasRemainingCountries = true,
                            AllCountriesCompleted = false,
                            SessionId = existingSession.SessionId,
                            FileName = existingSession.FileName,
                            RemainingCountries = remainingCountries,
                            SubmittedCountries = submittedCountries,
                            Message = hasSubmitted 
                                ? $"File sudah pernah diupload. Melanjutkan proses yang tersisa ({remainingCountries.Count} negara)."
                                : "File sudah pernah diupload. Melanjutkan dari awal.",
                            PreviewData = previewData
                        };
                    }
                    else
                    {
                        return new FileUploadResult
                        {
                            IsExistingFile = true,
                            HasRemainingCountries = false,
                            AllCountriesCompleted = true,
                            SessionId = existingSession.SessionId,
                            FileName = existingSession.FileName,
                            RemainingCountries = new List<string>(),
                            SubmittedCountries = submittedCountries,
                            Message = "File ini sudah selesai diproses. Semua negara telah disubmit."
                        };
                    }
                }
                else
                {
                    var previewData = await ProcessExcelFileAsync(file, uploadedBy);
                    
                    return new FileUploadResult
                    {
                        IsExistingFile = false,
                        HasRemainingCountries = true,
                        AllCountriesCompleted = false,
                        SessionId = previewData.SessionId,
                        FileName = previewData.FileName,
                        RemainingCountries = previewData.PendingCountries,
                        SubmittedCountries = new List<string>(),
                        Message = "File berhasil diproses. Silakan review data sebelum submit.",
                        PreviewData = previewData
                    };
                }
            }
            catch (Exception ex)
            {
                return new FileUploadResult
                {
                    IsExistingFile = false,
                    HasRemainingCountries = false,
                    AllCountriesCompleted = false,
                    SessionId = 0,
                    FileName = file.FileName,
                    Message = $"Error processing file: {ex.Message}"
                };
            }
        }

        public async Task<List<UploadSession>> GetAllUploadSessionsAsync()
        {
            return await _context.UploadSessions
                .Include(s => s.POMasters)
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();
        }

        public async Task<List<UploadSession>> GetActiveSessionsAsync()
        {
            return await _context.UploadSessions
                .Include(s => s.POMasters)
                .Where(s => s.Status == "IN_PROGRESS" || s.Status == "VALIDATED" || s.Status == "QR_GENERATED")
                .OrderByDescending(s => s.UploadDate)
                .ToListAsync();
        }

        private string GenerateSheetIdentifier()
        {
            return $"SH{DateTime.Now:yyyyMMddHHmmss}";
        }

        private async Task<string> GenerateFileHashAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToBase64String(hashBytes);
        }
    }
}