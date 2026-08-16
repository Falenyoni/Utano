namespace Utano.Module.Notifications.Configuration;

public class ResendSettings
{
    public string ApiKey { get; set; } = null!;
    public string FromEmail { get; set; } = "Utano <notifications@usenemihealth.com>";
}
