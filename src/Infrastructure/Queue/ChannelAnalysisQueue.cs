using Application.Analysis;
using Application.Analysis.Interfaces;
using Domain.Entities;
using System.Threading.Channels;

namespace Infrastructure.Queue;

public sealed class ChannelAnalysisQueue : IAnalysisQueue
{
    private readonly Channel<AnalysisQueueItem> _channel;
    public ChannelAnalysisQueue(Channel<AnalysisQueueItem> channel) => _channel = channel;

    public Task EnqueueAsync(AnalysisQueueItem item, CancellationToken ct) => _channel.Writer.WriteAsync(item, ct).AsTask();

    public async Task EnqueueManyAsync(string jobId, IEnumerable<Trade> trades, CancellationToken ct)
    {
        foreach (var trade in trades)
            await _channel.Writer.WriteAsync(new AnalysisQueueItem(jobId, trade), ct);
    }
}