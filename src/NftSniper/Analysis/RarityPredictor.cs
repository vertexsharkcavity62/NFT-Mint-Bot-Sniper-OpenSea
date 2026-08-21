using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NftSniper.Analysis;

public sealed class RarityPredictor(ILogger<RarityPredictor> logger)
{
    public RarityScore PredictFromMetadata(IReadOnlyList<JsonElement> tokenMetadataList)
    {
        if (tokenMetadataList.Count == 0)
            return new RarityScore(0, 0, RarityTier.Unknown);

        var traitFrequencies = new Dictionary<string, Dictionary<string, int>>();
        var totalTokens = tokenMetadataList.Count;

        foreach (var meta in tokenMetadataList)
        {
            if (!meta.TryGetProperty("attributes", out var attrs) || attrs.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var attr in attrs.EnumerateArray())
            {
                var traitType = attr.TryGetProperty("trait_type", out var tt) ? tt.GetString() ?? "unknown" : "unknown";
                var value = attr.TryGetProperty("value", out var tv) ? tv.ToString() : "none";

                if (!traitFrequencies.TryGetValue(traitType, out var valueMap))
                {
                    valueMap = new Dictionary<string, int>();
                    traitFrequencies[traitType] = valueMap;
                }
                valueMap[value] = valueMap.GetValueOrDefault(value) + 1;
            }
        }

        var avgTraitCount = traitFrequencies.Count > 0
            ? traitFrequencies.Values.Average(v => v.Count)
            : 0;

        var uniquenessRatio = traitFrequencies.Count > 0
            ? traitFrequencies.Values
                .SelectMany(v => v.Values)
                .Count(c => c == 1) / (double)Math.Max(1, totalTokens)
            : 0;

        var score = Math.Clamp(uniquenessRatio * 100 + avgTraitCount * 2, 0, 100);
        var tier = score switch
        {
            >= 80 => RarityTier.Legendary,
            >= 60 => RarityTier.Epic,
            >= 40 => RarityTier.Rare,
            >= 20 => RarityTier.Uncommon,
            _ => RarityTier.Common
        };

        logger.LogInformation("Rarity prediction: score={Score:F1} tier={Tier} traits={Traits} tokens={Tokens}",
            score, tier, traitFrequencies.Count, totalTokens);

        return new RarityScore(score, traitFrequencies.Count, tier);
    }

    public decimal EstimateFloorMultiplier(RarityTier tier) => tier switch
    {
        RarityTier.Legendary => 5.0m,
        RarityTier.Epic => 3.0m,
        RarityTier.Rare => 1.8m,
        RarityTier.Uncommon => 1.2m,
        RarityTier.Common => 1.0m,
        _ => 1.0m
    };
}

public record RarityScore(double Score, int TraitTypeCount, RarityTier Tier);

public enum RarityTier { Unknown, Common, Uncommon, Rare, Epic, Legendary }
