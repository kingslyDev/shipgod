using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("BarcodeUnits")]
public class BarcodeUnit
{
    [Key]
    public long Id { get; set; }
    public int ShipmentGroupId { get; set; }
    public ShipmentGroup ShipmentGroup { get; set; } = null!;
    [MaxLength(10)]
    public string BarcodeType { get; set; } = string.Empty; // PALLET/BOX/PCS
    public int SequenceNo { get; set; }
    public int Qty { get; set; }
    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;
    public bool IsScanned { get; set; }
    public DateTime? ScannedAt { get; set; }
    [MaxLength(60)]
    public string? ScannedBy { get; set; }
}
