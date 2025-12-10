using Application.Analysis.DTOs;
using Application.Analysis.DTOs.Response;
using Application.Analysis.Interfaces;
using System.Collections.Concurrent;

namespace Application.Analysis.Jobs;

public sealed class AnalysisJobStore : IAnalysisJobStore
{
    private readonly ConcurrentDictionary<string, AnalysisJobState> _jobs = new();

    public AnalysisJobState InitJob(string jobId, long total)
    {
        var state = new AnalysisJobState { Total = total };
        state.Start();
        _jobs[jobId] = state;
        return state;
    }

    public bool TryGet(string jobId, out AnalysisJobState? state) => _jobs.TryGetValue(jobId, out state);

    public (bool found, object payload, int statusCode) GetStatusPayload(string jobId)
    {
        if (!TryGet(jobId, out var s) || s is null)
            return (false, new { error = "Job não encontrado." }, 404);

        if (!s.Completed)
        {
            var pct = s.Total == 0 ? 0 : (double)s.Processed / s.Total;
            return (true, new { status = "Processando", processed = s.Processed, total = s.Total, progress = Math.Round(pct * 100, 2) }, 202);
        }
        else
        {
            var response = new AnalysisDtoResponse
            {
                Categories = [.. s.Categories.Select(c => c.ToString().ToUpperInvariant())],
                ProcessingTimeMs = s.Summary.ProcessingTimeMs,
                Summary = s.Summary.Summary.ToDictionary(
                    kv => kv.Key.ToString().ToUpperInvariant(),
                    kv => new CategorySummaryDto { Count = kv.Value.Count, TotalValue = kv.Value.TotalValue, TopClient = kv.Value.TopClient })
            };
            return (true, response, 200);
        }
    }
}
