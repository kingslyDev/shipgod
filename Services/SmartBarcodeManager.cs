using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services
{
    public class SmartBarcodeManager : ISmartBarcodeManager
    {
        private readonly AppDbContext _context;

        public SmartBarcodeManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BarcodeChangeResult> ManageBarcodeChanges(
            string qrIdentity,
            string modelName,
            int oldBoxCount,
            int newBoxCount,
            string operatorId)
        {
            try
            {
                Console.WriteLine($"🔍 DEBUG ManageBarcodeChanges: QR={qrIdentity}, Model={modelName}");
                Console.WriteLine($"🔍 DEBUG: oldBoxCount={oldBoxCount}, newBoxCount={newBoxCount}");
                
                if (newBoxCount == oldBoxCount)
                {
                    Console.WriteLine("🟢 No changes needed");
                    // 🟢 NO CHANGES NEEDED
                    return BarcodeChangeResult.CreateSuccess();
                }

                // 🔍 GET SESSION INFORMATION
                var session = await _context.UploadSessions
                    .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);
                
                if (session == null)
                {
                    Console.WriteLine("❌ Session not found");
                    return BarcodeChangeResult.CreateFailure("Session not found for QR Identity");
                }

                Console.WriteLine($"✅ Found session: {session.SessionId}");

                // 🔍 GET POMASTER FOR MODEL
                var poMaster = await _context.POMasters
                    .FirstOrDefaultAsync(p => p.SourceSessionId == session.SessionId && 
                                             p.ModelProduk == modelName);

                if (poMaster == null)
                {
                    Console.WriteLine($"❌ POMaster not found for model {modelName}");
                    return BarcodeChangeResult.CreateFailure($"POMaster not found for model {modelName}");
                }

                Console.WriteLine($"✅ Found POMaster: POId={poMaster.POId}");

                if (newBoxCount > oldBoxCount)
                {
                    // 📦 GENERATE NEW BARCODES (Smart Incremental)
                    var newBarcodes = await GenerateIncrementalBarcodes(
                        session.SessionId, poMaster.POId, qrIdentity, modelName, 
                        oldBoxCount + 1, newBoxCount, operatorId);
                    return BarcodeChangeResult.CreateSuccess(newBarcodes: newBarcodes);
                }
                else
                {
                    // 🗑️ REMOVE HIGHEST NUMBER BARCODES (Smart Protection)
                    var removedBarcodes = await RemoveHighestBarcodes(
                        session.SessionId, poMaster.POId, qrIdentity, modelName, 
                        newBoxCount, oldBoxCount, operatorId); // Fix parameter order
                    return BarcodeChangeResult.CreateSuccess(removedBarcodes: removedBarcodes);
                }
            }
            catch (Exception ex)
            {
                return BarcodeChangeResult.CreateFailure($"Barcode management error: {ex.Message}");
            }
        }

        private async Task<List<string>> GenerateIncrementalBarcodes(
            int sessionId,
            int poId,
            string qrIdentity,
            string modelName,
            int startBoxNumber,
            int endBoxNumber,
            string operatorId)
        {
            var newBarcodes = new List<string>();

            for (int i = startBoxNumber; i <= endBoxNumber; i++)
            {
                var barcode = $"{qrIdentity}_BOX_{modelName}_{i:D3}";
                
                // 🔍 CHECK FOR DUPLICATES
                var existingBarcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.BarcodeValue == barcode);

                if (existingBarcode == null)
                {
                    // 📝 CREATE NEW BARCODE REGISTRY
                    var barcodeRegistry = new BarcodeRegistry
                    {
                        BarcodeValue = barcode,
                        BarcodeType = "BOX",
                        SessionId = sessionId,
                        POId = poId,
                        ModelProduct = modelName,
                        BoxNumber = i,
                        Status = "GENERATED",
                        GeneratedDate = DateTime.Now,
                        GeneratedBy = operatorId,
                        IsActive = true
                    };

                    _context.BarcodeRegistries.Add(barcodeRegistry);
                    newBarcodes.Add(barcode);
                }
                else if (!existingBarcode.IsActive)
                {
                    // 🔄 REACTIVATE EXISTING BARCODE
                    existingBarcode.Status = "GENERATED";
                    existingBarcode.GeneratedDate = DateTime.Now;
                    existingBarcode.GeneratedBy = operatorId;
                    existingBarcode.IsActive = true;
                    existingBarcode.ScannedDate = null;
                    existingBarcode.ScannedBy = null;
                    newBarcodes.Add(barcode);
                }
            }

            await _context.SaveChangesAsync();
            return newBarcodes;
        }

        private async Task<List<string>> RemoveHighestBarcodes(
            int sessionId,
            int poId,
            string qrIdentity,
            string modelName,
            int newBoxCount,
            int oldBoxCount,
            string operatorId)
        {
            var removedBarcodes = new List<string>();

            Console.WriteLine($"�️ REMOVE HIGHEST BARCODES:");
            Console.WriteLine($"   SessionId: {sessionId}, POId: {poId}");
            Console.WriteLine($"   Model: {modelName}");
            Console.WriteLine($"   NewBoxCount: {newBoxCount}, OldBoxCount: {oldBoxCount}");
            Console.WriteLine($"   Need to remove: {oldBoxCount - newBoxCount} boxes");
            
            // 🛡️ GET ALL ACTIVE BARCODES FOR THIS MODEL (SORTED BY BOX NUMBER DESCENDING)
            var allActiveBarcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId &&
                           b.POId == poId &&
                           b.ModelProduct == modelName &&
                           b.IsActive == true)
                .OrderByDescending(b => b.BoxNumber) // 🔢 HIGHEST FIRST
                .ToListAsync();

            Console.WriteLine($"� Found {allActiveBarcodes.Count} total active barcodes for this model:");
            foreach (var b in allActiveBarcodes.Take(10)) // Show first 10
            {
                Console.WriteLine($"   Box #{b.BoxNumber}: {b.BarcodeValue} (Status: {b.Status})");
            }
            
            // 🎯 FILTER: GET BARCODES TO REMOVE
            // Strategy: Remove boxes with numbers > newBoxCount, starting from highest
            var barcodesToRemove = allActiveBarcodes
                .Where(b => b.BoxNumber > newBoxCount && // 🔢 BOX NUMBER HIGHER THAN NEW COUNT
                           b.Status == "GENERATED") // 🛡️ ONLY REMOVE GENERATED (NOT SCANNED)
                .Take(oldBoxCount - newBoxCount) // 🎯 EXACT NUMBER TO REMOVE
                .ToList();

            Console.WriteLine($"🎯 Filtered {barcodesToRemove.Count} barcodes to remove:");
            foreach (var b in barcodesToRemove)
            {
                Console.WriteLine($"   Will DELETE Box #{b.BoxNumber}: {b.BarcodeValue}");
            }
            
            // 🗑️ PERFORM SOFT DELETE
            foreach (var barcode in barcodesToRemove)
            {
                Console.WriteLine($"🗑️ DELETING: {barcode.BarcodeValue} (Box #{barcode.BoxNumber})");
                
                // Soft delete by marking as CANCELLED and inactive
                barcode.Status = "CANCELLED";
                barcode.IsActive = false;
                barcode.ScannedDate = DateTime.Now; // Mark when cancelled
                barcode.ScannedBy = operatorId; // Mark who cancelled it
                
                removedBarcodes.Add(barcode.BarcodeValue);
            }

            // 💾 SAVE CHANGES TO DATABASE
            if (removedBarcodes.Any())
            {
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Successfully removed {removedBarcodes.Count} barcodes (DB affected: {saveResult} rows)");
                
                // 🔍 VERIFY DELETION
                var verifyCount = await _context.BarcodeRegistries
                    .Where(b => b.SessionId == sessionId &&
                               b.POId == poId &&
                               b.ModelProduct == modelName &&
                               b.IsActive == true)
                    .CountAsync();
                    
                Console.WriteLine($"🔍 VERIFICATION: {verifyCount} active barcodes remaining (should be {newBoxCount})");
            }
            else
            {
                Console.WriteLine("⚠️ WARNING: No barcodes were marked for removal!");
                
                // 🔍 DEBUG: Let's see what barcodes exist
                Console.WriteLine("🔍 DEBUG: All barcodes status:");
                foreach (var b in allActiveBarcodes)
                {
                    var shouldRemove = b.BoxNumber > newBoxCount ? "YES" : "NO";
                    var canRemove = b.Status == "GENERATED" ? "YES" : "NO";
                    Console.WriteLine($"   Box #{b.BoxNumber}: Status={b.Status}, Should Remove={shouldRemove}, Can Remove={canRemove}");
                }
            }

            return removedBarcodes;
        }

        public async Task<List<BarcodeRegistryInfo>> GetBarcodesByQRIdentityAsync(string qrIdentity, string? modelName = null)
        {
            var session = await _context.UploadSessions
                .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);
            
            if (session == null) return new List<BarcodeRegistryInfo>();

            var query = _context.BarcodeRegistries.AsQueryable()
                .Where(b => b.SessionId == session.SessionId && b.IsActive == true);

            if (!string.IsNullOrEmpty(modelName))
            {
                query = query.Where(b => b.ModelProduct == modelName);
            }

            var barcodes = await query
                .OrderBy(b => b.ModelProduct)
                .ThenBy(b => b.BoxNumber)
                .ToListAsync();

            return barcodes.Select(b => new BarcodeRegistryInfo
            {
                BarcodeValue = b.BarcodeValue,
                QRIdentity = qrIdentity,
                ModelName = b.ModelProduct ?? "Unknown",
                BoxNumber = b.BoxNumber ?? 0,
                Status = b.Status,
                GeneratedDate = b.GeneratedDate,
                GeneratedBy = b.GeneratedBy ?? "System",
                ScannedDate = b.ScannedDate,
                ScannedBy = b.ScannedBy,
                Country = "ID", // Default
                Area = "WAREHOUSE" // Default
            }).ToList();
        }

        public async Task<BarcodeStatistics> GetBarcodeStatisticsAsync(string qrIdentity)
        {
            var session = await _context.UploadSessions
                .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);
            
            if (session == null) return new BarcodeStatistics { QRIdentity = qrIdentity };

            var barcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == session.SessionId && b.IsActive == true)
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var stats = new BarcodeStatistics
            {
                QRIdentity = qrIdentity,
                TotalGenerated = barcodes.Where(b => b.Status == "GENERATED").Sum(b => b.Count),
                TotalScanning = 0, // No SCANNING status in current model
                TotalCompleted = barcodes.Where(b => b.Status == "SCANNED").Sum(b => b.Count),
                TotalActive = barcodes.Sum(b => b.Count)
            };

            return stats;
        }

        // 🔍 DEBUG HELPER METHOD
        public async Task<string> GetBarcodeStatusDebugInfo(string qrIdentity, string modelName)
        {
            var session = await _context.UploadSessions
                .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);
            
            if (session == null) return "Session not found";

            var poMaster = await _context.POMasters
                .FirstOrDefaultAsync(p => p.SourceSessionId == session.SessionId && 
                                         p.ModelProduk == modelName);

            if (poMaster == null) return "POMaster not found";

            var barcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == session.SessionId &&
                           b.POId == poMaster.POId &&
                           b.ModelProduct == modelName)
                .OrderBy(b => b.BoxNumber)
                .ToListAsync();

            var result = $"DEBUG INFO for {qrIdentity} - {modelName}:\n";
            result += $"SessionId: {session.SessionId}, POId: {poMaster.POId}\n";
            result += $"Current QtyBox in POMaster: {poMaster.QtyBox}\n";
            result += $"Total Barcodes in Registry: {barcodes.Count}\n";
            result += "Barcode Details:\n";

            foreach (var b in barcodes)
            {
                result += $"  Box #{b.BoxNumber}: {b.BarcodeValue} - Status={b.Status}, Active={b.IsActive}\n";
            }

            return result;
        }
    }

    // 📊 SUPPORTING DTOs FOR BARCODE MANAGEMENT
    public class BarcodeRegistryInfo
    {
        public string BarcodeValue { get; set; } = string.Empty;
        public string QRIdentity { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int BoxNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime? ScannedDate { get; set; }
        public string? ScannedBy { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
    }

    public class BarcodeStatistics
    {
        public string QRIdentity { get; set; } = string.Empty;
        public int TotalGenerated { get; set; }
        public int TotalScanning { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalActive { get; set; }
        public double CompletionRate => TotalActive > 0 ? (double)TotalCompleted / TotalActive * 100 : 0;
    }
}
