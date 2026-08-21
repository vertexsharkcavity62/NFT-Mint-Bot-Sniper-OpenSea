using Microsoft.Extensions.Logging;
using NftSniper.Blockchain;
using NftSniper.Config;
using NftSniper.Models;
using NftSniper.Notifications;

namespace NftSniper.Core;

public sealed class MintExecutor(
    MintCaller mintCaller,
    WalletRotator walletRotator,
    GasBooster gasBooster,
    SniperConfig config,
    DiscordWebhook discord,
    TelegramAlert telegram,
    ILogger<MintExecutor> logger)
{
    public async Task<MintResult> Execute(NftContract contract, CancellationToken ct)
    {
        var wallet = walletRotator.GetNextAvailable(config.MaxMintsPerWallet);
        if (wallet is null)
        {
            logger.LogWarning("No wallets available for minting {Addr}", contract.Address[..10]);
            return new MintResult
            {
                ContractAddress = contract.Address, WalletAddress = "none",
                Status = MintStatus.Failed, ErrorMessage = "No wallets available"
            };
        }

        var selector = contract.MintFunctionSelectors.FirstOrDefault() ?? "a0712d68";
        var quantity = Math.Min(contract.MaxPerWallet > 0 ? contract.MaxPerWallet : 1, config.MaxMintsPerWallet);

        if (!gasBooster.IsGasProfitable(gasBooster.GetLastBaseFee(), contract.MintPrice, contract.MintPrice * 2))
            logger.LogWarning("Gas may not be profitable for {Addr}, proceeding anyway", contract.Address[..10]);

        logger.LogInformation("Executing mint: contract={Addr} wallet={Wallet} qty={Qty}",
            contract.Address[..10], wallet.Address[..10], quantity);

        var result = await mintCaller.ExecuteMint(contract, wallet, selector, quantity, contract.MintPrice, ct);

        if (result.IsSuccess)
        {
            walletRotator.RecordMint(wallet.Address);
            walletRotator.SetCooldown(wallet.Address, TimeSpan.FromSeconds(30));
        }

        if (config.NotificationsEnabled)
            await NotifyResult(result, contract, ct);

        return result;
    }

    private async Task NotifyResult(MintResult result, NftContract contract, CancellationToken ct)
    {
        var emoji = result.IsSuccess ? "✅" : "❌";
        var message = $"{emoji} Mint {result.Status}: {contract.Address[..10]} | " +
                      $"Tx: {result.TransactionHash?[..14] ?? "none"} | " +
                      $"Cost: {result.TotalCostEth:F4} ETH | Latency: {result.Latency.TotalMilliseconds:F0}ms";

        await Task.WhenAll(
            discord.SendAsync(message, ct),
            telegram.SendAsync(message, ct));
    }
}
