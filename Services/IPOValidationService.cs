using ShipmentFinishGood.Common;
using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    /// <summary>
    /// Service for PO and Session state validation in hierarchical lock system
    /// Ensures zero-failure validation for conveyor speed scanning
    /// </summary>
    public interface IPOValidationService
    {
        /// <summary>
        /// Validates if a session is still active and scannable
        /// </summary>
        /// <param name="sessionId">Session ID to validate</param>
        /// <returns>Session validation result with detailed status</returns>
        Task<Result<SessionValidationDto>> ValidateSessionStateAsync(int sessionId);

        /// <summary>
        /// Validates if a PO is still active and has scannable items
        /// </summary>
        /// <param name="poId">PO ID to validate</param>
        /// <returns>PO validation result with progress and next available item type</returns>
        Task<Result<POValidationDto>> ValidatePOStateAsync(int poId);

        /// <summary>
        /// Gets all available POs for a session that can be scanned
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>List of available POs with their status and progress</returns>
        Task<Result<List<POAvailabilityDto>>> GetAvailablePOsAsync(int sessionId);

        /// <summary>
        /// Resolves POId from a barcode value
        /// Critical for auto-detection of PO context during scanning
        /// </summary>
        /// <param name="barcode">Barcode value</param>
        /// <param name="sessionId">Session ID for validation</param>
        /// <returns>Resolved PO ID or null if cannot be determined</returns>
        Task<Result<int?>> ResolvePOFromBarcodeAsync(string barcode, int sessionId);

        /// <summary>
        /// Gets next available item type for a PO (BOX -> PALLET -> PCS progression)
        /// </summary>
        /// <param name="poId">PO ID</param>
        /// <returns>Next available item type or null if PO is complete</returns>
        Task<Result<string?>> GetNextAvailableItemTypeAsync(int poId);
    }
}
