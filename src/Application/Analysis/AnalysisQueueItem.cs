using Domain.Entities;

namespace Application.Analysis;

public sealed record AnalysisQueueItem(string JobId, Trade Trade);
