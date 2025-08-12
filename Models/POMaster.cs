using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class POMaster
    {
        [Key]
        public int POId { get; set; }
        
        [Required]
        public string NoPO { get; set; } = string.Empty;
        
        [Required]
        public string ModelProduk { get; set; } = string.Empty;
        
        public int QtyTotal { get; set; }
        
        public int QtyPallet { get; set; } = 0;
        
        public int QtyBox { get; set; } = 0;
        
        public int QtyPcs { get; set; } = 0;
        
        public string? Container { get; set; }
        
        public string? NoInvoice { get; set; }
        
        public string? ShipmentDetail { get; set; }
        
        public int? SourceSessionId { get; set; }
        
        public string Status { get; set; } = "PENDING"; // 'PENDING', 'SCANNING', 'COMPLETED'
        
        public string? ShipmentMethod { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public string? CreatedBy { get; set; }
        
        // Navigation properties
        public virtual UploadSession? SourceSession { get; set; }
        public virtual ICollection<PODetail> Details { get; set; } = new List<PODetail>();
    }
}
