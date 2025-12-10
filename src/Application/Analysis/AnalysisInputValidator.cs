using Application.Analysis.DTOs;
using Application.Analysis.Interfaces;
using Application.Response;
using Domain.Entities;
using FluentValidation;

namespace Application.Analysis;

public class AnalysisInputValidator : IAnalysisInputValidator
{
    private readonly IValidator<AnalysisDto> _validator;

    public AnalysisInputValidator(IValidator<AnalysisDto> validator) => _validator = validator;

    public (List<Error> errors, List<Trade> validTrades) Validate(List<AnalysisDto> dtos)
    {
        var errors = new List<Error>(dtos?.Count ?? 0);
        var trades = new List<Trade>(dtos?.Count ?? 0);

        if (dtos is null || dtos.Count == 0)
            return (new List<Error> { new(-1, "payload", "Nenhum dado foi informado.") }, trades);

        for (int i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var res = _validator.Validate(dto);
            if (!res.IsValid)
                foreach (var e in res.Errors)
                    errors.Add(new Error(i, e.PropertyName, e.ErrorMessage));
            else
                trades.Add(new Trade(dto.Value, dto.ClientSector, dto.ClientId));
        }

        return (errors, trades);
    }
}
