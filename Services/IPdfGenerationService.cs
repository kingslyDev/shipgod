using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface IPdfGenerationService
    {
        Task<byte[]> GenerateBarcodesPdfAsync(QRManagementDto qrData);
        Task<byte[]> GenerateQRIdentityPdfAsync(QRManagementDto qrData);
        Task<byte[]> GenerateComprehensiveReportPdfAsync(QRManagementDto qrData);
    }
}
