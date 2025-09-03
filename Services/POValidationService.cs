using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services
{
    /// <summary>
    /// Implementation of PO and Session validation service
    /// Zero-failure validation for hierarchical lock system
    /// </summary>
    public class POValidationService : IPOValidationService
    {
        private readonly AppDbContext _context;
        private static readonly string[] ItemTypeProgression = { "BOX", "PALLET", "PCS" };

        public POValidationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result<SessionValidationDto>> ValidateSessionStateAsync(int sessionId)
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return Result<SessionValidationDto>.Failure("Session not found");
                }

                // Get master barcode for QR identity
                var masterBarcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                            b.BarcodeType == "MASTER" && 
                                            b.IsActive);

                // Calculate completion statistics
                var completedPOs = session.POMasters.Count(po => po.Status == "COMPLETED");
                var totalPOs = session.POMasters.Count;
                var progressPercentage = totalPOs > 0 ? (double)completedPOs / totalPOs * 100 : 0;

                var result = new SessionValidationDto
                {
                    SessionId = sessionId,
                    Status = session.Status,
                    IsActive = session.Status != "SCAN_COMPLETED",
                    CanScan = session.Status != "SCAN_COMPLETED" && masterBarcode != null,
                    CompletionReason = session.Status == "SCAN_COMPLETED" ? "All POs completed" : null,
                    CompletedAt = session.Status == "SCAN_COMPLETED" ? DateTime.Now : null,
                    QRIdentity = masterBarcode?.BarcodeValue ?? session.IdentityQRCode ?? string.Empty,
                    TotalPOs = totalPOs,
                    CompletedPOs = completedPOs,
                    ProgressPercentage = progressPercentage
                };

                return Result<SessionValidationDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<SessionValidationDto>.Failure($"Error validating session state: {ex.Message}");
            }
        }

        public async Task<Result<POValidationDto>> ValidatePOStateAsync(int poId)
        {
            try
            {
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                {
                    return Result<POValidationDto>.Failure("PO not found");
                }

                // Calculate progress for this PO
                var progress = await CalculatePOProgressAsync(poId);
                var nextItemType = await GetNextAvailableItemTypeAsync(poId);

                var result = new POValidationDto
                {
                    POId = poId,
                    NoPO = po.NoPO,
                    ModelProduct = po.ModelProduk,
                    IsCompleted = progress.IsAllComplete,
                    CanContinueScanning = !progress.IsAllComplete,
                    NextAvailableItemType = nextItemType.IsSuccess ? nextItemType.Value : null,
                    Progress = progress,
                    Status = po.Status
                };

                return Result<POValidationDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<POValidationDto>.Failure($"Error validating PO state: {ex.Message}");
            }
        }

        public async Task<Result<List<POAvailabilityDto>>> GetAvailablePOsAsync(int sessionId)
        {
            try
            {
                var pos = await _context.POMasters
                    .Where(p => p.SourceSessionId == sessionId)
                    .OrderBy(p => p.POId)
                    .ToListAsync();

                var availabilityList = new List<POAvailabilityDto>();

                foreach (var po in pos)
                {
                    var progress = await CalculatePOProgressAsync(po.POId);
                    var nextItemType = await GetNextAvailableItemTypeAsync(po.POId);
                    var availableItemTypes = GetAvailableItemTypes(po, progress);

                    var availability = new POAvailabilityDto
                    {
                        POId = po.POId,
                        NoPO = po.NoPO,
                        ModelProduct = po.ModelProduk,
                        Status = po.Status,
                        IsAvailable = !progress.IsAllComplete && availableItemTypes.Any(),
                        UnavailableReason = progress.IsAllComplete ? "PO completed" : 
                                          !availableItemTypes.Any() ? "No scannable items" : null,
                        Progress = progress,
                        AvailableItemTypes = availableItemTypes,
                        NextItemType = nextItemType.IsSuccess ? nextItemType.Value : null
                    };

                    availabilityList.Add(availability);
                }

                return Result<List<POAvailabilityDto>>.Success(availabilityList);
            }
            catch (Exception ex)
            {
                return Result<List<POAvailabilityDto>>.Failure($"Error getting available POs: {ex.Message}");
            }
        }

        public async Task<Result<int?>> ResolvePOFromBarcodeAsync(string barcode, int sessionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    return Result<int?>.Failure("Barcode cannot be empty");
                }

                // Strategy 1: BOX barcode - direct lookup from BarcodeRegistries
                var barcodeRegistry = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.BarcodeValue == barcode && 
                                            b.SessionId == sessionId && 
                                            b.BarcodeType == "BOX" && 
                                            b.IsActive);

                if (barcodeRegistry?.POId != null)
                {
                    return Result<int?>.Success(barcodeRegistry.POId);
                }

                // Strategy 2: PALLET/PCS - find available PO with that item type
                var itemType = DetermineItemType(barcode);
                if (itemType != "BOX")
                {
                    var availablePOs = await GetAvailablePOsAsync(sessionId);
                    if (availablePOs.IsSuccess && availablePOs.Value != null)
                    {
                        var suitablePO = availablePOs.Value
                            .Where(po => po.IsAvailable && po.AvailableItemTypes.Contains(itemType))
                            .OrderBy(po => po.POId)
                            .FirstOrDefault();

                        if (suitablePO != null)
                        {
                            return Result<int?>.Success(suitablePO.POId);
                        }
                    }
                }

                return Result<int?>.Success(null);
            }
            catch (Exception ex)
            {
                return Result<int?>.Failure($"Error resolving PO from barcode: {ex.Message}");
            }
        }

        public async Task<Result<string?>> GetNextAvailableItemTypeAsync(int poId)
        {
            try
            {
                var po = await _context.POMasters.FindAsync(poId);
                if (po == null)
                {
                    return Result<string?>.Failure("PO not found");
                }

                var progress = await CalculatePOProgressAsync(poId);

                // Return next incomplete item type in progression order
                foreach (var itemType in ItemTypeProgression)
                {
                    switch (itemType)
                    {
                        case "BOX" when po.QtyBox > 0 && !progress.IsBoxComplete:
                            return Result<string?>.Success("BOX");
                        case "PALLET" when po.QtyPallet > 0 && !progress.IsPalletComplete:
                            return Result<string?>.Success("PALLET");
                        case "PCS" when po.QtyPcs > 0 && !progress.IsPcsComplete:
                            return Result<string?>.Success("PCS");
                    }
                }

                return Result<string?>.Success(null); // PO completed
            }
            catch (Exception ex)
            {
                return Result<string?>.Failure($"Error getting next available item type: {ex.Message}");
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        private async Task<POProgressSummaryDto> CalculatePOProgressAsync(int poId)
        {
            var po = await _context.POMasters.FindAsync(poId);
            if (po == null)
            {
                return new POProgressSummaryDto();
            }

            // BOX progress from BarcodeRegistries
            var scannedBoxes = await _context.BarcodeRegistries
                .CountAsync(b => b.POId == poId && 
                                b.BarcodeType == "BOX" && 
                                b.Status == "SCANNED" && 
                                b.IsActive);

            // PALLET progress from ScanningActivities with POContext
            var scannedPallets = await _context.ScanningActivities
                .CountAsync(sa => sa.POContext == $"PO_{poId}" && 
                                 sa.Action == "SCAN_PALLET" && 
                                 sa.Result == "SUCCESS");

            // PCS progress from ScanningActivities with POContext
            var scannedPcs = await _context.ScanningActivities
                .CountAsync(sa => sa.POContext == $"PO_{poId}" && 
                                 sa.Action == "SCAN_PCS" && 
                                 sa.Result == "SUCCESS");

            return new POProgressSummaryDto
            {
                TotalBoxes = po.QtyBox,
                ScannedBoxes = scannedBoxes,
                TotalPallets = po.QtyPallet,
                ScannedPallets = scannedPallets,
                TotalPcs = po.QtyPcs,
                ScannedPcs = scannedPcs
            };
        }

        private static List<string> GetAvailableItemTypes(POMaster po, POProgressSummaryDto progress)
        {
            var availableTypes = new List<string>();

            // BOX: available if PO has boxes and not all scanned
            if (po.QtyBox > 0 && !progress.IsBoxComplete)
            {
                availableTypes.Add("BOX");
            }

            // PALLET: available if PO has pallets and not all scanned
            if (po.QtyPallet > 0 && !progress.IsPalletComplete)
            {
                availableTypes.Add("PALLET");
            }

            // PCS: available if PO has PCS and not all scanned
            if (po.QtyPcs > 0 && !progress.IsPcsComplete)
            {
                availableTypes.Add("PCS");
            }

            return availableTypes;
        }

        private static string DetermineItemType(string barcode)
        {
            var upperBarcode = barcode.ToUpperInvariant();

            if (upperBarcode.Contains("PALLET"))
                return "PALLET";

            if (upperBarcode.StartsWith("%Q"))
                return "PCS";

            if (upperBarcode.Contains("BOX"))
                return "BOX";

            return "UNKNOWN";
        }
    }
}
