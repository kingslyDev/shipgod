using ShipmentFinishGood.ViewModels;
using System.Threading.Tasks;

namespace ShipmentFinishGood.Services;

public interface IGroupingService
{
    Task<POUploadPreviewViewModel> BuildPreviewAsync(Stream fileStream, string fileName);
    Task<bool> CommitAsync(POUploadPreviewViewModel preview, string performedBy);
    Task<POUploadPreviewViewModel> ApplyMethodAndGroupAsync(POUploadPreviewViewModel preview, string method);
    Task<int> ParseAndStoreAsync(Stream fileStream, string fileName, string createdBy);
    Task<POUploadPreviewViewModel> LoadSessionPreviewAsync(int sessionId);
    Task<bool> SaveRowMethodsAsync(int sessionId, Dictionary<int, string> rowMethods);
}
