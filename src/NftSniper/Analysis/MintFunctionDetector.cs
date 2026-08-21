using Microsoft.Extensions.Logging;

namespace NftSniper.Analysis;

public sealed class MintFunctionDetector(ILogger<MintFunctionDetector> logger)
{
    private static readonly Dictionary<string, string> KnownSelectors = new()
    {
        ["40c10f19"] = "mint(address,uint256)",
        ["a0712d68"] = "mint(uint256)",
        ["6a627842"] = "mint(address)",
        ["1249c58b"] = "mint()",
        ["2ab4d052"] = "publicMint(uint256)",
        ["a723533e"] = "whitelistMint(uint256,bytes32[])",
        ["efef39a1"] = "purchase(uint256)",
        ["4e71d92d"] = "claim()",
        ["3ccfd60b"] = "withdraw()",
        ["d85d3d27"] = "freeMint(uint256)",
        ["26092b83"] = "presaleMint(uint256,bytes32[])",
        ["84bb1e42"] = "mintTo(address)",
    };

    public List<DetectedFunction> DetectFromBytecode(string bytecodeHex)
    {
        var results = new List<DetectedFunction>();
        var normalized = bytecodeHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase);

        foreach (var (selector, signature) in KnownSelectors)
        {
            var index = normalized.IndexOf(selector, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;

            var fn = new DetectedFunction(selector, signature, ClassifyFunction(signature), index);
            results.Add(fn);
            logger.LogInformation("Detected mint function: {Sig} (selector={Sel})", signature, selector);
        }

        if (results.Count == 0)
            logger.LogDebug("No known mint selectors found in bytecode of length {Len}", normalized.Length);

        return results;
    }

    public bool HasPublicMint(IEnumerable<DetectedFunction> functions) =>
        functions.Any(f => f.Category is FunctionCategory.PublicMint or FunctionCategory.FreeMint);

    public bool HasWhitelistMint(IEnumerable<DetectedFunction> functions) =>
        functions.Any(f => f.Category is FunctionCategory.WhitelistMint);

    private static FunctionCategory ClassifyFunction(string signature) => signature switch
    {
        var s when s.Contains("free", StringComparison.OrdinalIgnoreCase) => FunctionCategory.FreeMint,
        var s when s.Contains("whitelist", StringComparison.OrdinalIgnoreCase) => FunctionCategory.WhitelistMint,
        var s when s.Contains("presale", StringComparison.OrdinalIgnoreCase) => FunctionCategory.WhitelistMint,
        var s when s.Contains("claim", StringComparison.OrdinalIgnoreCase) => FunctionCategory.Claim,
        var s when s.Contains("purchase", StringComparison.OrdinalIgnoreCase) => FunctionCategory.Purchase,
        var s when s.Contains("withdraw", StringComparison.OrdinalIgnoreCase) => FunctionCategory.Admin,
        _ => FunctionCategory.PublicMint
    };
}

public record DetectedFunction(string Selector, string Signature, FunctionCategory Category, int BytecodeOffset);

public enum FunctionCategory { PublicMint, FreeMint, WhitelistMint, Claim, Purchase, Admin }
