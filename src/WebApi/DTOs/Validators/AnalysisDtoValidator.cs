using Application.Analysis.DTOs;
using FluentValidation;

namespace WebApi.DTOs.Validators;

public class AnalysisDtoValidator : AbstractValidator<AnalysisDto>
{
    public AnalysisDtoValidator(ISet<string> allowedSectors)
    {
        Include(new TradeDtoValidator(allowedSectors));

        RuleFor(x => x.ClientId)
            .MaximumLength(50).WithMessage("ClientId deve ter no máximo 50 caracteres.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("ClientId aceita apenas letras, números e hífen.")
            .When(x => !string.IsNullOrWhiteSpace(x.ClientId));
    }
}
