using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public interface IQRManagementService
    {
        Task<QRManagementDto?> GetQRDataAsync(int sessionId);
        Task<bool> RegenerateQRAsync(int sessionId, string generatedBy);
        Task<byte[]?> GenerateBarcodesPDFAsync(int sessionId);
        Task<byte[]?> GetQRImageAsync(int sessionId);
    }
}
