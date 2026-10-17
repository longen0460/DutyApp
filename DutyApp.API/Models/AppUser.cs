namespace DutyApp.API.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Zam"; // Admin | Starosta | Zam
}