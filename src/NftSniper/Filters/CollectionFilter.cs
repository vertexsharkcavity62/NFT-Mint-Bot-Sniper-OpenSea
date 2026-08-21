using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Filters;

public sealed class CollectionFilter(SniperConfig config, ILogger<CollectionFilter> logger)
{
    public bool IsAllowed(NftContract contract)
    {
        if (config.AllowedCollectionPatterns.Length == 0)
            return true;

        var name = contract.Name ?? "";

        if (config.AllowedCollectionPatterns is ["*"])
        {
            logger.LogDebug("Collection filter: wildcard — allowing {Addr}", contract.Address[..10]);
            return true;
        }

        foreach (var pattern in config.AllowedCollectionPatterns)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            if (Regex.IsMatch(name, regex, RegexOptions.IgnoreCase))
            {
                logger.LogDebug("Collection {Name} matched pattern {Pattern}", name, pattern);
                return true;
            }
        }

        logger.LogDebug("Collection {Name} ({Addr}) did not match any allowed pattern", name, contract.Address[..10]);
        return false;
    }

    public List<NftContract> FilterBatch(IEnumerable<NftContract> contracts) =>
        contracts.Where(IsAllowed).ToList();
}
