using Microsoft.Extensions.Logging;
using NftSniper.Config;

namespace NftSniper.Blockchain;

public sealed class GasBooster(SniperConfig config, ILogger<GasBooster> logger)
{
    private decimal _lastBaseFee;
    private readonly Lock _lock = new();

    public decimal CalculateOptimalGas(decimal currentBaseFeeGwei, decimal? pendingPoolMedian = null)
    {
        lock (_lock) { _lastBaseFee = currentBaseFeeGwei; }

        var priorityFee = DeterminePriorityFee(currentBaseFeeGwei, pendingPoolMedian);
        var maxFee = (currentBaseFeeGwei * 2) + priorityFee;
        var boosted = maxFee * config.GasBoostMultiplier;
        var capped = Math.Min(boosted, config.MaxGasPriceGwei);

        logger.LogDebug("Gas calc: base={Base} priority={Priority} boosted={Boosted} capped={Capped}",
            currentBaseFeeGwei, priorityFee, boosted, capped);

        return capped;
    }

    public decimal ApplyUrgencyBoost(decimal currentGas, UrgencyLevel urgency) =>
        urgency switch
        {
            UrgencyLevel.Low => currentGas,
            UrgencyLevel.Medium => Math.Min(currentGas * 1.15m, config.MaxGasPriceGwei),
            UrgencyLevel.High => Math.Min(currentGas * 1.35m, config.MaxGasPriceGwei),
            UrgencyLevel.Critical => config.MaxGasPriceGwei,
            _ => currentGas
        };

    public bool IsGasProfitable(decimal gasGwei, decimal mintPriceEth, decimal expectedFloorEth)
    {
        var estimatedGasCostEth = gasGwei * 200_000m / 1_000_000_000m;
        var totalCost = mintPriceEth + estimatedGasCostEth;
        var profit = expectedFloorEth - totalCost;
        return profit > 0;
    }

    public decimal GetLastBaseFee()
    {
        lock (_lock) { return _lastBaseFee; }
    }

    private static decimal DeterminePriorityFee(decimal baseFee, decimal? pendingMedian) =>
        pendingMedian.HasValue
            ? Math.Max(pendingMedian.Value * 1.1m, 2m)
            : baseFee switch
            {
                < 20 => 2m,
                < 50 => 3m,
                < 100 => 5m,
                _ => 8m
            };
}

public enum UrgencyLevel { Low, Medium, High, Critical }
