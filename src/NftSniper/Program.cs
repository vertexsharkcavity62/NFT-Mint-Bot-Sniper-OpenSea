using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NftSniper.Analysis;
using NftSniper.Blockchain;
using NftSniper.Config;
using NftSniper.Core;
using NftSniper.Filters;
using NftSniper.Marketplace;
using NftSniper.Notifications;
using NftSniper.Utils;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var sniperConfig = new SniperConfig();
if (!sniperConfig.IsValid())
{
    Console.Error.WriteLine("Invalid configuration. Check environment variables.");
    return 1;
}

builder.Services.AddSingleton(sniperConfig);
builder.Services.AddSingleton<WalletPool>();
builder.Services.AddSingleton<QueueManager>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ContractDecoder>();
builder.Services.AddSingleton<MintFunctionDetector>();
builder.Services.AddSingleton<WhitelistChecker>();
builder.Services.AddSingleton<RarityPredictor>();
builder.Services.AddSingleton<MetadataParser>();

builder.Services.AddSingleton<GasBooster>();
builder.Services.AddSingleton<WalletRotator>();
builder.Services.AddSingleton<ContractDeployListener>(sp => new ContractDeployListener(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<ContractDeployListener>>()));
builder.Services.AddSingleton<MintCaller>(sp => new MintCaller(
    sniperConfig,
    sp.GetRequiredService<GasBooster>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<MintCaller>>()));

builder.Services.AddSingleton<ContractAnalyzer>(sp => new ContractAnalyzer(
    sp.GetRequiredService<ContractDecoder>(),
    sp.GetRequiredService<MintFunctionDetector>(),
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<ContractAnalyzer>>()));

builder.Services.AddSingleton<OpenSeaClient>(sp => new OpenSeaClient(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<OpenSeaClient>>()));
builder.Services.AddSingleton<BlurClient>(sp => new BlurClient(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<BlurClient>>()));
builder.Services.AddSingleton<AutoLister>();

builder.Services.AddSingleton<CollectionFilter>();
builder.Services.AddSingleton<PriceFilter>();
builder.Services.AddSingleton<CreatorFilter>(sp => new CreatorFilter(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<CreatorFilter>>()));

builder.Services.AddSingleton<DiscordWebhook>(sp => new DiscordWebhook(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<DiscordWebhook>>()));
builder.Services.AddSingleton<TelegramAlert>(sp => new TelegramAlert(
    sniperConfig,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    sp.GetRequiredService<ILogger<TelegramAlert>>()));

builder.Services.AddSingleton<MintExecutor>();
builder.Services.AddHostedService<SniperEngine>();

var app = builder.Build();
await app.RunAsync();
return 0;
