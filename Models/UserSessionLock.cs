using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    /// <summary>
    /// Tracks which session a user is currently locked to
    /// </summary>
    public class UserSessionLock
    {
        [Key]
        public int LockId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int SessionId { get; set; }
        
        [Required]
        public string MasterQRCode { get; set; } = string.Empty;
        
        public DateTime LockedAt { get; set; } = DateTime.Now;
        
        public DateTime? UnlockedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string? CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual UploadSession Session { get; set; } = null!;
        public virtual ICollection<UserPOLock> POLocks { get; set; } = new List<UserPOLock>();
    }
}
