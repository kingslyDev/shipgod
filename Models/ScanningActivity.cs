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
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public string Result { get; set; } = "SUCCESS"; // 'SUCCESS', 'ERROR'
        
        public string? ErrorMessage { get; set; }
    }
}
