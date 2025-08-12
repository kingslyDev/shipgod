using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Services
{
    public interface IExcelProcessingService
    {
        Task<UploadPreviewDto> ProcessExcelFileAsync(IFormFile file, string uploadedBy);
        Task<List<ProcessedPOData>> CalculateProcessedDataAsync(List<ExcelRowData> rawData, string shipmentType);
        Task<bool> SubmitProcessedDataAsync(CalculationRequest request, string createdBy);
        Task<UploadPreviewDto?> GetPreviewAsync(int sessionId);
        Task<bool> UpdatePODataAsync(int poId, ProcessedPOData updatedData);
        Task<List<POMaster>> GetPOMastersBySessionAsync(int sessionId);
    }
}
