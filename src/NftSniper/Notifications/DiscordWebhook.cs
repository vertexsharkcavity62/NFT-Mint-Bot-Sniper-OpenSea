using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;

namespace NftSniper.Notifications;

public sealed class DiscordWebhook(SniperConfig config, HttpClient http, ILogger<DiscordWebhook> logger)
{
    public async Task SendAsync(string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
            return;

        var payload = JsonSerializer.Serialize(new
        {
            content = message,
            username = "NFT Sniper",
            embeds = Array.Empty<object>()
        });

        try
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(config.DiscordWebhookUrl, content, ct);

            if (response.IsSuccessStatusCode)
                logger.LogDebug("Discord notification sent");
            else
                logger.LogWarning("Discord webhook returned {Code}", response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Discord notification");
        }
    }

    public async Task SendEmbedAsync(string title, string description, int color, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
            return;

        var payload = JsonSerializer.Serialize(new
        {
            username = "NFT Sniper",
            embeds = new[] { new { title, description, color, timestamp = DateTimeOffset.UtcNow.ToString("o") } }
        });

        try
        {
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await http.PostAsync(config.DiscordWebhookUrl, content, ct);
            logger.LogDebug("Discord embed sent: {Title}", title);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Discord embed");
        }
    }
}
