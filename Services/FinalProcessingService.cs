using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services
{
    public class FinalProcessingService : IFinalProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IModelConfigurationService _modelConfigService;

        public FinalProcessingService(AppDbContext context, IModelConfigurationService modelConfigService)
        {
            _context = context;
            _modelConfigService = modelConfigService;
        }

        public async Task<FinalTableDto?> GetFinalDataAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return null;

            var finalRows = new List<FinalRowData>();
            int rowIndex = 0;

            foreach (var poMaster in session.POMasters.OrderBy(p => p.NoPO).ThenBy(p => p.ModelProduk))
            {
                finalRows.Add(new FinalRowData
                {
                    RowIndex = rowIndex++,
                    NoPO = poMaster.NoPO,
                    Model = poMaster.ModelProduk,
                    TotalQty = poMaster.QtyTotal,
                    QtyPallet = poMaster.QtyPallet,
                    QtyBox = poMaster.QtyBox,
                    QtyPcs = poMaster.QtyPcs,
                    Container = poMaster.Container,
                    NoInvoice = poMaster.NoInvoice,
                    ShipmentDetail = poMaster.ShipmentDetail,
                    IsEditable = session.Status == "PROCESSED"
                });
            }

            return new FinalTableDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                ShipmentDate = session.ShipmentDate,
                Status = session.Status,
                FinalData = finalRows,
                QRIdentity = session.IdentityQRCode,
                HasQRGenerated = !string.IsNullOrEmpty(session.IdentityQRCode)
            };
        }

        public async Task<Result<FinalRowData>> UpdateFinalRowAsync(int sessionId, int rowIndex, FinalRowUpdateRequest request)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return Result<FinalRowData>.Failure("Session not found");

            var poMasters = session.POMasters.OrderBy(p => p.NoPO).ThenBy(p => p.ModelProduk).ToList();
            if (rowIndex >= poMasters.Count)
                return Result<FinalRowData>.Failure("Row index out of range");

            var poMaster = poMasters[rowIndex];
            
            // Update quantities and recalculate breakdown
            poMaster.QtyTotal = request.TotalQty;
            poMaster.Container = request.Container;
            poMaster.NoInvoice = request.NoInvoice;
            poMaster.ShipmentDetail = request.ShipmentDetail;

            // Recalculate breakdown based on model configuration
            await RecalculateBreakdownAsync(poMaster, session.ShipmentType ?? "LOOSE");

            await _context.SaveChangesAsync();

            var updatedRow = new FinalRowData
            {
                RowIndex = rowIndex,
                NoPO = poMaster.NoPO,
                Model = poMaster.ModelProduk,
                TotalQty = poMaster.QtyTotal,
                QtyPallet = poMaster.QtyPallet,
                QtyBox = poMaster.QtyBox,
                QtyPcs = poMaster.QtyPcs,
                Container = poMaster.Container,
                NoInvoice = poMaster.NoInvoice,
                ShipmentDetail = poMaster.ShipmentDetail
            };

            return Result<FinalRowData>.Success(updatedRow);
        }

        public async Task<bool> SaveMetadataAsync(int sessionId, List<FinalRowMetadata> metadata, string updatedBy)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return false;

            var poMasters = session.POMasters.OrderBy(p => p.NoPO).ThenBy(p => p.ModelProduk).ToList();

            foreach (var meta in metadata)
            {
                if (meta.RowIndex < poMasters.Count)
                {
                    var poMaster = poMasters[meta.RowIndex];
                    poMaster.Container = meta.Container;
                    poMaster.NoInvoice = meta.NoInvoice;
                    poMaster.ShipmentDetail = meta.ShipmentDetail;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<QRManagementDto?> GenerateQRIdentityAsync(int sessionId, string generatedBy)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) return null;

            // Generate QR Identity
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var qrIdentity = $"QR_{session.SheetIdentifier}_{session.ShipmentType}_{timestamp}";
            
            session.IdentityQRCode = qrIdentity;
            session.Status = "QR_GENERATED";

            // Calculate total boxes
            var totalBoxes = session.POMasters.Sum(p => p.QtyBox);
            session.TotalBoxes = totalBoxes;

            await _context.SaveChangesAsync();

            // Generate barcode list for boxes
            var barcodeList = new List<string>();
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    var barcode = $"{qrIdentity}_BOX_{poMaster.ModelProduk}_{i:D3}";
                    barcodeList.Add(barcode);
                }
            }

            return new QRManagementDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                QRIdentity = qrIdentity,
                Status = "Ready for Scanning",
                GeneratedDate = DateTime.Now,
                GeneratedBy = generatedBy,
                TotalBoxes = totalBoxes,
                BarcodeList = barcodeList,
                CanRegenerate = true
            };
        }

        private async Task RecalculateBreakdownAsync(POMaster poMaster, string shipmentType)
        {
            var modelConfigs = await _modelConfigService.GetAllAsync();
            var configDict = modelConfigs
                .GroupBy(m => m.ModelName)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (!configDict.TryGetValue(poMaster.ModelProduk, out var configs) || configs.Count == 0)
            {
                // No config found: keep everything as loose pieces
                poMaster.QtyPcs = poMaster.QtyTotal;
                poMaster.QtyPallet = 0;
                poMaster.QtyBox = 0;
                return;
            }

            // Prefer config matching the shipment type; fall back to any available
            var configForType = configs.FirstOrDefault(c => string.Equals(c.Type, shipmentType, StringComparison.OrdinalIgnoreCase))
                               ?? configs.First();

            var palletConfig = configs.FirstOrDefault(c => string.Equals(c.Type, "PALLET", StringComparison.OrdinalIgnoreCase));
            var pcsPerPallet = string.Equals(shipmentType, "PALLET", StringComparison.OrdinalIgnoreCase)
                ? (palletConfig?.PcsPerPallet ?? configForType.PcsPerPallet)
                : configForType.PcsPerPallet;

            var pcsPerBox = configForType.PcsPerBox;
            var remainingQty = poMaster.QtyTotal;

            if (pcsPerPallet > 0)
            {
                poMaster.QtyPallet = remainingQty / pcsPerPallet;
                remainingQty = remainingQty % pcsPerPallet;
            }

            if (pcsPerBox > 0)
            {
                poMaster.QtyBox = remainingQty / pcsPerBox;
                remainingQty = remainingQty % pcsPerBox;
            }

            poMaster.QtyPcs = remainingQty;
        }
    }
}
