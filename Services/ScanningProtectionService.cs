using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Repositories;

namespace ShipmentFinishGood.Services
{
    public class ScanningProtectionService : IScanningProtectionService
    {
        private readonly AppDbContext _context;

        public ScanningProtectionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConflictResolutionResult> CheckScanningConflicts(
            string qrIdentity, 
            string modelName, 
            int oldBoxCount, 
            int newBoxCount)
        {
            try
            {
                // 🟢 NO CHANGES = NO CONFLICTS
                if (newBoxCount == oldBoxCount)
                {
                    return ConflictResolutionResult.NoConflict();
                }

                // 📦 INCREASE BOXES = NO CONFLICTS (Only adding new)
                if (newBoxCount > oldBoxCount)
                {
                    return ConflictResolutionResult.NoConflict();
                }

                // 🔍 DECREASE BOXES = CHECK FOR SCANNING CONFLICTS
                var session = await _context.UploadSessions
                    .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);

                if (session == null)
                {
                    return ConflictResolutionResult.CreateConflict(
                        "Session not found for QR Identity", 
                        new List<string>());
                }

                var poMaster = await _context.POMasters
                    .FirstOrDefaultAsync(p => p.SourceSessionId == session.SessionId && 
                                             p.ModelProduk == modelName);

                if (poMaster == null)
                {
                    return ConflictResolutionResult.CreateConflict(
                        $"POMaster not found for model {modelName}", 
                        new List<string>());
                }

                // 🛡️ CHECK FOR ACTIVE/SCANNED BARCODES THAT WOULD BE REMOVED
                var conflictingBarcodes = await _context.BarcodeRegistries
                    .Where(b => b.SessionId == session.SessionId &&
                               b.POId == poMaster.POId &&
                               b.ModelProduct == modelName &&
                               b.BoxNumber > newBoxCount && // Boxes that would be removed
                               b.BoxNumber <= oldBoxCount &&
                               (b.Status == "SCANNED" || // Already scanned - PROTECTED
                                b.Status == "GENERATED") && // Could be currently scanning
                               b.IsActive == true)
                    .Select(b => b.BarcodeValue)
                    .ToListAsync();

                if (conflictingBarcodes.Any())
                {
                    // 🔍 CHECK IF ANY ARE ACTUALLY BEING SCANNED NOW
                    var activelyScanning = await CheckActivelyScanningBarcodes(conflictingBarcodes);
                    
                    if (activelyScanning.Any())
                    {
                        return ConflictResolutionResult.CreateConflict(
                            $"Cannot reduce boxes: {activelyScanning.Count} barcodes are currently being scanned or already completed",
                            activelyScanning);
                    }

                    // 🔍 CHECK FOR COMPLETED SCANS
                    var completedScans = await _context.BarcodeRegistries
                        .Where(b => conflictingBarcodes.Contains(b.BarcodeValue) &&
                                   b.Status == "SCANNED")
                        .Select(b => b.BarcodeValue)
                        .ToListAsync();

                    if (completedScans.Any())
                    {
                        return ConflictResolutionResult.CreateConflict(
                            $"Cannot reduce boxes: {completedScans.Count} boxes have already been scanned and cannot be removed",
                            completedScans);
                    }
                }

                // ✅ NO CONFLICTS FOUND
                return ConflictResolutionResult.NoConflict();
            }
            catch (Exception ex)
            {
                return ConflictResolutionResult.CreateConflict(
                    $"Error checking scanning conflicts: {ex.Message}", 
                    new List<string>());
            }
        }

        private async Task<List<string>> CheckActivelyScanningBarcodes(List<string> barcodes)
        {
            // 🔍 CHECK SCANNING ACTIVITY TABLE FOR RECENT ACTIVITY
            // Consider barcodes scanned in the last hour as potentially active
            var recentScanningActivity = await _context.ScanningActivities
                .Where(s => barcodes.Contains(s.BarcodeValue) &&
                           s.Action == "SCAN_BOX" &&
                           s.Timestamp > DateTime.Now.AddHours(-1))
                .Select(s => s.BarcodeValue)
                .Distinct()
                .ToListAsync();

            return recentScanningActivity;
        }

        public async Task<List<string>> GetProtectedBarcodesAsync(string qrIdentity, string modelName)
        {
            var session = await _context.UploadSessions
                .FirstOrDefaultAsync(s => s.IdentityQRCode == qrIdentity);

            if (session == null) return new List<string>();

            var poMaster = await _context.POMasters
                .FirstOrDefaultAsync(p => p.SourceSessionId == session.SessionId && 
                                         p.ModelProduk == modelName);

            if (poMaster == null) return new List<string>();

            // 🛡️ PROTECTED BARCODES: SCANNED OR ACTIVELY SCANNING
            var protectedBarcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == session.SessionId &&
                           b.POId == poMaster.POId &&
                           b.ModelProduct == modelName &&
                           (b.Status == "SCANNED") && // Already completed
                           b.IsActive == true)
                .Select(b => b.BarcodeValue)
                .ToListAsync();

            return protectedBarcodes;
        }
    }
}
