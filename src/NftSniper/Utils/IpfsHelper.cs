namespace NftSniper.Utils;

public static class IpfsHelper
{
    private const string DefaultGateway = "https://ipfs.io/ipfs/";
    private static readonly string[] Gateways =
    [
        "https://ipfs.io/ipfs/",
        "https://cloudflare-ipfs.com/ipfs/",
        "https://gateway.pinata.cloud/ipfs/",
        "https://dweb.link/ipfs/"
    ];

    public static string ResolveToHttp(string uri, int gatewayIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return "";

        var gateway = gatewayIndex >= 0 && gatewayIndex < Gateways.Length
            ? Gateways[gatewayIndex]
            : DefaultGateway;

        if (uri.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
            return gateway + uri[7..];

        if (uri.StartsWith("Qm", StringComparison.Ordinal) || uri.StartsWith("bafy", StringComparison.OrdinalIgnoreCase))
            return gateway + uri;

        if (uri.StartsWith("ar://", StringComparison.OrdinalIgnoreCase))
            return "https://arweave.net/" + uri[5..];

        return uri;
    }

    public static bool IsIpfsUri(string uri) =>
        !string.IsNullOrWhiteSpace(uri) &&
        (uri.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase) ||
         uri.StartsWith("Qm", StringComparison.Ordinal) ||
         uri.StartsWith("bafy", StringComparison.OrdinalIgnoreCase));

    public static string ExtractCid(string uri)
    {
        if (uri.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
            uri = uri[7..];

        var slashIndex = uri.IndexOf('/');
        return slashIndex > 0 ? uri[..slashIndex] : uri;
    }

    public static string BuildTokenUri(string baseUri, int tokenId)
    {
        var resolved = ResolveToHttp(baseUri);
        if (resolved.EndsWith('/'))
            return resolved + tokenId;
        return resolved + "/" + tokenId;
    }

    public static async Task<string> FetchContent(HttpClient http, string uri, CancellationToken ct)
    {
        var url = ResolveToHttp(uri);
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
