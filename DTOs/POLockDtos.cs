using System;

namespace ShipmentFinishGood.DTOs
{
    /// <summary>
    /// DTO for PO Lock information
    /// </summary>
    public class POLockDto
    {
        public int POLockId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public int POId { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string ModelProduk { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; }
        public bool IsActive { get; set; }
        public string? LockedBy { get; set; }
        
        // Additional PO information
        public int QtyBox { get; set; }
        public int QtyTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Progress information
        public int ScannedBoxes { get; set; }
        public double Progress => QtyBox > 0 ? (double)ScannedBoxes / QtyBox * 100 : 0;
        public bool IsCompleted => ScannedBoxes >= QtyBox;
    }

    /// <summary>
    /// DTO for available POs in a session for lock selection
    /// </summary>
    public class AvailablePODto
    {
        public int POId { get; set; }
        public string NoPO { get; set; } = string.Empty;
        public string ModelProduk { get; set; } = string.Empty;
        public int QtyBox { get; set; }
        public int QtyTotal { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Container { get; set; }
        public string? ShipmentDetail { get; set; }
        
        // Scanning progress
        public int ScannedBoxes { get; set; }
        public double Progress => QtyBox > 0 ? (double)ScannedBoxes / QtyBox * 100 : 0;
        public bool IsCompleted => ScannedBoxes >= QtyBox;
        public bool IsAvailableForLock => !IsCompleted;
        
        // Current lock status
        public bool IsLocked { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
    }

    /// <summary>
    /// Response DTO for PO lock operations
    /// </summary>
    public class POLockResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public POLockDto? POLock { get; set; }
        public List<AvailablePODto> AvailablePOs { get; set; } = new List<AvailablePODto>();
    }
}
