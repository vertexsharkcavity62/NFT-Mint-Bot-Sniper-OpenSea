using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NftSniper.Blockchain;
using NftSniper.Config;
using NftSniper.Filters;
using NftSniper.Marketplace;
using NftSniper.Models;

namespace NftSniper.Core;

public sealed class SniperEngine(
    ContractDeployListener listener,
    ContractAnalyzer analyzer,
    MintExecutor executor,
    AutoLister autoLister,
    QueueManager queue,
    CollectionFilter collectionFilter,
    PriceFilter priceFilter,
    CreatorFilter creatorFilter,
    SniperConfig config,
    ILogger<SniperEngine> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("SniperEngine started — polling every {Ms}ms", config.PollIntervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var deploys = await listener.PollNewDeployments(ct);
                foreach (var contract in deploys)
                    await ProcessNewContract(contract, ct);

                await ProcessQueue(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "SniperEngine loop error");
            }

            await Task.Delay(config.PollIntervalMs, ct);
        }

        logger.LogInformation("SniperEngine stopped");
    }

    private async Task ProcessNewContract(NftContract contract, CancellationToken ct)
    {
        if (queue.HasSeen(contract.Address)) return;
        if (!collectionFilter.IsAllowed(contract)) return;
        if (!priceFilter.IsWithinBudget(contract)) return;
        if (!creatorFilter.IsCreatorAllowed(contract)) return;

        if (!await creatorFilter.MeetsMinimumScore(contract, ct))
        {
            logger.LogDebug("Creator score too low for {Addr}", contract.Address[..10]);
            return;
        }

        var analysis = await analyzer.Analyze(contract, ct);
        if (!analysis.IsMintable)
        {
            logger.LogDebug("Contract {Addr} not mintable: {Reason}", contract.Address[..10], analysis.Reason);
            return;
        }

        queue.Enqueue(analysis.Contract);
        logger.LogInformation("Queued contract {Addr} — queue size: {Size}", contract.Address[..10], queue.Count);
    }

    private async Task ProcessQueue(CancellationToken ct)
    {
        var contract = queue.Dequeue();
        if (contract is null) return;

        logger.LogInformation("Processing mint for {Addr}", contract.Address[..10]);
        var result = await executor.Execute(contract, ct);

        if (result.IsSuccess)
        {
            logger.LogInformation("Mint successful: {Hash}", result.TransactionHash?[..14]);
            await autoLister.ListMintedToken(result, ct);
        }
        else
        {
            logger.LogWarning("Mint failed for {Addr}: {Error}", contract.Address[..10], result.ErrorMessage);
        }
    }
}
