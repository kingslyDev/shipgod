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

        public QRManagementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QRManagementDto?> GetQRDataAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null || string.IsNullOrEmpty(session.IdentityQRCode))
                return null;

            // Generate barcode list
            var barcodeList = new List<string>();
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    var barcode = $"{session.IdentityQRCode}_BOX_{poMaster.ModelProduk}_{i:D3}";
                    barcodeList.Add(barcode);
                }
            }

            // Generate QR code image as base64
            var qrImageBase64 = GenerateQRCodeBase64(session.IdentityQRCode);

            return new QRManagementDto
            {
                SessionId = session.SessionId,
                FileName = session.FileName,
                SheetName = session.SheetName,
                QRIdentity = session.IdentityQRCode,
                QRImageBase64 = qrImageBase64,
                Status = "Ready for Scanning",
                GeneratedDate = session.UploadDate,
                GeneratedBy = session.UploadedBy ?? "System",
                TotalBoxes = session.TotalBoxes,
                BarcodeList = barcodeList,
                CanRegenerate = true
            };
        }

        public async Task<bool> RegenerateQRAsync(int sessionId, string generatedBy)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null) return false;

            // Generate new QR Identity
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
            }
            
            session.IdentityQRCode = qrIdentity;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]?> GenerateBarcodesPDFAsync(int sessionId)
        {
            var session = await _context.UploadSessions
                .Include(s => s.POMasters)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null || string.IsNullOrEmpty(session.IdentityQRCode))
                return null;

            // For now, return a simple text-based PDF placeholder
            // In production, use a proper PDF library like iTextSharp or PdfSharpCore
            var content = $"PDF Barcodes for Session {sessionId}\n";
            content += $"QR Identity: {session.IdentityQRCode}\n";
            content += $"Generated: {DateTime.Now}\n\n";
            content += "BOX BARCODES:\n";
            
            foreach (var poMaster in session.POMasters)
            {
                for (int i = 1; i <= poMaster.QtyBox; i++)
                {
                    var barcode = $"{session.IdentityQRCode}_BOX_{poMaster.ModelProduk}_{i:D3}";
                    content += $"- {barcode}\n";
                }
            }

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        public async Task<byte[]?> GetQRImageAsync(int sessionId)
        {
            var session = await _context.UploadSessions.FindAsync(sessionId);
            if (session == null || string.IsNullOrEmpty(session.IdentityQRCode))
                return null;

            return GenerateQRCodeBytes(session.IdentityQRCode);
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
