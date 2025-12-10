using Application.Analysis.Jobs;

namespace Application.Analysis.Interfaces;

public interface IAnalysisJobStore
{
    AnalysisJobState InitJob(string jobId, long total);
    bool TryGet(string jobId, out AnalysisJobState? state);
    (bool found, object payload, int statusCode) GetStatusPayload(string jobId);
}
