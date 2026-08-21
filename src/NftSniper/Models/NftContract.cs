namespace NftSniper.Models;

public record NftContract
{
    public required string Address { get; init; }
    public required string DeployerAddress { get; init; }
    public string? Name { get; init; }
    public string? Symbol { get; init; }
    public string? BytecodeHash { get; init; }
    public long BlockNumber { get; init; }
    public DateTimeOffset DiscoveredAt { get; init; } = DateTimeOffset.UtcNow;
    public decimal MintPrice { get; init; }
    public int MaxSupply { get; init; }
    public int MaxPerWallet { get; init; }
    public string? BaseUri { get; init; }
    public List<string> MintFunctionSelectors { get; init; } = [];
    public bool IsWhitelistOnly { get; init; }
    public ContractStatus Status { get; init; } = ContractStatus.Discovered;
}

public enum ContractStatus
{
    Discovered,
    Analyzing,
    Queued,
    Minting,
    Minted,
    Listed,
    Skipped,
    Failed
}
