using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;

namespace NftSniper.Notifications;

public sealed class TelegramAlert(SniperConfig config, HttpClient http, ILogger<TelegramAlert> logger)
{
    private const string BaseUrl = "https://api.telegram.org/bot";

    public async Task SendAsync(string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramBotToken) || string.IsNullOrWhiteSpace(config.TelegramChatId))
            return;

        var url = $"{BaseUrl}{config.TelegramBotToken}/sendMessage";
        var payload = new { chat_id = config.TelegramChatId, text = message, parse_mode = "HTML" };

        try
        {
            var response = await http.PostAsJsonAsync(url, payload, ct);
            if (response.IsSuccessStatusCode)
                logger.LogDebug("Telegram alert sent");
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Telegram API returned {Code}: {Body}",
                    response.StatusCode, body[..Math.Min(200, body.Length)]);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram alert");
        }
    }

    public async Task SendWithButtonsAsync(string message, Dictionary<string, string> buttons, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramBotToken) || string.IsNullOrWhiteSpace(config.TelegramChatId))
            return;

        var keyboard = buttons.Select(b => new { text = b.Key, url = b.Value }).ToArray();
        var url = $"{BaseUrl}{config.TelegramBotToken}/sendMessage";
        var payload = new
        {
            chat_id = config.TelegramChatId,
            text = message,
            parse_mode = "HTML",
            reply_markup = new { inline_keyboard = new[] { keyboard } }
        };

        try
        {
            await http.PostAsJsonAsync(url, payload, ct);
            logger.LogDebug("Telegram alert with buttons sent");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram alert with buttons");
        }
    }
}
