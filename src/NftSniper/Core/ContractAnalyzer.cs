using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Analysis;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Core;

public sealed class ContractAnalyzer(
    ContractDecoder decoder,
    MintFunctionDetector mintDetector,
    SniperConfig config,
    HttpClient http,
    ILogger<ContractAnalyzer> logger)
{
    public async Task<AnalysisResult> Analyze(NftContract contract, CancellationToken ct)
    {
        logger.LogInformation("Analyzing contract {Address}...", contract.Address[..10]);

        var bytecode = await FetchBytecode(contract.Address, ct);
        if (string.IsNullOrEmpty(bytecode) || bytecode.Length < 20)
            return new AnalysisResult(contract, false, "No bytecode found");

        var decoded = decoder.Decode(bytecode);
        if (decoded.TokenStandard is TokenStandard.Unknown)
            return new AnalysisResult(contract, false, "Not an NFT contract");

        var mintFunctions = mintDetector.DetectFromBytecode(bytecode);
        if (mintFunctions.Count == 0)
            return new AnalysisResult(contract, false, "No mint functions detected");

        var hasPublicMint = mintDetector.HasPublicMint(mintFunctions);
        var hasWhitelistMint = mintDetector.HasWhitelistMint(mintFunctions);

        var updatedContract = contract with
        {
            MintFunctionSelectors = mintFunctions.Select(f => f.Selector).ToList(),
            IsWhitelistOnly = hasWhitelistMint && !hasPublicMint,
            Status = ContractStatus.Queued
        };

        logger.LogInformation("Analysis complete: {Addr} standard={Std} functions={Count} publicMint={Pub}",
            contract.Address[..10], decoded.TokenStandard, mintFunctions.Count, hasPublicMint);

        return new AnalysisResult(updatedContract, true, null, decoded, mintFunctions);
    }

    private async Task<string> FetchBytecode(string address, CancellationToken ct)
    {
        var payload = new { jsonrpc = "2.0", id = 1, method = "eth_getCode",
            @params = new object[] { address, "latest" } };

        var response = await http.PostAsJsonAsync(config.RpcUrl, payload, ct);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
        return json?.RootElement.GetProperty("result").GetString() ?? "";
    }
}

public record AnalysisResult(
    NftContract Contract,
    bool IsMintable,
    string? Reason,
    DecodedContract? DecodedContract = null,
    List<DetectedFunction>? MintFunctions = null);
