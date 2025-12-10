using Application.Analysis.DTOs;
using Application.Response;
using Domain.Entities;

namespace Application.Analysis.Interfaces;

public interface IAnalysisInputValidator
{
    (List<Error> errors, List<Trade> validTrades) Validate(List<AnalysisDto> dtos);
}
