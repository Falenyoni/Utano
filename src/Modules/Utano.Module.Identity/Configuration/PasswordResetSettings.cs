namespace Utano.Module.Identity.Configuration;

public class PasswordResetSettings
{
    public int ExpiryMinutes { get; set; } = 30;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
