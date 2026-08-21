using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Marketplace;

public sealed class AutoLister(
    OpenSeaClient openSea,
    BlurClient blur,
    SniperConfig config,
    ILogger<AutoLister> logger)
{
    public async Task<List<ListingResult>> ListMintedToken(MintResult mint, CancellationToken ct)
    {
        if (!config.AutoListEnabled || !mint.IsSuccess)
            return [];

        var listPrice = await CalculateListPrice(mint.ContractAddress, mint.TotalCostEth, ct);
        logger.LogInformation("Auto-listing token {Id} at {Price:F4} ETH on both marketplaces",
            mint.TokenId, listPrice);

        var results = await Task.WhenAll(
            openSea.CreateListing(mint.ContractAddress, mint.TokenId, listPrice, ct),
            blur.CreateListing(mint.ContractAddress, mint.TokenId, listPrice, ct));

        foreach (var result in results)
        {
            var status = result.IsListed ? "SUCCESS" : "FAILED";
            logger.LogInformation("Listing {Market}: {Status} — {Url}",
                result.Marketplace, status, result.ListingUrl ?? "N/A");
        }

        return [.. results];
    }

    private async Task<decimal> CalculateListPrice(string contractAddress, decimal totalCost, CancellationToken ct)
    {
        var openSeaFloor = 0m;
        var blurFloor = 0m;

        try { openSeaFloor = await openSea.GetFloorPrice(contractAddress, ct); }
        catch { logger.LogDebug("Could not fetch OpenSea floor for {Addr}", contractAddress[..10]); }

        try { blurFloor = await blur.GetFloorPrice(contractAddress, ct); }
        catch { logger.LogDebug("Could not fetch Blur floor for {Addr}", contractAddress[..10]); }

        var marketFloor = Math.Max(openSeaFloor, blurFloor);

        if (marketFloor > 0)
            return marketFloor * 0.95m;

        var markup = 1m + (config.AutoListMarkupPercent / 100m);
        return Math.Max(totalCost * markup, 0.01m);
    }
}
