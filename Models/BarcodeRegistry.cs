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
        
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        
        public string Status { get; set; } = "ACTIVE"; // 'ACTIVE', 'SCANNED'
        
        // Navigation properties
        public virtual UploadSession Session { get; set; } = null!;
    }
}
