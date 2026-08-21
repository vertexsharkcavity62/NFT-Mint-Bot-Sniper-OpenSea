using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace NftSniper.Analysis;

public sealed class WhitelistChecker(ILogger<WhitelistChecker> logger)
{
    public bool VerifyMerkleProof(string leaf, string root, IReadOnlyList<string> proof)
    {
        var current = NormalizeHash(leaf);

        foreach (var sibling in proof)
        {
            var siblingBytes = Convert.FromHexString(NormalizeHash(sibling));
            var currentBytes = Convert.FromHexString(current);

            byte[] combined = string.Compare(current, NormalizeHash(sibling), StringComparison.Ordinal) < 0
                ? [.. currentBytes, .. siblingBytes]
                : [.. siblingBytes, .. currentBytes];

            current = Convert.ToHexString(SHA256.HashData([.. combined])).ToLowerInvariant();
        }

        var match = current.Equals(NormalizeHash(root), StringComparison.OrdinalIgnoreCase);
        logger.LogDebug("Merkle proof verification: leaf={Leaf} root={Root} result={Match}",
            leaf[..10], root[..10], match);
        return match;
    }

    public string ComputeLeaf(string address)
    {
        var addressBytes = Convert.FromHexString(NormalizeHash(address));
        var padded = new byte[32];
        Array.Copy(addressBytes, 0, padded, 32 - addressBytes.Length, addressBytes.Length);
        return Convert.ToHexString(SHA256.HashData(padded)).ToLowerInvariant();
    }

    public async Task<bool> CheckOnChainWhitelist(HttpClient http, string rpcUrl, string contractAddress, string wallet)
    {
        var calldata = "0x9b19251a" + wallet.Replace("0x", "").PadLeft(64, '0');
        var payload = new { jsonrpc = "2.0", id = 1, method = "eth_call",
            @params = new object[] { new { to = contractAddress, data = calldata }, "latest" } };

        var response = await http.PostAsJsonAsync(rpcUrl, payload);
        var json = await response.Content.ReadAsStringAsync();

        var isWhitelisted = json.Contains("0x0000000000000000000000000000000000000000000000000000000000000001");
        logger.LogInformation("On-chain whitelist check: contract={Addr} wallet={Wallet} result={Result}",
            contractAddress[..10], wallet[..10], isWhitelisted);
        return isWhitelisted;
    }

    private static string NormalizeHash(string hex) =>
        hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..].ToLowerInvariant() : hex.ToLowerInvariant();
}
