using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    /// <summary>
    /// Registry of items (Box/Pallet/PCS) that belong to each PO
    /// </summary>
    public class POItemRegistry
    {
        [Key]
        public int ItemId { get; set; }
        
        [Required]
        public int POId { get; set; }
        
        [Required]
        public string ItemType { get; set; } = string.Empty; // BOX, PALLET, PCS
        
        [Required]
        public string BarcodeValue { get; set; } = string.Empty;
        
        public int ItemSequence { get; set; } // 1, 2, 3, etc for ordering
        
        public string Status { get; set; } = "PENDING"; // PENDING, SCANNED, VALIDATED
        
        public DateTime? ScannedAt { get; set; }
        
        public string? ScannedBy { get; set; }
        
        public DateTime? ValidatedAt { get; set; }
        
        public string? ValidatedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Additional metadata
        public string? ItemDescription { get; set; }
        public int? ExpectedQuantity { get; set; }
        public string? Container { get; set; }
        
        // Navigation properties
        public virtual POMaster POMaster { get; set; } = null!;
    }
    
    /// <summary>
    /// Enum for item types
    /// </summary>
    public enum POItemType
    {
        BOX,
        PALLET,
        PCS
    }
    
    /// <summary>
    /// Enum for item status
    /// </summary>
    public enum POItemStatus
    {
        PENDING,
        SCANNED,
        VALIDATED,
        COMPLETED
    }
}
