namespace Application.Analysis.DTOs.Response;

public class AnalysisDtoResponse : ClassificationDtoResponse
{
    public Dictionary<string, CategorySummaryDto> Summary { get; set; } = [];
    public long ProcessingTimeMs { get; set; }
}
