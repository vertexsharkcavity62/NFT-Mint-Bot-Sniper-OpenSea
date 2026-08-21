using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Blockchain;

public sealed class ContractDeployListener(SniperConfig config, HttpClient http, ILogger<ContractDeployListener> logger)
{
    public async Task<List<NftContract>> PollNewDeployments(CancellationToken ct)
    {
        var contracts = new List<NftContract>();
        var blockPayload = new { jsonrpc = "2.0", id = 1, method = "eth_blockNumber", @params = Array.Empty<object>() };

        var blockResp = await http.PostAsJsonAsync(config.RpcUrl, blockPayload, ct);
        var blockJson = await blockResp.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var blockHex = blockJson?.RootElement.GetProperty("result").GetString() ?? "0x0";
        var blockNumber = Convert.ToInt64(blockHex.Replace("0x", ""), 16);

        var txPayload = new { jsonrpc = "2.0", id = 2, method = "eth_getBlockByNumber",
            @params = new object[] { $"0x{blockNumber:x}", true } };

        var txResp = await http.PostAsJsonAsync(config.RpcUrl, txPayload, ct);
        var txJson = await txResp.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var txs = txJson?.RootElement.GetProperty("result").GetProperty("transactions");

        if (txs is null || txs.Value.ValueKind != JsonValueKind.Array) return contracts;

        foreach (var tx in txs.Value.EnumerateArray())
        {
            var to = tx.TryGetProperty("to", out var toProp) ? toProp.GetString() : null;
            if (to is not null) continue;

            var from = tx.GetProperty("from").GetString() ?? "";
            var hash = tx.GetProperty("hash").GetString() ?? "";
            var input = tx.TryGetProperty("input", out var inp) ? inp.GetString() ?? "" : "";

            if (input.Length < 100) continue;

            var contractAddr = DeriveContractAddress(from, blockNumber);
            contracts.Add(new NftContract
            {
                Address = contractAddr,
                DeployerAddress = from,
                BlockNumber = blockNumber,
                BytecodeHash = hash,
                Status = ContractStatus.Discovered
            });

            logger.LogInformation("New deploy detected: {Addr} by {From} at block {Block}",
                contractAddr, from[..10], blockNumber);
        }

        return contracts;
    }

    private static string DeriveContractAddress(string deployer, long nonce)
    {
        var deployerBytes = Convert.FromHexString(deployer.Replace("0x", "").PadLeft(40, '0'));
        var nonceBytes = BitConverter.GetBytes(nonce);
        var combined = new byte[deployerBytes.Length + nonceBytes.Length];
        deployerBytes.CopyTo(combined, 0);
        nonceBytes.CopyTo(combined, deployerBytes.Length);
        var hash = System.Security.Cryptography.SHA256.HashData(combined);
        return "0x" + Convert.ToHexString(hash[12..32]).ToLowerInvariant();
    }
}
