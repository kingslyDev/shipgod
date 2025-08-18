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

        public QRManagementService(AppDbContext context, IBarcodeService barcodeService)
        {
            _context = context;
            _barcodeService = barcodeService;
        }

        public async Task<QRManagementDto?> GetQRDataAsync(int sessionId)
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
            var barcodeList = barcodes.Select(b => b.BarcodeValue).ToList();

            // Generate QR code image as base64
            var qrImageBase64 = GenerateQRCodeBase64(masterBarcode.BarcodeValue);

            return new QRManagementDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                QRIdentity = masterBarcode.BarcodeValue,
                QRImageBase64 = qrImageBase64,
                Status = "Ready for Scanning",
                GeneratedDate = session.UploadDate,
                GeneratedBy = session.UploadedBy ?? "System",
                TotalBoxes = session.TotalBoxes,
                BarcodeList = barcodeList,
                CanRegenerate = false // Remove regenerate functionality
            };
        }

        public async Task<byte[]?> GenerateBarcodesPDFAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return null;

            // Get master barcode from BarcodeRegistries
            var masterBarcode = await _context.BarcodeRegistries
                .FirstOrDefaultAsync(b => b.SessionId == sessionId && 
                                        b.BarcodeType == "MASTER" && 
                                        b.IsActive);

            if (masterBarcode == null)
                return null;

            // Get all barcodes from BarcodeRegistries instead of generating them
            var boxBarcodes = await _context.BarcodeRegistries
                .Where(b => b.SessionId == sessionId && 
                           b.BarcodeType == "BOX" && 
                           b.IsActive)
                .OrderBy(b => b.ModelProduct)
                .ThenBy(b => b.BoxNumber)
                .ToListAsync();

            // For now, return a simple text-based PDF placeholder
            // In production, use a proper PDF library like iTextSharp or PdfSharpCore
            var content = $"PDF Barcodes for Session {sessionId}\n";
            content += $"QR Identity: {masterBarcode.BarcodeValue}\n";
            content += $"Generated: {DateTime.Now}\n\n";
            content += "BOX BARCODES:\n";
            
            foreach (var barcode in boxBarcodes)
            {
                content += $"- {barcode.BarcodeValue}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(content);
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
    }
}
