using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Blockchain;

public sealed class MintCaller(SniperConfig config, GasBooster gasBooster, HttpClient http, ILogger<MintCaller> logger)
{
    public async Task<MintResult> ExecuteMint(NftContract contract, WalletEntry wallet, string functionSelector,
        int quantity, decimal mintPriceEth, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var gasPrice = gasBooster.CalculateOptimalGas(30m);
        var valueWei = (long)(mintPriceEth * 1_000_000_000_000_000_000m);
        var calldata = BuildCalldata(functionSelector, wallet.Address, quantity);

        var txPayload = new
        {
            jsonrpc = "2.0", id = 1, method = "eth_sendTransaction",
            @params = new[] { new {
                from = wallet.Address,
                to = contract.Address,
                gas = "0x30D40",
                gasPrice = $"0x{(long)(gasPrice * 1_000_000_000):x}",
                value = $"0x{valueWei:x}",
                data = calldata
            }}
        };

        try
        {
            var response = await http.PostAsJsonAsync(config.RpcUrl, txPayload, ct);
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var result = json?.RootElement.GetProperty("result").GetString();

            stopwatch.Stop();
            if (result is not null && result.StartsWith("0x"))
            {
                logger.LogInformation("Mint tx sent: {Hash} for {Contract}", result[..14], contract.Address[..10]);
                return new MintResult
                {
                    ContractAddress = contract.Address, WalletAddress = wallet.Address,
                    TransactionHash = result, GasPrice = gasPrice,
                    TotalCost = mintPriceEth, Status = MintStatus.Submitted, Latency = stopwatch.Elapsed
                };
            }

            var error = json?.RootElement.TryGetProperty("error", out var err) == true ? err.GetRawText() : "unknown";
            logger.LogWarning("Mint rejected: {Error}", error);
            return new MintResult
            {
                ContractAddress = contract.Address, WalletAddress = wallet.Address,
                Status = MintStatus.Failed, ErrorMessage = error, Latency = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Mint call failed for {Contract}", contract.Address[..10]);
            return new MintResult
            {
                ContractAddress = contract.Address, WalletAddress = wallet.Address,
                Status = MintStatus.Failed, ErrorMessage = ex.Message, Latency = stopwatch.Elapsed
            };
        }
    }

    private static string BuildCalldata(string selector, string address, int quantity)
    {
        var addr = address.Replace("0x", "").PadLeft(64, '0');
        var qty = quantity.ToString("x").PadLeft(64, '0');
        return "0x" + selector + addr + qty;
    }
}
