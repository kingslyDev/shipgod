using Microsoft.EntityFrameworkCore;
using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Repositories;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using System.IO;

namespace ShipmentFinishGood.Services
{
    public class QRManagementService : IQRManagementService
    {
        private readonly AppDbContext _context;
        private readonly IBarcodeService _barcodeService;
        private readonly IScanningService _scanningService;
        private readonly IPdfGenerationService _pdfGenerationService;

        public QRManagementService(AppDbContext context, IBarcodeService barcodeService, 
            IScanningService scanningService, IPdfGenerationService pdfGenerationService)
        {
            _context = context;
            _barcodeService = barcodeService;
            _scanningService = scanningService;
            _pdfGenerationService = pdfGenerationService;
        }

        public async Task<QRManagementDto?> GetQRDataAsync(int sessionId)
        {
            try
            {
                var session = await _context.UploadSessions
                    .Include(s => s.POMasters)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                    return null;

                // Get master barcode from BarcodeRegistries instead of using IdentityQRCode
                var masterBarcode = await _context.BarcodeRegistries
                    .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                            b.BarcodeType == "MASTER" && 
                                            b.IsActive);

                if (masterBarcode == null)
                    return null;

                // Get barcode list from database
                var barcodes = await _barcodeService.GetBarcodeListForSessionAsync(sessionId);
                var barcodeList = barcodes?.Select(b => b.BarcodeValue).ToList() ?? new List<string>();

                // Get PO Master data for this session
                var poMasters = await _context.POMasters
                    .Where(po => po.SourceSessionId == sessionId)
                    .ToListAsync();

                if (poMasters == null)
                    poMasters = new List<Models.POMaster>();

                // Calculate totals from PO Masters
                var totalPallets = poMasters.Sum(po => po.QtyPallet);
                var totalBoxes = poMasters.Sum(po => po.QtyBox);
                var totalPcs = poMasters.Sum(po => po.QtyPcs);
                var totalQty = poMasters.Sum(po => po.QtyTotal);

                // Get ONLY BOX barcodes for scanning activities - EXCLUDE MASTER
                var sessionBoxBarcodes = await _context.BarcodeRegistries
                    .Where(b => b.SessionId == sessionId && 
                               b.IsActive && 
                               b.BarcodeType == "BOX") // ONLY BOX barcodes
                    .Select(b => b.BarcodeValue)
                    .ToListAsync();

                if (sessionBoxBarcodes == null)
                    sessionBoxBarcodes = new List<string>();

                // Get scanning activities for BOX scans only - NO MASTER QR
                var scannedBoxActivities = await _context.ScanningActivities
                    .Where(sa => sessionBoxBarcodes.Contains(sa.BarcodeValue) && 
                                sa.Result == "SUCCESS" &&
                                sa.Action == "SCAN_BOX") // Only SCAN_BOX actions
                    .OrderByDescending(sa => sa.Timestamp)
                    .ToListAsync();

                if (scannedBoxActivities == null)
                    scannedBoxActivities = new List<Models.ScanningActivity>();

            // Get complete scan progress including all types (BOX, PALLET, PCS)
            var scanProgress = await _scanningService.GetScanProgressAsync(sessionId);

            // Ensure scanProgress is not null and has valid values
            if (scanProgress == null)
            {
                scanProgress = new ScanProgressDto
                {
                    SessionId = sessionId,
                    TotalBoxes = totalBoxes,
                    TotalPallets = totalPallets,
                    TotalPcs = totalPcs,
                    ScannedBoxes = 0,
                    ScannedPallets = 0,
                    ScannedPcs = 0,
                    CanComplete = false
                };
            }

            // Use comprehensive scanning data
            var totalScanned = scanProgress.ScannedBoxes + scanProgress.ScannedPallets + scanProgress.ScannedPcs;
            var totalItems = scanProgress.TotalBoxes + scanProgress.TotalPallets + scanProgress.TotalPcs;
            var lastScanned = scannedBoxActivities.FirstOrDefault(); // Keep BOX for last scanned info

            // Calculate scan percentage based on total items (BOX + PALLET + PCS)
            var scanPercentage = totalItems > 0 ? (int)Math.Round((double)totalScanned / totalItems * 100) : 0;

            // Create PO summaries with scan count per PO
            var poSummaries = new List<POSummaryInfo>();
            foreach (var po in poMasters)
            {
                // Ensure PO properties are not null
                var modelProduct = po.ModelProduk ?? "";
                var poNumber = po.NoPO ?? "";
                var status = po.Status ?? "PENDING";
                
                // Get scan count for this specific PO by matching model product in barcode
                var poScannedCount = scannedBoxActivities.Count(sa => 
                    sessionBoxBarcodes.Any(bc => bc.Contains(modelProduct.Replace(" ", "")) && bc == sa.BarcodeValue));
                
                var poScanPercentage = po.QtyBox > 0 ? 
                    Math.Round((decimal)poScannedCount / po.QtyBox * 100, 1) : 0;

                poSummaries.Add(new POSummaryInfo
                {
                    PONumber = poNumber,
                    ModelProduct = modelProduct,
                    QtyTotal = po.QtyTotal,
                    QtyPallet = po.QtyPallet,
                    QtyBox = po.QtyBox,
                    QtyPcs = po.QtyPcs,
                    Status = status,
                    ScannedCount = poScannedCount,
                    ScannedPercentage = poScanPercentage
                });
            }

            // Generate QR code image as base64
            var qrImageBase64 = GenerateQRCodeBase64(masterBarcode.BarcodeValue);

            return new QRManagementDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName ?? "",
                SheetName = session.SheetName ?? "",
                QRIdentity = masterBarcode.BarcodeValue ?? "",
                QRImageBase64 = qrImageBase64 ?? "",
                Status = "Ready for Scanning",
                GeneratedDate = session.UploadDate,
                GeneratedBy = session.UploadedBy ?? "System",
                TotalBoxes = totalBoxes,
                TotalPallets = totalPallets,
                TotalPcs = totalPcs,
                TotalQty = totalQty,
                
                // Comprehensive scanning information
                TotalScanned = totalScanned,
                ScannedBoxes = scanProgress.ScannedBoxes,
                ScannedPallets = scanProgress.ScannedPallets,
                ScannedPcs = scanProgress.ScannedPcs,
                ScanPercentage = scanPercentage,
                CanComplete = scanProgress.CanComplete,
                
                LastScannedDate = lastScanned?.Timestamp,
                LastScannedBy = lastScanned?.UserId,
                BarcodeList = barcodeList ?? new List<string>(),
                POSummaries = poSummaries ?? new List<POSummaryInfo>(),
                CanRegenerate = false // Remove regenerate functionality
            };
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to add proper logging here)
                Console.WriteLine($"Error in GetQRDataAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]?> GenerateBarcodesPDFAsync(int sessionId)
        {
            try
            {
                // Get QR data for PDF generation
                var qrData = await GetQRDataAsync(sessionId);
                if (qrData == null)
                    return null;

                // Use the professional PDF generation service
                return await _pdfGenerationService.GenerateBarcodesPdfAsync(qrData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                return null;
            }
        }

        public async Task<byte[]?> GetQRImageAsync(int sessionId)
        {
            // Get master barcode from BarcodeRegistries
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return null;

            return GenerateQRCodeBytes(masterBarcode.BarcodeValue);
        }

        private string GenerateQRCodeBase64(string content)
        {
            try
            {
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var qrBitmap = new BitmapByteQRCode(qrCodeData);
                var qrCodeImage = qrBitmap.GetGraphic(20);
                
                return Convert.ToBase64String(qrCodeImage);
            }
            catch
            {
                // Fallback: return empty base64 image
                return string.Empty;
            }
        }

        private byte[]? GenerateQRCodeBytes(string content)
        {
            try
            {
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                var qrBitmap = new BitmapByteQRCode(qrCodeData);
                return qrBitmap.GetGraphic(20);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> RegenerateQRCodeAsync(int sessionId)
        {
            try
            {
                var session = await _context.UploadSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null) return null;

                var qrIdentity = session.IdentityQRCode;
                if (string.IsNullOrEmpty(qrIdentity)) return null;

                // Generate new QR code using the private method
                var qrImageBytes = GenerateQRCodeBase64(qrIdentity);
                if (string.IsNullOrEmpty(qrImageBytes)) return null;

                return qrImageBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error regenerating QR code: {ex.Message}");
                return null;
            }
        }
    }
}
