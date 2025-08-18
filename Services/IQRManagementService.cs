using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface IQRManagementService
    {
        Task<QRManagementDto?> GetQRDataAsync(int sessionId);
        Task<byte[]?> GenerateBarcodesPDFAsync(int sessionId);
        Task<byte[]?> GetQRImageAsync(int sessionId);
    }
}
