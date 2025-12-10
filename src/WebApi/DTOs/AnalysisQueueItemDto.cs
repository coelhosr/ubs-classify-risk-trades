using Domain.Entities;

namespace WebApi.DTOs;

public sealed record AnalysisQueueItemDto(string JobId, Trade Trade);