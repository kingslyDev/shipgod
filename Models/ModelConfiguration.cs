using System.ComponentModel.DataAnnotations;

namespace ShipmentFinishGood.Models
{
    public class ModelConfiguration
    {
        [Key]
        public int ConfigId { get; set; }
        [Required]
        public string ModelName { get; set; } = string.Empty;
        public int PcsPerPallet { get; set; }
        public int PcsPerBox { get; set; }
        public string Type { get; set; } = string.Empty; // LOOSE atau PALLET
        public string Description { get; set; } = string.Empty;
    }
}
