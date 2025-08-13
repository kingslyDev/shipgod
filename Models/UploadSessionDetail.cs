using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class UploadSessionDetail
    {
        [Key]
        public int DetailId { get; set; }
        
        public int SessionId { get; set; }
        
        public string? OriginalPO { get; set; }
        
        public string? Model { get; set; }
        
        public int OriginalQty { get; set; }
        
        public int RowIndex { get; set; }
        
        // NEW: Country field
        public string? Country { get; set; }
        
        // Navigation properties
        public virtual UploadSession Session { get; set; } = null!;
    }
}
