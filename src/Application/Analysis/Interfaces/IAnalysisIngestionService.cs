using Application.Analysis.DTOs;

namespace Application.Analysis.Interfaces;

public interface IAnalysisIngestionService
{
    Task<AnalyzeAcceptedResult> IngestAsync(List<AnalysisDto> dtos, CancellationToken ct);
}
