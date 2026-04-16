namespace TastyTrails.Models;

public class UserPreviewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? ProfileImage { get; set; } // Koristimo tvoj naziv iz modela
}