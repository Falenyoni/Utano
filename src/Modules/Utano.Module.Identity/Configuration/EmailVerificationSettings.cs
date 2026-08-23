namespace Utano.Module.Identity.Configuration;

public class EmailVerificationSettings
{
    // Far more generous than the password-reset window (30 min) since there's no security
    // sensitivity here - just a courtesy so a signup that checks email a day later doesn't
    // hit a needlessly expired link and have to ask for a resend.
    public int ExpiryMinutes { get; set; } = 1440;
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
