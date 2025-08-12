using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class PODetail
    {
        [Key]
        public int DetailId { get; set; }
        
        public int POId { get; set; }
        
        public string? UnitType { get; set; } // 'PALLET', 'BOX', 'PCS'
        
        public string? Barcode { get; set; }
        
        public int Qty { get; set; } = 1;
        
        public DateTime? ScannedDate { get; set; }
        
        public string? ScannedBy { get; set; }
        
        // Navigation properties
        public virtual POMaster PO { get; set; } = null!;
    }
}
