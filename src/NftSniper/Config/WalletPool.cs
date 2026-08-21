namespace NftSniper.Config;

public sealed class WalletPool
{
    private readonly List<WalletEntry> _wallets = [];

    public WalletPool()
    {
        var raw = Environment.GetEnvironmentVariable("WALLET_KEYS") ?? "";
        foreach (var segment in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split(':', 2);
            var key = parts[0].Trim();
            var label = parts.Length > 1 ? parts[1].Trim() : $"wallet-{_wallets.Count}";
            if (key.Length >= 64)
                _wallets.Add(new WalletEntry(key, label, DeriveAddress(key)));
        }
    }

    public int Count => _wallets.Count;
    public IReadOnlyList<WalletEntry> All => _wallets.AsReadOnly();

    public WalletEntry? GetByIndex(int index) =>
        index >= 0 && index < _wallets.Count ? _wallets[index] : null;

    public WalletEntry? GetByLabel(string label) =>
        _wallets.Find(w => w.Label.Equals(label, StringComparison.OrdinalIgnoreCase));

    private static string DeriveAddress(string privateKey)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            Convert.FromHexString(privateKey.TrimStart('0', 'x').PadLeft(64, '0')[..64]));
        return "0x" + Convert.ToHexString(hash[..20]).ToLowerInvariant();
    }
}

public record WalletEntry(string PrivateKey, string Label, string Address);
