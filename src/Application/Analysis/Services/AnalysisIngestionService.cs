using Application.Analysis.DTOs;
using Application.Analysis.Exceptions;
using Application.Analysis.Interfaces;

namespace Application.Analysis.Services;

public class AnalysisIngestionService : IAnalysisIngestionService
{

    private readonly IAnalysisInputValidator _inputValidator;
    private readonly IJobIdGenerator _idGen;
    private readonly IAnalysisJobStore _store;
    private readonly IAnalysisQueue _queue;

    public AnalysisIngestionService(
            IAnalysisInputValidator inputValidator,
            IJobIdGenerator idGen,
            IAnalysisJobStore store,
            IAnalysisQueue queue)
    {
        _inputValidator = inputValidator;
        _idGen = idGen;
        _store = store;
        _queue = queue;
    }

    public async Task<AnalyzeAcceptedResult> IngestAsync(List<AnalysisDto> dtos, CancellationToken ct)
    {
        var (errors, trades) = _inputValidator.Validate(dtos);

        if (errors.Count == 1 && errors[0].Index == -1 && errors[0].Field == "payload")
            throw new ArgumentException(errors[0].Message);

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var jobId = _idGen.NewId();
        _store.InitJob(jobId, trades.Count);

        await _queue.EnqueueManyAsync(jobId, trades, ct);

        return new AnalyzeAcceptedResult(jobId, trades.Count);
    }
}
