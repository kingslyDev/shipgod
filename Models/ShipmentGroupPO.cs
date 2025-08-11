using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("ShipmentGroupPOs")]
public class ShipmentGroupPO
{
    [Key]
    public int Id { get; set; }
    public int ShipmentGroupId { get; set; }
    public ShipmentGroup ShipmentGroup { get; set; } = null!;
    public int POId { get; set; }
    public PO PO { get; set; } = null!;
    public int QtyContribution { get; set; }
}
