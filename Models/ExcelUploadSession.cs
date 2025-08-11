using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models
{
    [Table("ExcelUploadSessions")]
    public class ExcelUploadSession
    {
        [Key]
        public int SessionId { get; set; }
        [Required, MaxLength(200)]
        public string FileName { get; set; } = string.Empty;
        [Required, MaxLength(20)]
        public string Status { get; set; } = "PREVIEW"; // PREVIEW, COMPLETED
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedDate { get; set; }
        [MaxLength(100)]
        public string? CreatedBy { get; set; }
        public virtual ICollection<ExcelUploadItem> Items { get; set; } = new List<ExcelUploadItem>();
    }

    [Table("ExcelUploadItems")]
    public class ExcelUploadItem
    {
        [Key]
        public int ItemId { get; set; }
        [ForeignKey("Session")]
        public int SessionId { get; set; }
        public ExcelUploadSession Session { get; set; } = null!;
        [MaxLength(50)]
        public string NoPO { get; set; } = string.Empty;
        [MaxLength(50)]
        public string ModelProduk { get; set; } = string.Empty;
        public int QtyTotal { get; set; }
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;
        [MaxLength(50)]
        public string SheetName { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public bool IsSelected { get; set; } = false;
        public bool IsProcessed { get; set; } = false;
        [MaxLength(10)]
        public string? MethodPlanned { get; set; } // LOOSE/PALLET yang dipilih user
        public int? ProcessedPOId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
