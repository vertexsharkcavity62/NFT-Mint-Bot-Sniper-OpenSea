using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Filters;

public sealed class CreatorFilter(SniperConfig config, HttpClient http, ILogger<CreatorFilter> logger)
{
    private readonly Dictionary<string, double> _scoreCache = new(StringComparer.OrdinalIgnoreCase);

    public bool IsCreatorAllowed(NftContract contract)
    {
        if (config.BlockedCreators.Length == 0)
            return true;

        var blocked = config.BlockedCreators.Any(b =>
            b.Equals(contract.DeployerAddress, StringComparison.OrdinalIgnoreCase));

        if (blocked)
            logger.LogInformation("Creator {Addr} is on the blocklist — skipping", contract.DeployerAddress[..10]);

        return !blocked;
    }

    public async Task<double> GetCreatorScore(string deployerAddress, CancellationToken ct)
    {
        if (_scoreCache.TryGetValue(deployerAddress, out var cached))
            return cached;

        var score = await FetchDeploymentHistory(deployerAddress, ct);
        _scoreCache[deployerAddress] = score;
        return score;
    }

    public async Task<bool> MeetsMinimumScore(NftContract contract, CancellationToken ct)
    {
        var score = await GetCreatorScore(contract.DeployerAddress, ct);
        var meets = score >= (double)config.MinCreatorScore;

        logger.LogDebug("Creator {Addr} score={Score:F2} min={Min} pass={Pass}",
            contract.DeployerAddress[..10], score, config.MinCreatorScore, meets);

        return meets;
    }

    private async Task<double> FetchDeploymentHistory(string deployer, CancellationToken ct)
    {
        try
        {
            var payload = new { jsonrpc = "2.0", id = 1, method = "eth_getTransactionCount",
                @params = new object[] { deployer, "latest" } };

            var response = await http.PostAsJsonAsync(config.RpcUrl, payload, ct);
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var countHex = json?.RootElement.GetProperty("result").GetString() ?? "0x0";
            var txCount = Convert.ToInt64(countHex.Replace("0x", ""), 16);

            return txCount switch
            {
                > 500 => 0.9,
                > 100 => 0.7,
                > 20 => 0.5,
                > 5 => 0.3,
                _ => 0.1
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch creator score for {Addr}", deployer[..10]);
            return 0.5;
        }
    }
}
