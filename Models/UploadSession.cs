using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class UploadSession
    {
        [Key]
        public int SessionId { get; set; }
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        public string SheetName { get; set; } = string.Empty;
        
        public string? ShipmentType { get; set; } // 'LOOSE' atau 'PALLET'
        
        public string Status { get; set; } = "PREVIEW"; // 'PREVIEW', 'PROCESSED'
        
        public string? IdentityQRCode { get; set; }
        
        public DateTime UploadDate { get; set; } = DateTime.Now;
        
        public string? UploadedBy { get; set; }
        
        // Additional fields
        public string? SheetIdentifier { get; set; } // SH001, SH002
        
        public string? MasterBarcode { get; set; }
        
        public int TotalBoxes { get; set; } = 0;
        
        public DateTime? ShipmentDate { get; set; }
        
        // File hash for duplicate detection
        public string? FileHash { get; set; }
        
        // NEW: Country support
        public string? Country { get; set; } // Single country for processed sessions
        public string? Countries { get; set; } // Comma-separated countries for preview sessions
        public int? ParentSessionId { get; set; } // Link to original session for country submissions
        
        // Navigation properties
        public virtual ICollection<UploadSessionDetail> Details { get; set; } = new List<UploadSessionDetail>();
        public virtual ICollection<POMaster> POMasters { get; set; } = new List<POMaster>();
        public virtual UploadSession? ParentSession { get; set; }
        public virtual ICollection<UploadSession> ChildSessions { get; set; } = new List<UploadSession>();
    }
}
