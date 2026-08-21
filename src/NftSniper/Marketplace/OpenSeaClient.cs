using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Marketplace;

public sealed class OpenSeaClient(SniperConfig config, HttpClient http, ILogger<OpenSeaClient> logger)
{
    private const string BaseUrl = "https://api.opensea.io/api/v2";

    public async Task<ListingResult> CreateListing(string contractAddress, int tokenId, decimal priceEth, CancellationToken ct)
    {
        ConfigureHeaders();
        var payload = new
        {
            listing = new
            {
                protocol_address = "0x00000000000000ADc04C56Bf30aC9d3c0aAF14dC",
                parameters = new
                {
                    offerer = config.OpenSeaApiKey[..10],
                    offer = new[] { new { token = contractAddress, identifierOrCriteria = tokenId.ToString(), amount = "1" } },
                    consideration = new[] { new { amount = ((long)(priceEth * 1_000_000_000_000_000_000m)).ToString("x") } },
                    startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    endTime = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds().ToString()
                }
            }
        };

        try
        {
            var response = await http.PostAsJsonAsync($"{BaseUrl}/orders/ethereum/seaport/listings", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var url = $"https://opensea.io/assets/ethereum/{contractAddress}/{tokenId}";
                logger.LogInformation("Listed on OpenSea: {Url} at {Price} ETH", url, priceEth);
                return new ListingResult
                {
                    ContractAddress = contractAddress, TokenId = tokenId,
                    Marketplace = "OpenSea", ListPrice = priceEth,
                    ListingUrl = url, Status = ListingStatus.Active
                };
            }

            logger.LogWarning("OpenSea listing failed: {Status} {Body}", response.StatusCode, body[..Math.Min(200, body.Length)]);
            return new ListingResult
            {
                ContractAddress = contractAddress, TokenId = tokenId,
                Marketplace = "OpenSea", Status = ListingStatus.Failed, ErrorMessage = body[..Math.Min(200, body.Length)]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenSea API error for {Contract}/{Token}", contractAddress[..10], tokenId);
            return new ListingResult
            {
                ContractAddress = contractAddress, TokenId = tokenId,
                Marketplace = "OpenSea", Status = ListingStatus.Failed, ErrorMessage = ex.Message
            };
        }
    }

    public async Task<decimal> GetFloorPrice(string contractAddress, CancellationToken ct)
    {
        ConfigureHeaders();
        var response = await http.GetAsync($"{BaseUrl}/collections/{contractAddress}/stats", ct);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var floor = json?.RootElement.GetProperty("total").GetProperty("floor_price").GetDecimal() ?? 0;
        return floor;
    }

    private void ConfigureHeaders()
    {
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(config.OpenSeaApiKey))
            http.DefaultRequestHeaders.Add("X-API-KEY", config.OpenSeaApiKey);
    }
}
