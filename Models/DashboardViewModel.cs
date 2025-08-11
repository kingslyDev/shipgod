namespace ShipmentFinishGood.Models;

public class DashboardViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
