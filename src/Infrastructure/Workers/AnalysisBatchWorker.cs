using Application.Analysis;
using Application.Analysis.Interfaces;
using Application.Risks.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Infrastructure.Workers;

public sealed class AnalysisBatchWorker(
    Channel<AnalysisQueueItem> channel,
    IAnalysisJobStore store,
    IRiskClassifier classifier,
    ILogger<AnalysisBatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int BATCH_SIZE = 2;
        var buffer = new List<AnalysisQueueItem>(BATCH_SIZE);

        await foreach (var item in channel.Reader.ReadAllAsync(stoppingToken))
        {
            buffer.Add(item);
            if (buffer.Count >= BATCH_SIZE)
            {
                await ProcessBatchAsync(buffer);
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
            await ProcessBatchAsync(buffer);
    }

    private Task ProcessBatchAsync(List<AnalysisQueueItem> batch)
    {
        try
        {
            foreach (var qi in batch)
            {
                var cat = classifier.Classify(qi.Trade);
                if (store.TryGet(qi.JobId, out var state))
                    state!.Register(cat, qi.Trade);
            }
            logger.LogInformation("Lote processado: {Count} itens", batch.Count);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar lote de {Count}", batch.Count);
            return Task.CompletedTask;
        }
    }
}

