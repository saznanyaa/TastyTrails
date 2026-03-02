namespace TastyTrails.Models.DTOs;

public class RegisterDto
{
    public string Name { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}