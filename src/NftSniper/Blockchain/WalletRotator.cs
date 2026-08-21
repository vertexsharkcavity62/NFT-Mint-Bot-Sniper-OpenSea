using Microsoft.Extensions.Logging;
using NftSniper.Config;

namespace NftSniper.Blockchain;

public sealed class WalletRotator(WalletPool pool, ILogger<WalletRotator> logger)
{
    private int _currentIndex;
    private readonly Dictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _mintCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public WalletEntry? GetNextAvailable(int maxMintsPerWallet)
    {
        lock (_lock)
        {
            if (pool.Count == 0) return null;

            for (var i = 0; i < pool.Count; i++)
            {
                var index = (_currentIndex + i) % pool.Count;
                var wallet = pool.GetByIndex(index)!;

                if (IsOnCooldown(wallet.Address)) continue;

                var count = _mintCounts.GetValueOrDefault(wallet.Address);
                if (count >= maxMintsPerWallet) continue;

                _currentIndex = (index + 1) % pool.Count;
                logger.LogDebug("Selected wallet {Label} ({Address})", wallet.Label, wallet.Address[..10]);
                return wallet;
            }

            logger.LogWarning("No available wallets — all on cooldown or at mint limit");
            return null;
        }
    }

    public void RecordMint(string address)
    {
        lock (_lock)
        {
            _mintCounts[address] = _mintCounts.GetValueOrDefault(address) + 1;
        }
    }

    public void SetCooldown(string address, TimeSpan duration)
    {
        lock (_lock)
        {
            _cooldowns[address] = DateTimeOffset.UtcNow + duration;
            logger.LogDebug("Wallet {Addr} on cooldown for {Sec}s", address[..10], duration.TotalSeconds);
        }
    }

    public void ResetAll()
    {
        lock (_lock)
        {
            _cooldowns.Clear();
            _mintCounts.Clear();
            _currentIndex = 0;
        }
    }

    private bool IsOnCooldown(string address) =>
        _cooldowns.TryGetValue(address, out var until) && DateTimeOffset.UtcNow < until;
}
