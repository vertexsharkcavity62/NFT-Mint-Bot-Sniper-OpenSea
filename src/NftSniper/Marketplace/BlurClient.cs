using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NftSniper.Config;
using NftSniper.Models;

namespace NftSniper.Marketplace;

public sealed class BlurClient(SniperConfig config, HttpClient http, ILogger<BlurClient> logger)
{
    private const string BaseUrl = "https://core-api.prod.blur.io/v1";

    public async Task<ListingResult> CreateListing(string contractAddress, int tokenId, decimal priceEth, CancellationToken ct)
    {
        ConfigureHeaders();
        var payload = new
        {
            collection = contractAddress,
            tokenId = tokenId.ToString(),
            price = new { amount = priceEth.ToString("F18"), unit = "ETH" },
            expirationTime = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
        };

        try
        {
            var response = await http.PostAsJsonAsync($"{BaseUrl}/listings", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var url = $"https://blur.io/asset/{contractAddress}/{tokenId}";
                logger.LogInformation("Listed on Blur: {Url} at {Price} ETH", url, priceEth);
                return new ListingResult
                {
                    ContractAddress = contractAddress, TokenId = tokenId,
                    Marketplace = "Blur", ListPrice = priceEth,
                    ListingUrl = url, Status = ListingStatus.Active, ExpiresIn = TimeSpan.FromDays(30)
                };
            }

            logger.LogWarning("Blur listing failed: {Code} {Body}", response.StatusCode, body[..Math.Min(200, body.Length)]);
            return new ListingResult
            {
                ContractAddress = contractAddress, TokenId = tokenId,
                Marketplace = "Blur", Status = ListingStatus.Failed, ErrorMessage = body[..Math.Min(200, body.Length)]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Blur API error for {Contract}/{Token}", contractAddress[..10], tokenId);
            return new ListingResult
            {
                ContractAddress = contractAddress, TokenId = tokenId,
                Marketplace = "Blur", Status = ListingStatus.Failed, ErrorMessage = ex.Message
            };
        }
    }

    public async Task<decimal> GetFloorPrice(string contractAddress, CancellationToken ct)
    {
        ConfigureHeaders();
        var response = await http.GetAsync($"{BaseUrl}/collections/{contractAddress}", ct);
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var floor = json?.RootElement.GetProperty("collection").GetProperty("floorPrice").GetDecimal() ?? 0;
        return floor;
    }

    private void ConfigureHeaders()
    {
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(config.BlurApiKey))
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.BlurApiKey}");
    }
}
