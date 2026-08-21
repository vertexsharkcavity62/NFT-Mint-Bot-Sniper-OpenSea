namespace NftSniper.Config;

public sealed class SniperConfig
{
    public string RpcUrl { get; init; } = GetEnvOrDefault("ETH_RPC_URL", "https://eth-mainnet.g.alchemy.com/v2/demo");
    public string WssUrl { get; init; } = GetEnvOrDefault("ETH_WSS_URL", "wss://eth-mainnet.g.alchemy.com/v2/demo");
    public string OpenSeaApiKey { get; init; } = GetEnvOrDefault("OPENSEA_API_KEY", "");
    public string BlurApiKey { get; init; } = GetEnvOrDefault("BLUR_API_KEY", "");
    public string DiscordWebhookUrl { get; init; } = GetEnvOrDefault("DISCORD_WEBHOOK_URL", "");
    public string TelegramBotToken { get; init; } = GetEnvOrDefault("TELEGRAM_BOT_TOKEN", "");
    public string TelegramChatId { get; init; } = GetEnvOrDefault("TELEGRAM_CHAT_ID", "");
    public decimal MaxMintPriceEth { get; init; } = decimal.TryParse(GetEnvOrDefault("MAX_MINT_PRICE", "0.1"), out var v) ? v : 0.1m;
    public decimal MaxGasPriceGwei { get; init; } = decimal.TryParse(GetEnvOrDefault("MAX_GAS_GWEI", "100"), out var g) ? g : 100m;
    public decimal GasBoostMultiplier { get; init; } = 1.2m;
    public int MaxMintsPerWallet { get; init; } = 3;
    public int PollIntervalMs { get; init; } = 1000;
    public int MintTimeoutSeconds { get; init; } = 120;
    public decimal AutoListMarkupPercent { get; init; } = 50m;
    public bool AutoListEnabled { get; init; } = true;
    public bool NotificationsEnabled { get; init; } = true;
    public string[] BlockedCreators { get; init; } = [];
    public string[] AllowedCollectionPatterns { get; init; } = ["*"];
    public decimal MinCreatorScore { get; init; } = 0.3m;

    private static string GetEnvOrDefault(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) ?? fallback;

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(RpcUrl) && MaxMintPriceEth > 0 && MaxGasPriceGwei > 0;
}
