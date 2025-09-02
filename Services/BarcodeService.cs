using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;
using ShipmentFinishGood.Common;

namespace ShipmentFinishGood.Services
{
    public class BarcodeService : IBarcodeService
    {
        private readonly AppDbContext _context;

        public BarcodeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Result> GenerateBarcodesForSessionAsync(int sessionId, string generatedBy)
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return Result.Failure("Session not found");

                if (string.IsNullOrEmpty(session.IdentityQRCode))
                    return Result.Failure("Session does not have QR Identity generated");

                // ✅ ROBUST: Cancel ALL existing barcodes first to prevent conflicts
                await _context.BarcodeRegistries
                    .Where(b => b.SessionId == sessionId && b.IsActive)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(x => x.Status, "CANCELLED")
                        .SetProperty(x => x.IsActive, false));
                
                // ✅ FORCE: Ensure transaction is committed before proceeding
                await _context.SaveChangesAsync();

                // ✅ VALIDATION: Check POMasters data integrity first
                var poMastersWithIssues = session.POMasters.Where(p => p.QtyBox <= 0).ToList();
                if (poMastersWithIssues.Any())
                {
                    var issueDetails = string.Join(", ", poMastersWithIssues.Select(p => $"POId {p.POId}: QtyBox={p.QtyBox}"));
                    return Result.Failure($"POMasters have invalid QtyBox values: {issueDetails}");
                }

                var barcodesToInsert = new List<BarcodeRegistry>();

                // ✅ MASTER QR: Always generate master first
                barcodesToInsert.Add(new BarcodeRegistry
                {
                    BarcodeValue = session.IdentityQRCode,
                    BarcodeType = "MASTER",
                    SessionId = sessionId,
                    Status = "GENERATED",
                    GeneratedBy = generatedBy,
                    GeneratedDate = DateTime.Now,
                    IsActive = true
                });

                // ✅ BOX BARCODES: Generate with full validation and logging
                var expectedTotalBoxes = session.POMasters.Sum(p => p.QtyBox);
                var actualBoxesGenerated = 0;

                foreach (var poMaster in session.POMasters.OrderBy(p => p.POId))
                {
                    // ✅ VALIDATION: Skip invalid POMasters
                    if (poMaster.QtyBox <= 0)
                    {
                        continue;
                    }

                    // ✅ SEQUENTIAL GENERATION: Ensure all boxes are generated
                    for (int boxNumber = 1; boxNumber <= poMaster.QtyBox; boxNumber++)
                    {
                        // 🆕 NEW FORMAT: QR_{SessionId}_{NoPO}_{Model}_BOX_{BoxNumber}
                        var barcode = $"QR_{sessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{boxNumber:D3}";
                        
                        // ✅ DUPLICATE CHECK: Prevent duplicates in memory
                        if (barcodesToInsert.Any(b => b.BarcodeValue == barcode))
                        {
                            // Make unique by adding timestamp suffix if somehow duplicate exists
                            barcode = $"QR_{sessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_{boxNumber:D3}_{DateTime.Now.Ticks}";
                        }

                        barcodesToInsert.Add(new BarcodeRegistry
                        {
                            BarcodeValue = barcode,
                            BarcodeType = "BOX",
                            SessionId = sessionId,
                            POId = poMaster.POId,
                            ModelProduct = poMaster.ModelProduk,
                            BoxNumber = boxNumber,
                            Status = "GENERATED",
                            GeneratedBy = generatedBy,
                            GeneratedDate = DateTime.Now,
                            IsActive = true
                        });

                        actualBoxesGenerated++;
                    }
                }

                // ✅ FINAL VALIDATION: Ensure we generated the correct count
                var expectedBoxBarcodes = expectedTotalBoxes;
                var actualBoxBarcodes = barcodesToInsert.Count(b => b.BarcodeType == "BOX");
                
                if (actualBoxBarcodes != expectedBoxBarcodes)
                {
                    return Result.Failure($"Barcode generation mismatch! Expected: {expectedBoxBarcodes}, Generated: {actualBoxBarcodes}");
                }

                // ✅ TRANSACTIONAL INSERT: Use transaction for atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Insert in batches for better performance and error handling
                    const int batchSize = 100;
                    for (int i = 0; i < barcodesToInsert.Count; i += batchSize)
                    {
                        var batch = barcodesToInsert.Skip(i).Take(batchSize).ToList();
                        await _context.BarcodeRegistries.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to insert barcodes in transaction: {ex.Message}", ex);
                }

                // ✅ POST-GENERATION VERIFICATION: Verify all barcodes were saved
                await Task.Delay(100); // Small delay to ensure DB consistency
                
                var savedCount = await _context.BarcodeRegistries
                    .CountAsync(b => b.SessionId == sessionId && b.IsActive);
                
                var expectedTotal = barcodesToInsert.Count;
                if (savedCount != expectedTotal)
                {
                    return Result.Failure($"❌ CRITICAL: Verification failed! Expected: {expectedTotal}, Saved: {savedCount}. Some barcodes may be missing!");
                }

                // ✅ DETAILED VERIFICATION: Check each PO has correct barcode count
                var verificationResults = await _context.POMasters
                    .Where(p => p.SourceSessionId == sessionId)
                    .Select(p => new
                    {
                        p.POId,
                        p.NoPO,
                        p.ModelProduk,
                        ExpectedBoxes = p.QtyBox,
                        ActualBarcodes = _context.BarcodeRegistries.Count(b => b.POId == p.POId && b.BarcodeType == "BOX" && b.IsActive)
                    })
                    .ToListAsync();

                var missingPOs = verificationResults.Where(v => v.ActualBarcodes != v.ExpectedBoxes).ToList();
                if (missingPOs.Any())
                {
                    var details = string.Join(", ", missingPOs.Select(m => $"PO {m.NoPO} ({m.ModelProduk}): Expected {m.ExpectedBoxes}, Got {m.ActualBarcodes}"));
                    return Result.Failure($"❌ CRITICAL: Missing barcodes detected! {details}");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"❌ ERROR generating barcodes: {ex.Message}");
            }
        }

        public async Task<Result> RegenerateBarcodesForPOAsync(int poId, string generatedBy)
        {
            try
            {
                var poMaster = await _context.POMasters
                    .Include(p => p.SourceSession)
                    .FirstOrDefaultAsync(p => p.POId == poId);

                if (poMaster?.SourceSession == null)
                    return Result.Failure("PO Master or Session not found");

                var session = poMaster.SourceSession;
                if (string.IsNullOrEmpty(session.IdentityQRCode))
                    return Result.Failure("Session does not have QR Identity");

                // Cancel existing barcodes for this PO
                await _context.BarcodeRegistries
                    .Where(b => b.POId == poId && b.IsActive)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(x => x.Status, "CANCELLED")
                        .SetProperty(x => x.IsActive, false));

                // Generate new barcodes with NEW FORMAT
                var barcodesToInsert = new List<BarcodeRegistry>();
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    // 🆕 NEW FORMAT: QR_{SessionId}_{NoPO}_{Model}_BOX_{BoxNumber}
                    var barcode = $"QR_{session.SessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{i:D3}";
                    barcodesToInsert.Add(new BarcodeRegistry
                    {
                        BarcodeValue = barcode,
                        BarcodeType = "BOX",
                        SessionId = session.SessionId,
                        POId = poMaster.POId,
                        ModelProduct = poMaster.ModelProduk,
                        BoxNumber = i,
                        Status = "GENERATED",
                        GeneratedBy = generatedBy,
                        GeneratedDate = DateTime.Now,
                        IsActive = true
                    });
                }

                await _context.BarcodeRegistries.AddRangeAsync(barcodesToInsert);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error regenerating barcodes for PO: {ex.Message}");
            }
        }

        public async Task<Result> UpdateBarcodeQuantityAsync(int poId, int newQtyBox, string updatedBy)
        {
            try
            {
                var poMaster = await _context.POMasters
                    .Include(p => p.SourceSession)
                    .FirstOrDefaultAsync(p => p.POId == poId);

                if (poMaster?.SourceSession == null)
                    return Result.Failure("PO Master or Session not found");

                var session = poMaster.SourceSession;
                if (string.IsNullOrEmpty(session.IdentityQRCode))
                    return Result.Failure("Session does not have QR Identity");

                var currentBarcodes = await _context.BarcodeRegistries
                    .Where(b => b.POId == poId && b.IsActive && b.BarcodeType == "BOX")
                    .OrderBy(b => b.BoxNumber)
                    .ToListAsync();

                var currentCount = currentBarcodes.Count;

                if (newQtyBox == currentCount)
                {
                    return Result.Success();
                }
                else if (newQtyBox > currentCount)
                {
                    // Add new barcodes
                    var barcodesToAdd = new List<BarcodeRegistry>();
                    for (int i = currentCount + 1; i <= newQtyBox; i++)
                    {
                        var barcode = $"QR_{session.SessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_BOX_{i:D3}";
                        barcodesToAdd.Add(new BarcodeRegistry
                        {
                            BarcodeValue = barcode,
                            BarcodeType = "BOX",
                            SessionId = session.SessionId,
                            POId = poMaster.POId,
                            ModelProduct = poMaster.ModelProduk,
                            BoxNumber = i,
                            Status = "GENERATED",
                            GeneratedBy = updatedBy,
                            GeneratedDate = DateTime.Now,
                            IsActive = true
                        });
                    }

                    await _context.BarcodeRegistries.AddRangeAsync(barcodesToAdd);
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) 
                    when (dbEx.InnerException?.Message?.Contains("duplicate key") == true)
                    {
                        // Handle potential duplicate key errors gracefully
                        var duplicateBarcodeValue = ExtractDuplicateBarcodeValue(dbEx.InnerException.Message);
                        if (!string.IsNullOrEmpty(duplicateBarcodeValue))
                        {
                            // Remove the duplicate and try again
                            barcodesToAdd.RemoveAll(b => b.BarcodeValue == duplicateBarcodeValue);
                            if (barcodesToAdd.Any())
                            {
                                // Clear the context and try again
                                _context.ChangeTracker.Clear();
                                await _context.BarcodeRegistries.AddRangeAsync(barcodesToAdd);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }

                    return Result.Success();
                }
                else // newQtyBox < currentCount
                {
                    // Cancel excess barcodes (prefer unscanned ones)
                    var excessCount = currentCount - newQtyBox;
                    var barcodesToCancel = currentBarcodes
                        .Where(b => b.Status == "GENERATED") // Prefer unscanned
                        .Take(excessCount)
                        .ToList();

                    // If not enough unscanned, take from scanned ones
                    if (barcodesToCancel.Count < excessCount)
                    {
                        var additionalNeeded = excessCount - barcodesToCancel.Count;
                        var scannedToCancel = currentBarcodes
                            .Where(b => b.Status == "SCANNED")
                            .Take(additionalNeeded);
                        barcodesToCancel.AddRange(scannedToCancel);
                    }

                    foreach (var barcode in barcodesToCancel)
                    {
                        barcode.Status = "CANCELLED";
                        barcode.IsActive = false;
                    }

                    await _context.SaveChangesAsync();

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error updating barcode quantity: {ex.Message}");
            }
        }

        public async Task<List<BarcodeDto>> GetBarcodesForSessionAsync(int sessionId)
        {
            var barcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && b.IsActive)
                .OrderBy(b => b.ModelProduct)
                .ThenBy(b => b.BoxNumber)
                .ToListAsync();

            return barcodes.Select(b => new BarcodeDto
            {
                BarcodeValue = b.BarcodeValue,
                ModelProduct = b.ModelProduct ?? "",
                BoxNumber = b.BoxNumber ?? 0,
                IsScanned = b.ScannedDate.HasValue,
                ScannedDate = b.ScannedDate,
                ScannedBy = b.ScannedBy
            }).ToList();
        }

        public async Task<List<BarcodeRegistry>> GetBarcodeRegistriesForSessionAsync(int sessionId)
        {
            return await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && b.IsActive)
                .OrderBy(b => b.ModelProduct)
                .ThenBy(b => b.BoxNumber)
                .ToListAsync();
        }

        public async Task<List<BarcodeItemDto>> GetBarcodeListForSessionAsync(int sessionId)
        {
            var scannedBarcodes = await GetScannedBarcodesAsync(sessionId);

            return await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && b.IsActive && b.BarcodeType == "BOX")
                .OrderBy(b => b.ModelProduct)
                .ThenBy(b => b.BoxNumber)
                .Select(b => new BarcodeItemDto
                {
                    BarcodeValue = b.BarcodeValue,
                    ModelProduct = b.ModelProduct ?? "",
                    BoxNumber = b.BoxNumber ?? 0,
                    IsScanned = b.Status == "SCANNED",
                    ScannedTime = b.ScannedDate
                })
                .ToListAsync();
        }

        public async Task<List<string>> GetScannedBarcodesAsync(int sessionId)
        {
            return await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && b.IsActive && b.Status == "SCANNED")
                .Select(b => b.BarcodeValue)
                .ToListAsync();
        }

        public async Task<BarcodeRegistry?> GetBarcodeByValueAsync(string barcodeValue)
        {
            return await _context.BarcodeRegistries
                .Include(b => b.Session)
                .Include(b => b.POMaster)
                .FirstOrDefaultAsync(b => b.BarcodeValue == barcodeValue && b.IsActive);
        }

        public async Task<int> GetTotalBarcodeCountAsync(int sessionId)
        {
            return await _context.BarcodeRegistries
                .CountAsync(b => b.SessionId == sessionId && b.IsActive && b.BarcodeType == "BOX");
        }

        public async Task<int> GetScannedBarcodeCountAsync(int sessionId)
        {
            return await _context.BarcodeRegistries
                .CountAsync(b => b.SessionId == sessionId && b.IsActive && b.Status == "SCANNED" && b.BarcodeType == "BOX");
        }

        public async Task<bool> IsBarcodeValidAsync(string barcodeValue, int sessionId)
        {
            return await _context.BarcodeRegistries
                .AnyAsync(b => b.BarcodeValue == barcodeValue && 
                             b.SessionId == sessionId && 
                             b.IsActive && 
                             b.Status == "GENERATED");
        }

        public async Task<bool> IsBarcodeScannedAsync(string barcodeValue)
        {
            return await _context.BarcodeRegistries
                .AnyAsync(b => b.BarcodeValue == barcodeValue && b.Status == "SCANNED");
        }

        public async Task<bool> IsSessionCompleteAsync(int sessionId)
        {
            var totalCount = await GetTotalBarcodeCountAsync(sessionId);
            var scannedCount = await GetScannedBarcodeCountAsync(sessionId);
            var masterScanned = await _context.BarcodeRegistries
                .AnyAsync(b => b.SessionId == sessionId && b.BarcodeType == "MASTER" && b.Status == "SCANNED");

            return totalCount > 0 && scannedCount == totalCount && masterScanned;
        }

        public async Task<Result> MarkBarcodeAsScannedAsync(string barcodeValue, string scannedBy)
        {
            try
            {
                var barcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.BarcodeValue == barcodeValue && b.IsActive);

                if (barcode == null)
                    return Result.Failure("Barcode not found");

                if (barcode.Status == "SCANNED")
                    return Result.Failure("Barcode already scanned");

                barcode.Status = "SCANNED";
                barcode.ScannedDate = DateTime.Now;
                barcode.ScannedBy = scannedBy;

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error marking barcode as scanned: {ex.Message}");
            }
        }

        public async Task<Result> UnmarkBarcodeAsync(string barcodeValue)
        {
            try
            {
                var barcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.BarcodeValue == barcodeValue && b.IsActive);

                if (barcode == null)
                    return Result.Failure("Barcode not found");

                barcode.Status = "GENERATED";
                barcode.ScannedDate = null;
                barcode.ScannedBy = null;

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error unmarking barcode: {ex.Message}");
            }
        }

        public async Task<Result> CancelExcessBarcodesAsync(int poId, int newQtyBox, string cancelledBy)
        {
            return await UpdateBarcodeQuantityAsync(poId, newQtyBox, cancelledBy);
        }

        public async Task<Result> CleanupSessionBarcodesAsync(int sessionId)
        {
            try
            {
                await _context.BarcodeRegistries
                    .Where(b => b.SessionId == sessionId)
                    .ExecuteUpdateAsync(b => b
                        .SetProperty(x => x.IsActive, false)
                        .SetProperty(x => x.Status, "CANCELLED"));

                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error cleaning up session barcodes: {ex.Message}");
            }
        }

        public async Task<List<POProgressDto>> GetProgressByPOAsync(int sessionId)
        {
            try
            {
                // Get PO data with row mapping from the original session upload
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null) return new List<POProgressDto>();

                var poProgress = await _context.BarcodeRegistries
                    .Where(b => b.SessionId == sessionId && b.BarcodeType == "BOX" && b.IsActive)
                    .Include(b => b.POMaster)
                    .GroupBy(b => new { b.POId, b.POMaster!.NoPO, b.ModelProduct })
                    .Select(g => new POProgressDto
                    {
                        POId = g.Key.POId ?? 0,
                        NoPO = g.Key.NoPO ?? "",
                        ModelProduct = g.Key.ModelProduct ?? "",
                        TotalBoxes = g.Count(),
                        ScannedBoxes = g.Count(b => b.Status == "SCANNED"),
                        ProgressPercentage = g.Count() > 0 ? (double)g.Count(b => b.Status == "SCANNED") / g.Count() * 100 : 0,
                        LastScanTime = g.Where(b => b.Status == "SCANNED").Max(b => b.ScannedDate),
                        Status = g.Count() > 0 && g.Count(b => b.Status == "SCANNED") == g.Count() ? "COMPLETED" : 
                                g.Any(b => b.Status == "SCANNED") ? "IN_PROGRESS" : "PENDING"
                    })
                    .ToListAsync();

                return poProgress;
            }
            catch (Exception)
            {
                return new List<POProgressDto>();
            }
        }

        public async Task<POProgressDto?> GetProgressByPOIdAsync(int sessionId, int poId)
        {
            try
            {
                var progressList = await GetProgressByPOAsync(sessionId);
                return progressList.FirstOrDefault(p => p.POId == poId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 🚨 MISSION CRITICAL: Auto-fix missing barcodes untuk SessionId tertentu
        /// Method ini akan memastikan TIDAK ADA BARCODE YANG MISS!
        /// </summary>
        public async Task<Result> AutoFixMissingBarcodesAsync(int sessionId, string fixedBy = "auto_fix_system")
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return Result.Failure($"Session {sessionId} not found");

                if (string.IsNullOrEmpty(session.IdentityQRCode))
                    return Result.Failure($"Session {sessionId} does not have QR Identity");

                var missingBarcodes = new List<BarcodeRegistry>();
                var fixReport = new List<string>();

                // Check setiap POMaster
                foreach (var poMaster in session.POMasters)
                {
                    if (poMaster.QtyBox <= 0) continue;

                    // Hitung berapa barcode yang sudah ada
                    var existingCount = await _context.BarcodeRegistries
                        .CountAsync(b => b.POId == poMaster.POId && b.BarcodeType == "BOX" && b.IsActive);

                    var missingCount = poMaster.QtyBox - existingCount;
                    
                    if (missingCount > 0)
                    {
                        // Generate missing barcodes
                        var existingBoxNumbers = await _context.BarcodeRegistries
                            .Where(b => b.POId == poMaster.POId && b.BarcodeType == "BOX" && b.IsActive)
                            .Select(b => b.BoxNumber)
                            .ToListAsync();

                        for (int boxNumber = 1; boxNumber <= poMaster.QtyBox; boxNumber++)
                        {
                            if (!existingBoxNumbers.Contains(boxNumber))
                            {
                                var barcode = $"QR_{sessionId}_{poMaster.NoPO}_{poMaster.ModelProduk}_{boxNumber:D3}";
                                
                                missingBarcodes.Add(new BarcodeRegistry
                                {
                                    BarcodeValue = barcode,
                                    BarcodeType = "BOX",
                                    SessionId = sessionId,
                                    POId = poMaster.POId,
                                    ModelProduct = poMaster.ModelProduk,
                                    BoxNumber = boxNumber,
                                    Status = "GENERATED",
                                    GeneratedBy = fixedBy,
                                    GeneratedDate = DateTime.Now,
                                    IsActive = true
                                });
                            }
                        }

                        fixReport.Add($"PO {poMaster.NoPO} ({poMaster.ModelProduk}): Added {missingCount} missing barcodes");
                    }
                }

                if (missingBarcodes.Any())
                {
                    // Insert missing barcodes dalam transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _context.BarcodeRegistries.AddRangeAsync(missingBarcodes);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var report = $"✅ AUTO-FIX COMPLETED: Added {missingBarcodes.Count} missing barcodes. Details: {string.Join("; ", fixReport)}";
                        return Result.Success();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return Result.Failure($"❌ AUTO-FIX FAILED: {ex.Message}");
                    }
                }
                else
                {
                    return Result.Success(); // No missing barcodes
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"❌ AUTO-FIX ERROR: {ex.Message}");
            }
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
