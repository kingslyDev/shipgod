using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    /// <summary>
    /// Tracks user locks to specific POs within a session
    /// Enables hierarchical locking: Session → PO → BOX scanning
    /// </summary>
    public class POLock
    {
        [Key]
        public int POLockId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int SessionId { get; set; }
        
        [Required]
        public int POId { get; set; }
        
        public DateTime LockedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
        
        public string? LockedBy { get; set; } // Who locked (for audit)
        
        // Navigation properties
        public virtual UploadSession Session { get; set; } = null!;
        public virtual POMaster PO { get; set; } = null!;
    }
}
