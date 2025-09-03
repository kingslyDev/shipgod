using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class ScanningActivity
    {
        [Key]
        public int ActivityId { get; set; }
        
        [Required]
        public string BarcodeValue { get; set; } = string.Empty;
        
        [Required]
        public string Action { get; set; } = string.Empty; // 'SCAN_MASTER', 'SCAN_BOX', 'AREA_ASSIGN'
        
        public string? UserId { get; set; }
        
        public string? AssignedArea { get; set; } // Area A, B, C
        
        /// <summary>
        /// PO Context for hierarchical lock tracking - Format: "PO_{POId}"
        /// Used for PALLET/PCS items to link them to specific POs
        /// </summary>
        public string? POContext { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string Result { get; set; } = "SUCCESS"; // 'SUCCESS', 'ERROR'
        
        public string? ErrorMessage { get; set; }
    }
}
