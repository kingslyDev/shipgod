using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class BarcodeRegistry
    {
        [Key]
        public int BarcodeId { get; set; }
        
        [Required]
        public string BarcodeValue { get; set; } = string.Empty;
        
        [Required]
        public string BarcodeType { get; set; } = string.Empty; // 'MASTER', 'BOX'
        
        public int SessionId { get; set; }
        
        public int? POId { get; set; } // Link to specific POMaster for BOX type
        
        public string? ModelProduct { get; set; } // Cache model product name
        
        public int? BoxNumber { get; set; } // Box sequence number for BOX type
        
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        
        public string? GeneratedBy { get; set; }
        
        public string Status { get; set; } = "GENERATED"; // 'GENERATED', 'SCANNED', 'CANCELLED'
        
        public DateTime? ScannedDate { get; set; }
        
        public string? ScannedBy { get; set; }
        
        public bool IsActive { get; set; } = true; // For soft delete
        
        // Navigation properties
        public virtual UploadSession Session { get; set; } = null!;
        public virtual POMaster? POMaster { get; set; }
    }
}
