using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Configuration;

namespace Utano.Module.Notifications.Infrastructure.Services;

public class ResendEmailSender(
    HttpClient httpClient,
    IOptions<ResendSettings> settings,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("emails", new
        {
            from = settings.Value.FromEmail,
            to = new[] { toEmail },
            subject,
            html = htmlBody,
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Resend send failed ({Status}): {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to send email.");
        }
    }
}
