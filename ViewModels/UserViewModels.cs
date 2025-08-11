using System.ComponentModel.DataAnnotations;
using ShipmentFinishGood.Domain;

namespace ShipmentFinishGood.ViewModels;

public class EditUserViewModel
{
    public int Id { get; set; }

    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Password Baru")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password harus 6 - 100 karakter" )]
    public string? Password { get; set; }

    [Required]
    [Display(Name = "Nama Lengkap")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public UserRole Role { get; set; }
}
