using ShipmentFinishGood.DTOs;
using ShipmentFinishGood.Common;
using ShipmentFinishGood.Models;

namespace ShipmentFinishGood.Services
{
    public interface IBarcodeService
    {
        // Generation methods
        Task<Result> GenerateBarcodesForSessionAsync(int sessionId, string generatedBy);
        Task<Result> RegenerateBarcodesForPOAsync(int poId, string generatedBy);
        Task<Result> UpdateBarcodeQuantityAsync(int poId, int newQtyBox, string updatedBy);
        
        // Query methods
        Task<List<BarcodeDto>> GetBarcodesForSessionAsync(int sessionId);
        Task<List<BarcodeRegistry>> GetBarcodeRegistriesForSessionAsync(int sessionId); // New method
        Task<List<BarcodeItemDto>> GetBarcodeListForSessionAsync(int sessionId);
        Task<List<string>> GetScannedBarcodesAsync(int sessionId);
        Task<BarcodeRegistry?> GetBarcodeByValueAsync(string barcodeValue);
        Task<int> GetTotalBarcodeCountAsync(int sessionId);
        Task<int> GetScannedBarcodeCountAsync(int sessionId);
        
        // Validation methods
        Task<bool> IsBarcodeValidAsync(string barcodeValue, int sessionId);
        Task<bool> IsBarcodeScannedAsync(string barcodeValue);
        Task<bool> IsSessionCompleteAsync(int sessionId);
        
        // Scanning operations
        Task<Result> MarkBarcodeAsScannedAsync(string barcodeValue, string scannedBy);
        Task<Result> UnmarkBarcodeAsync(string barcodeValue); // For testing/admin purposes
        
        // Management operations
        Task<Result> CancelExcessBarcodesAsync(int poId, int newQtyBox, string cancelledBy);
        Task<Result> CleanupSessionBarcodesAsync(int sessionId);
        
        // Progress tracking
        Task<List<POProgressDto>> GetProgressByPOAsync(int sessionId);
        Task<POProgressDto?> GetProgressByPOIdAsync(int sessionId, int poId);
    }
}
