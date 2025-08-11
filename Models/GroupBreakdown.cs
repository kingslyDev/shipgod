using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("GroupBreakdowns")]
public class GroupBreakdown
{
    [Key]
    public int ShipmentGroupId { get; set; }
    public ShipmentGroup ShipmentGroup { get; set; } = null!;
    public int QtyTotal { get; set; }
    public int QtyPallet { get; set; }
    public int QtyBox { get; set; }
    public int QtyPcs { get; set; }
    public int PcsPerPallet { get; set; }
    public int PcsPerBox { get; set; }
    public bool IntegrityOk { get; set; }
}
