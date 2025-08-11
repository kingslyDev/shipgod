using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("ShipmentGroups")]
public class ShipmentGroup
{
    [Key]
    public int Id { get; set; }
    [MaxLength(10)]
    public string ShipmentMethod { get; set; } = string.Empty; // LOOSE / PALLET
    [MaxLength(50)]
    public string ModelCode { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;
    public bool MixedPO { get; set; }
    [MaxLength(400)]
    public string? DisplayNoPO { get; set; }
    [MaxLength(300)]
    public string GroupSignature { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Calculated, Barcoded, Completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(60)]
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public GroupBreakdown? Breakdown { get; set; }
    public ICollection<ShipmentGroupPO> POs { get; set; } = new List<ShipmentGroupPO>();
}
