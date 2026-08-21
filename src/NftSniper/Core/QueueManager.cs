using NftSniper.Models;

namespace NftSniper.Core;

public sealed class QueueManager
{
    private readonly PriorityQueue<NftContract, int> _queue = new();
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public int Count
    {
        get { lock (_lock) { return _queue.Count; } }
    }

    public bool Enqueue(NftContract contract, int priority = 5)
    {
        lock (_lock)
        {
            if (!_seen.Add(contract.Address))
                return false;

            var adjustedPriority = CalculatePriority(contract, priority);
            _queue.Enqueue(contract, adjustedPriority);
            return true;
        }
    }

    public NftContract? Dequeue()
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out var contract, out _) ? contract : null;
        }
    }

    public NftContract? Peek()
    {
        lock (_lock)
        {
            return _queue.TryPeek(out var contract, out _) ? contract : null;
        }
    }

    public bool HasSeen(string address)
    {
        lock (_lock) { return _seen.Contains(address); }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
            _seen.Clear();
        }
    }

    private static int CalculatePriority(NftContract contract, int basePriority)
    {
        var score = basePriority;
        if (contract.MintPrice == 0) score -= 2;
        if (contract.MaxSupply is > 0 and <= 5000) score -= 1;
        if (contract.IsWhitelistOnly) score += 3;
        if (contract.MintFunctionSelectors.Count > 0) score -= 1;
        return Math.Clamp(score, 0, 10);
    }
}
