using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Filters;

public sealed class PriceFilter(SniperConfig config, ILogger<PriceFilter> logger)
{
    public bool IsWithinBudget(NftContract contract)
    {
        if (contract.MintPrice <= 0)
        {
            logger.LogDebug("Free mint detected for {Addr} — allowed", contract.Address[..10]);
            return true;
        }

        if (contract.MintPrice > config.MaxMintPriceEth)
        {
            logger.LogDebug("Contract {Addr} mint price {Price} exceeds max {Max}",
                contract.Address[..10], contract.MintPrice, config.MaxMintPriceEth);
            return false;
        }

        return true;
    }

    public decimal EstimateTotalCost(NftContract contract, int quantity, decimal currentGasGwei)
    {
        var mintCost = contract.MintPrice * quantity;
        var gasCost = currentGasGwei * 200_000m / 1_000_000_000m * quantity;
        var total = mintCost + gasCost;

        logger.LogDebug("Cost estimate for {Addr}: mint={Mint:F4} gas={Gas:F4} total={Total:F4} ETH",
            contract.Address[..10], mintCost, gasCost, total);

        return total;
    }

    public bool IsProfitable(decimal mintCost, decimal estimatedFloorPrice) =>
        estimatedFloorPrice > mintCost * 1.1m;
}
