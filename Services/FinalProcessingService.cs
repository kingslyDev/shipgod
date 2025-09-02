using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Data;

namespace ShipmentFinishGood.Services
{
    public class FinalProcessingService : IFinalProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IModelConfigurationService _modelConfigService;
        private readonly ISmartCalculationEngine _smartEngine;

        public FinalProcessingService(
            AppDbContext context, 
            IModelConfigurationService modelConfigService,
            ISmartCalculationEngine smartEngine)
        {
            _context = context;
            _modelConfigService = modelConfigService;
            _smartEngine = smartEngine;
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
            
            // Check for QR Identity duplicate
            var duplicateQR = await _context.UploadSessions
                .Where(s => s.IdentityQRCode == qrIdentity && 
                           s.SessionId != sessionId &&
                           s.Status != "DELETED")
                .FirstOrDefaultAsync();

            if (duplicateQR != null)
            {
                // Add milliseconds to make it unique
                var timestampWithMs = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                qrIdentity = $"QR_{session.SheetIdentifier}_{session.ShipmentType}_{timestampWithMs}";
                
                // Double check uniqueness
                var duplicateQR2 = await _context.UploadSessions
                    .Where(s => s.IdentityQRCode == qrIdentity && s.SessionId != sessionId)
                    .FirstOrDefaultAsync();
                    
                if (duplicateQR2 != null)
                {
                    throw new InvalidOperationException($"Unable to generate unique QR Identity. Please try again.");
                }
            }
            
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
                    var barcode = $"QR_{session.SessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{i:D3}";
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

        // 🧠 SMART AUTO-CALCULATION ENGINE INTEGRATION
        public async Task<SmartUpdateResult> SmartUpdateRowAsync(int sessionId, int rowIndex, SmartUpdateRequest request)
        {
            return await _smartEngine.ExecuteSmartUpdateAsync(sessionId, rowIndex, request);
        }

        public async Task<Result<FinalRowData>> AddFinalRowAsync(int sessionId, FinalRowUpdateRequest request, string createdBy)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return Result<FinalRowData>.Failure("Session not found");

            // Extract PO and Model from NoInvoice and Container (UI will pass these)
            // In JavaScript, we'll pass noPO via Container field and model via ShipmentDetail field temporarily
            var noPO = request.Container ?? $"PO_{DateTime.Now:yyyyMMdd_HHmmss}";
            var modelProduk = request.ShipmentDetail ?? "NEW_MODEL";
            
            // Create new PO Master
            var newPOMaster = new Models.POMaster
            {
                NoPO = noPO,
                ModelProduk = modelProduk,
                QtyTotal = request.TotalQty,
                Container = request.Container,
                NoInvoice = request.NoInvoice,
                ShipmentDetail = request.ShipmentDetail,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            // Recalculate breakdown
            await RecalculateBreakdownAsync(newPOMaster, session.ShipmentType ?? "LOOSE");

            session.POMasters.Add(newPOMaster);
            await _context.SaveChangesAsync(); // Save first to get POId

            // Generate barcode (smartbarcode pattern) if QR already exists
            if (!string.IsNullOrEmpty(session.IdentityQRCode))
            {
                var qrIdentity = session.IdentityQRCode;
                var barcodeList = new List<string>();
                for (int i = 1; i <= newPOMaster.QtyBox; i++)
                {
                    var barcode = $"QR_{session.SessionId}_{newPOMaster.NoPO}_{newPOMaster.ModelProduk}_BOX_{i:D3}";
                    barcodeList.Add(barcode);
                    
                    // Save to BarcodeRegistry
                    var barcodeRegistry = new Models.BarcodeRegistry
                    {
                        BarcodeValue = barcode,
                        BarcodeType = "BOX",
                        SessionId = sessionId,
                        POId = newPOMaster.POId,
                        ModelProduct = newPOMaster.ModelProduk,
                        BoxNumber = i,
                        GeneratedDate = DateTime.Now,
                        GeneratedBy = createdBy,
                        Status = "GENERATED"
                    };
                    _context.BarcodeRegistries.Add(barcodeRegistry);
                }
                
                // Update total boxes in session
                session.TotalBoxes = session.POMasters.Sum(p => p.QtyBox);
                
                try
                {
                    await _context.SaveChangesAsync(); // Save barcode registries
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) 
                when (dbEx.InnerException?.Message?.Contains("duplicate key") == true)
                {
                    // If duplicate key error occurs during final processing, log and continue
                    // This might happen if the barcode was already generated by another process
                    Console.WriteLine($"Warning: Duplicate barcode detected during final processing: {dbEx.InnerException.Message}");
                    
                    // Try to save again after clearing duplicates from context
                    var duplicateBarcodeValue = ExtractDuplicateBarcodeValue(dbEx.InnerException.Message);
                    if (!string.IsNullOrEmpty(duplicateBarcodeValue))
                    {
                        // Remove duplicate entries from change tracker
                        var duplicateEntries = _context.ChangeTracker.Entries<Models.BarcodeRegistry>()
                            .Where(e => e.Entity.BarcodeValue == duplicateBarcodeValue && e.State == EntityState.Added)
                            .ToList();
                        
                        foreach (var entry in duplicateEntries)
                        {
                            entry.State = EntityState.Detached;
                        }
                        
                        if (_context.ChangeTracker.HasChanges())
                        {
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        throw; // Re-throw if we can't handle it
                    }
                }
            }

            var rowIndex = session.POMasters.Count - 1;
            var newRow = new DTOs.FinalRowData
            {
                RowIndex = rowIndex,
                NoPO = newPOMaster.NoPO,
                Model = newPOMaster.ModelProduk,
                TotalQty = newPOMaster.QtyTotal,
                QtyPallet = newPOMaster.QtyPallet,
                QtyBox = newPOMaster.QtyBox,
                QtyPcs = newPOMaster.QtyPcs,
                Container = newPOMaster.Container,
                NoInvoice = newPOMaster.NoInvoice,
                ShipmentDetail = newPOMaster.ShipmentDetail,
                IsEditable = session.Status == "PROCESSED"
            };

            return Result<FinalRowData>.Success(newRow);
        }        private async Task RecalculateBreakdownAsync(POMaster poMaster, string shipmentType)
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

        private static string ExtractDuplicateBarcodeValue(string errorMessage)
        {
            try
            {
                // Extract barcode value from error message like:
                // "The duplicate key value is (QR_2_20250827082413_BOX_RF-2400DEB-K_001)."
                var startIndex = errorMessage.IndexOf("(");
                var endIndex = errorMessage.IndexOf(")");
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    return errorMessage.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
