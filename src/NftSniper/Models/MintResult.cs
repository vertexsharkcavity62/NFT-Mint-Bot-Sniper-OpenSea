namespace NftSniper.Models;

public record MintResult
{
    public required string ContractAddress { get; init; }
    public required string WalletAddress { get; init; }
    public string? TransactionHash { get; init; }
    public int TokenId { get; init; }
    public decimal GasUsed { get; init; }
    public decimal GasPrice { get; init; }
    public decimal TotalCost { get; init; }
    public MintStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan Latency { get; init; }

    public bool IsSuccess => Status is MintStatus.Confirmed;
    public decimal TotalCostEth => TotalCost + (GasUsed * GasPrice / 1_000_000_000m);
}

public enum MintStatus
{
    Pending,
    Submitted,
    Confirmed,
    Reverted,
    OutOfGas,
    Failed
}
