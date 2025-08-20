using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Common;
using Microsoft.AspNetCore.SignalR;
using ShipmentFinishGood.Hubs;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services
{
    public class SmartCalculationEngine : ISmartCalculationEngine
    {
        private readonly AppDbContext _context;
        private readonly IModelConfigurationService _modelConfigService;
        private readonly ISmartBarcodeManager _barcodeManager;
        private readonly IScanningProtectionService _protectionService;
        private readonly IHubContext<ProgressHub> _hubContext;

        public SmartCalculationEngine(
            AppDbContext context,
            IModelConfigurationService modelConfigService,
            ISmartBarcodeManager barcodeManager,
            IScanningProtectionService protectionService,
            IHubContext<ProgressHub> hubContext)
        {
            _context = context;
            _modelConfigService = modelConfigService;
            _barcodeManager = barcodeManager;
            _protectionService = protectionService;
            _hubContext = hubContext;
        }

        public async Task<SmartUpdateResult> ExecuteSmartUpdateAsync(
            int sessionId, 
            int rowIndex, 
            SmartUpdateRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 🧠 STEP 1: BUILD INTELLIGENT CONTEXT
                var context = await BuildCalculationContext(sessionId, rowIndex);
                if (!context.IsValid)
                {
                    return SmartUpdateResult.Failure("Invalid session or row context");
                }

                // 🎯 STEP 2: SMART CALCULATION
                var calculationResult = await PerformSmartCalculation(context, request.NewTotalQty);
                
                // 🛡️ STEP 3: SCANNING PROTECTION CHECK
                var protectionResult = await _protectionService.CheckScanningConflicts(
                    context.QRIdentity, context.ModelName, calculationResult.OldBoxCount, calculationResult.NewBoxCount);
                
                if (protectionResult.HasConflict)
                {
                    return SmartUpdateResult.Failure($"Cannot update: {protectionResult.ConflictMessage}");
                }

                // 🤖 STEP 4: INTELLIGENT BARCODE MANAGEMENT
                var barcodeResult = await _barcodeManager.ManageBarcodeChanges(
                    context.QRIdentity,
                    context.ModelName, 
                    calculationResult.OldBoxCount,
                    calculationResult.NewBoxCount,
                    request.OperatorId);

                // 📊 STEP 5: UPDATE DATABASE
                await UpdatePOMasterData(context, request, calculationResult);
                
                // 💾 STEP 6: COMMIT TRANSACTION
                await transaction.CommitAsync();

                // 📱 STEP 7: REAL-TIME NOTIFICATION
                await SendRealTimeUpdate(sessionId, context, calculationResult, barcodeResult);

                return SmartUpdateResult.Success(
                    CreateUpdatedRowData(context, calculationResult),
                    barcodeResult,
                    BuildSmartNotifications(calculationResult, barcodeResult)
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return SmartUpdateResult.Failure($"Smart Engine Error: {ex.Message}");
            }
        }

        private async Task<SmartCalculationContext> BuildCalculationContext(int sessionId, int rowIndex)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null) 
                return SmartCalculationContext.Invalid("Session not found");

            var poMasters = session.POMasters.OrderBy(p => p.NoPO).ThenBy(p => p.ModelProduk).ToList();
            if (rowIndex >= poMasters.Count)
                return SmartCalculationContext.Invalid("Row index out of range");

            var poMaster = poMasters[rowIndex];
            
            return new SmartCalculationContext
            {
                IsValid = true,
                SessionId = sessionId,
                Session = session,
                POMaster = poMaster,
                RowIndex = rowIndex,
                QRIdentity = session.IdentityQRCode ?? "",
                ModelName = poMaster.ModelProduk,
                ShipmentType = session.ShipmentType ?? "LOOSE",
                CurrentTotalQty = poMaster.QtyTotal,
                CurrentBoxCount = poMaster.QtyBox
            };
        }

        private async Task<SmartCalculationResult> PerformSmartCalculation(
            SmartCalculationContext context, 
            int newTotalQty)
        {
            // 🧠 GET MODEL CONFIGURATION
            var modelConfigs = await _modelConfigService.GetAllAsync();
            var configDict = modelConfigs
                .GroupBy(m => m.ModelName)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new SmartCalculationResult
            {
                OldTotalQty = context.CurrentTotalQty,
                NewTotalQty = newTotalQty,
                OldBoxCount = context.CurrentBoxCount
            };

            if (!configDict.TryGetValue(context.ModelName, out var configs) || configs.Count == 0)
            {
                // 📦 NO CONFIG: KEEP AS PIECES
                result.NewQtyPcs = newTotalQty;
                result.NewQtyBox = 0;
                result.NewQtyPallet = 0;
                result.CalculationStrategy = "NO_CONFIG_PIECES_ONLY";
            }
            else
            {
                // 🎯 SMART CONFIG-BASED CALCULATION
                var configForType = configs.FirstOrDefault(c => 
                    string.Equals(c.Type, context.ShipmentType, StringComparison.OrdinalIgnoreCase))
                    ?? configs.First();

                var palletConfig = configs.FirstOrDefault(c => 
                    string.Equals(c.Type, "PALLET", StringComparison.OrdinalIgnoreCase));
                
                var pcsPerPallet = string.Equals(context.ShipmentType, "PALLET", StringComparison.OrdinalIgnoreCase)
                    ? (palletConfig?.PcsPerPallet ?? configForType.PcsPerPallet)
                    : configForType.PcsPerPallet;

                var pcsPerBox = configForType.PcsPerBox;
                var remainingQty = newTotalQty;

                // 🧮 HIERARCHICAL BREAKDOWN
                if (pcsPerPallet > 0)
                {
                    result.NewQtyPallet = remainingQty / pcsPerPallet;
                    remainingQty = remainingQty % pcsPerPallet;
                }

                if (pcsPerBox > 0)
                {
                    result.NewQtyBox = remainingQty / pcsPerBox;
                    remainingQty = remainingQty % pcsPerBox;
                }

                result.NewQtyPcs = remainingQty;
                result.CalculationStrategy = $"CONFIG_BASED_{context.ShipmentType}";
                result.UsedConfiguration = configForType;
            }

            result.BoxCountChanged = result.NewQtyBox != result.OldBoxCount;
            result.BoxCountDifference = result.NewQtyBox - result.OldBoxCount;

            return result;
        }

        private async Task UpdatePOMasterData(
            SmartCalculationContext context, 
            SmartUpdateRequest request,
            SmartCalculationResult calculationResult)
        {
            var poMaster = context.POMaster;
            
            // 📊 UPDATE QUANTITIES
            poMaster.QtyTotal = calculationResult.NewTotalQty;
            poMaster.QtyPallet = calculationResult.NewQtyPallet;
            poMaster.QtyBox = calculationResult.NewQtyBox;
            poMaster.QtyPcs = calculationResult.NewQtyPcs;
            
            // 📝 UPDATE METADATA
            if (!string.IsNullOrEmpty(request.Container))
                poMaster.Container = request.Container;
            if (!string.IsNullOrEmpty(request.NoInvoice))
                poMaster.NoInvoice = request.NoInvoice;
            if (!string.IsNullOrEmpty(request.ShipmentDetail))
                poMaster.ShipmentDetail = request.ShipmentDetail;

            await _context.SaveChangesAsync();
        }

        private async Task SendRealTimeUpdate(
            int sessionId, 
            SmartCalculationContext context,
            SmartCalculationResult calculationResult,
            BarcodeChangeResult barcodeResult)
        {
            var updateData = new
            {
                RowIndex = context.RowIndex,
                UpdatedBreakdown = new
                {
                    QtyPallet = calculationResult.NewQtyPallet,
                    QtyBox = calculationResult.NewQtyBox,
                    QtyPcs = calculationResult.NewQtyPcs
                },
                BarcodeChanges = new
                {
                    Added = barcodeResult.NewBarcodes?.Count ?? 0,
                    Removed = barcodeResult.RemovedBarcodes?.Count ?? 0
                },
                Timestamp = DateTime.Now
            };

            await _hubContext.Clients.Group($"Session_{sessionId}")
                .SendAsync("SmartUpdateComplete", updateData);
        }

        private FinalRowData CreateUpdatedRowData(
            SmartCalculationContext context,
            SmartCalculationResult calculationResult)
        {
            return new FinalRowData
            {
                RowIndex = context.RowIndex,
                NoPO = context.POMaster.NoPO,
                Model = context.POMaster.ModelProduk,
                TotalQty = calculationResult.NewTotalQty,
                QtyPallet = calculationResult.NewQtyPallet,
                QtyBox = calculationResult.NewQtyBox,
                QtyPcs = calculationResult.NewQtyPcs,
                Container = context.POMaster.Container,
                NoInvoice = context.POMaster.NoInvoice,
                ShipmentDetail = context.POMaster.ShipmentDetail,
                IsEditable = context.Session.Status == "PROCESSED"
            };
        }

        private List<string> BuildSmartNotifications(
            SmartCalculationResult calculationResult,
            BarcodeChangeResult barcodeResult)
        {
            var notifications = new List<string>
            {
                $"✅ Smart calculation complete using {calculationResult.CalculationStrategy}"
            };

            if (calculationResult.BoxCountChanged)
            {
                if (calculationResult.BoxCountDifference > 0)
                {
                    notifications.Add($"📦 Added {calculationResult.BoxCountDifference} boxes - {barcodeResult.NewBarcodes?.Count ?? 0} new barcodes generated");
                }
                else
                {
                    notifications.Add($"📦 Removed {Math.Abs(calculationResult.BoxCountDifference)} boxes - {barcodeResult.RemovedBarcodes?.Count ?? 0} barcodes deleted (highest numbers)");
                }
            }

            return notifications;
        }
    }
}
