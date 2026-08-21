using Microsoft.Extensions.Logging;

namespace NftSniper.Analysis;

public sealed class ContractDecoder(ILogger<ContractDecoder> logger)
{
    private static readonly byte[] Erc721InterfaceId = [0x80, 0xac, 0x58, 0xcd];
    private static readonly byte[] Erc1155InterfaceId = [0xd9, 0xb6, 0x7a, 0x26];

    public DecodedContract Decode(string bytecodeHex)
    {
        var clean = bytecodeHex.Replace("0x", "", StringComparison.OrdinalIgnoreCase);
        var bytes = Convert.FromHexString(clean.Length % 2 == 0 ? clean : "0" + clean);

        var standard = DetectTokenStandard(clean);
        var hasOwnable = clean.Contains("8da5cb5b", StringComparison.OrdinalIgnoreCase);
        var hasMintGuard = clean.Contains("a22cb465", StringComparison.OrdinalIgnoreCase);
        var selectors = ExtractFunctionSelectors(bytes);

        logger.LogInformation("Decoded contract: standard={Std} selectors={Count} ownable={Own}",
            standard, selectors.Count, hasOwnable);

        return new DecodedContract
        {
            TokenStandard = standard,
            FunctionSelectors = selectors,
            HasOwnable = hasOwnable,
            HasMintGuard = hasMintGuard,
            BytecodeSize = bytes.Length,
            EstimatedComplexity = EstimateComplexity(bytes.Length, selectors.Count)
        };
    }

    private static TokenStandard DetectTokenStandard(string hex)
    {
        var has721 = hex.Contains(Convert.ToHexString(Erc721InterfaceId), StringComparison.OrdinalIgnoreCase);
        var has1155 = hex.Contains(Convert.ToHexString(Erc1155InterfaceId), StringComparison.OrdinalIgnoreCase);
        return (has721, has1155) switch
        {
            (true, _) => TokenStandard.Erc721,
            (_, true) => TokenStandard.Erc1155,
            _ => TokenStandard.Unknown
        };
    }

    private static List<string> ExtractFunctionSelectors(byte[] bytecode)
    {
        var selectors = new HashSet<string>();
        for (var i = 0; i < bytecode.Length - 4; i++)
        {
            if (bytecode[i] != 0x63) continue;
            var selector = Convert.ToHexString(bytecode[(i + 1)..(i + 5)]).ToLowerInvariant();
            if (selector is not ("00000000" or "ffffffff"))
                selectors.Add(selector);
        }
        return [.. selectors];
    }

    private static int EstimateComplexity(int size, int selectorCount) =>
        (size, selectorCount) switch
        {
            ( < 5000, _) => 1,
            ( < 15000, < 20) => 2,
            ( < 30000, _) => 3,
            _ => 4
        };
}

public record DecodedContract
{
    public TokenStandard TokenStandard { get; init; }
    public List<string> FunctionSelectors { get; init; } = [];
    public bool HasOwnable { get; init; }
    public bool HasMintGuard { get; init; }
    public int BytecodeSize { get; init; }
    public int EstimatedComplexity { get; init; }
}

public enum TokenStandard { Unknown, Erc721, Erc1155, Erc20 }
