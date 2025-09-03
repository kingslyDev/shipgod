using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    /// <summary>
    /// Tracks which PO within a session a user is currently locked to
    /// </summary>
    public class UserPOLock
    {
        [Key]
        public int POLockId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int SessionLockId { get; set; }
        
        [Required]
        public int POId { get; set; }
        
        [Required]
        public string NoPO { get; set; } = string.Empty;
        
        public DateTime LockedAt { get; set; } = DateTime.Now;
        
        public DateTime? UnlockedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? CompletedAt { get; set; }
        
        public string? CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual UserSessionLock SessionLock { get; set; } = null!;
        public virtual POMaster POMaster { get; set; } = null!;
    }
}
