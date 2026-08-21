using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NftSniper.Utils;

public sealed class MetadataParser(ILogger<MetadataParser> logger)
{
    public NftMetadata Parse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
            var description = root.TryGetProperty("description", out var d) ? d.GetString() : null;
            var image = root.TryGetProperty("image", out var img) ? img.GetString() : null;
            var externalUrl = root.TryGetProperty("external_url", out var ext) ? ext.GetString() : null;
            var animationUrl = root.TryGetProperty("animation_url", out var anim) ? anim.GetString() : null;

            var attributes = new List<TraitAttribute>();
            if (root.TryGetProperty("attributes", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
            {
                foreach (var attr in attrs.EnumerateArray())
                {
                    var traitType = attr.TryGetProperty("trait_type", out var tt) ? tt.GetString() ?? "" : "";
                    var value = attr.TryGetProperty("value", out var tv) ? tv.ToString() : "";
                    var displayType = attr.TryGetProperty("display_type", out var dt) ? dt.GetString() : null;
                    attributes.Add(new TraitAttribute(traitType, value, displayType));
                }
            }

            var resolvedImage = image is not null ? IpfsHelper.ResolveToHttp(image) : null;

            logger.LogDebug("Parsed metadata: name={Name} traits={Count} image={HasImage}",
                name, attributes.Count, resolvedImage is not null);

            return new NftMetadata(name, description, resolvedImage, externalUrl, animationUrl, attributes);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse NFT metadata JSON");
            return new NftMetadata(null, null, null, null, null, []);
        }
    }

    public async Task<NftMetadata> FetchAndParse(HttpClient http, string tokenUri, CancellationToken ct)
    {
        var json = await IpfsHelper.FetchContent(http, tokenUri, ct);
        return Parse(json);
    }
}

public record NftMetadata(
    string? Name,
    string? Description,
    string? ImageUrl,
    string? ExternalUrl,
    string? AnimationUrl,
    List<TraitAttribute> Attributes);

public record TraitAttribute(string TraitType, string Value, string? DisplayType);
