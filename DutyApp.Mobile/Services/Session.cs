namespace DutyApp.Mobile.Services;

public class Session
{
    public string? Token { get; set; }
    public string? Role { get; set; }
    public string? Login { get; set; }

    public bool IsAdmin => Role == "Admin";
    public bool CanEditGroup => Role == "Admin";

    public void Save()
    {
        Preferences.Set("token", Token);
        Preferences.Set("role", Role);
        Preferences.Set("login", Login);
    }

    public bool Load()
    {
        Token = Preferences.Get("token", null);
        Role = Preferences.Get("role", null);
        Login = Preferences.Get("login", null);
        return !string.IsNullOrEmpty(Token);
    }

    public void Clear()
    {
        Preferences.Remove("token");
        Preferences.Remove("role");
        Preferences.Remove("login");
        Token = null;
        Role = null;
        Login = null;
    }
}