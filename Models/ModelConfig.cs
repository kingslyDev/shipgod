using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("ModelConfigs")]
public class ModelConfig
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(50)]
    public string ModelCode { get; set; } = null!;
    [Required, MaxLength(10)]
    public string Method { get; set; } = null!; // LOOSE / PALLET
    public int PcsPerPallet { get; set; }
    public int PcsPerBox { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}
