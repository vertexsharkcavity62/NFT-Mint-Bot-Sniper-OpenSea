namespace NftSniper.Models;

public record ListingResult
{
    public required string ContractAddress { get; init; }
    public int TokenId { get; init; }
    public required string Marketplace { get; init; }
    public decimal ListPrice { get; init; }
    public string? ListingUrl { get; init; }
    public ListingStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset ListedAt { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan? ExpiresIn { get; init; }

    public bool IsListed => Status is ListingStatus.Active;
}

public enum ListingStatus
{
    Pending,
    Active,
    Expired,
    Cancelled,
    Sold,
    Failed
}
