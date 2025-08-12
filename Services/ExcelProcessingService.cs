using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services
{
    public class ExcelProcessingService : IExcelProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IModelConfigurationService _modelConfigService;

        public ExcelProcessingService(AppDbContext context, IModelConfigurationService modelConfigService)
        {
            _context = context;
            _modelConfigService = modelConfigService;
        }

        public async Task<UploadPreviewDto> ProcessExcelFileAsync(IFormFile file, string uploadedBy)
        {
            var rawData = ParseExcelFileAsync(file);
            var processedDataLoose = await CalculateProcessedDataAsync(rawData, "LOOSE");

            var session = new UploadSession
            {
                FileName = file.FileName,
                SheetName = "Sheet1",
                Status = "PREVIEW",
                UploadDate = DateTime.Now,
                UploadedBy = uploadedBy,
                SheetIdentifier = GenerateSheetIdentifier()
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
                    RowIndex = row.RowIndex
                };
                _context.UploadSessionDetails.Add(detail);
            }
            await _context.SaveChangesAsync();

            return new UploadPreviewDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                RawData = rawData,
                ProcessedData = processedDataLoose
            };
        }

        private List<ExcelRowData> ParseExcelFileAsync(IFormFile file)
        {
            var rawData = new List<ExcelRowData>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var currentPO = string.Empty;

            for (int row = 2; row <= lastRowUsed; row++)
            {
                var noPOCell = worksheet.Cell(row, 1).GetString().Trim();
                var modelCell = worksheet.Cell(row, 2).GetString().Trim();
                var qtyCell = worksheet.Cell(row, 3).GetString().Trim();

                if (string.IsNullOrEmpty(modelCell) && string.IsNullOrEmpty(qtyCell))
                    continue;

                if (!string.IsNullOrEmpty(noPOCell))
                {
                    currentPO = noPOCell;
                }

                if (!int.TryParse(qtyCell.Replace(".", "").Replace(",", ""), out int qty))
                {
                    if (decimal.TryParse(qtyCell, out decimal decimalQty))
                    {
                        qty = (int)Math.Round(decimalQty);
                    }
                    else
                    {
                        continue;
                    }
                }

                rawData.Add(new ExcelRowData
                {
                    NoPO = currentPO,
                    Model = modelCell,
                    Qty = qty,
                    RowIndex = row
                });
            }

            return rawData;
        }

        public async Task<List<ProcessedPOData>> CalculateProcessedDataAsync(List<ExcelRowData> rawData, string shipmentType)
        {
            var modelConfigs = await _modelConfigService.GetAllAsync();
            // Group by model name because we can have both LOOSE and PALLET configs with the same model name
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

        private void CalculateBreakdown(ProcessedPOData item, Dictionary<string, List<ModelConfiguration>> configDict, string shipmentType)
        {
            if (!configDict.TryGetValue(item.Model, out var configs) || configs.Count == 0)
            {
                // No config found for this model: keep everything as loose pieces
                item.QtyPcs = item.TotalQty;
                return;
            }

            // Prefer config matching the shipment type; fall back to any available
            var configForType = configs.FirstOrDefault(c => string.Equals(c.Type, shipmentType, StringComparison.OrdinalIgnoreCase))
                               ?? configs.First();

            // For pallet calculation, try to use PALLET config when available
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
                    CreatedBy = createdBy
                };

                _context.POMasters.Add(poMaster);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UploadPreviewDto?> GetPreviewAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return null;

            var rawData = session.Details.Select(d => new ExcelRowData
            {
                NoPO = d.OriginalPO,
                Model = d.Model,
                Qty = d.OriginalQty,
                RowIndex = d.RowIndex
            }).ToList();

            var processedData = await CalculateProcessedDataAsync(rawData, session.ShipmentType ?? "LOOSE");

            return new UploadPreviewDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                RawData = rawData,
                ProcessedData = processedData,
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
                .Where(s => s.POMasters.Any()) // Only sessions with processed POs
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
                    
                    // Summary data
                    TotalPOs = session.POMasters.Count,
                    TotalQty = session.POMasters.Sum(p => p.QtyTotal),
                    TotalBoxes = session.POMasters.Sum(p => p.QtyBox),
                    TotalPallets = session.POMasters.Sum(p => p.QtyPallet),
                    
                    // Progress tracking
                    TotalItemsToScan = totalItemsToScan,
                    ScannedItems = scannedItems,
                    
                    // Individual POs
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
                        SourceSessionId = p.SourceSessionId,
                        FileName = session.FileName,
                        QRIdentity = session.IdentityQRCode
                    }).ToList()
                };
            }).ToList();
        }

        private string GenerateSheetIdentifier()
        {
            return $"SH{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}
