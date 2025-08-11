using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipmentFinishGood.Models;

[Table("POs")]
public class PO
{
    [Key]
    public int Id { get; set; }
    [Required,MaxLength(30)]
    public string NoPO { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Destination { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
