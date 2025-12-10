using Application.Analysis.DTOs;
using FluentValidation;

namespace WebApi.DTOs.Validators;

public class TradeDtoValidator : AbstractValidator<TradeDto>
{
    public TradeDtoValidator(ISet<string> allowedSectors)
    {
        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Valor {PropertyValue} inválido. Deve ser maior do que 0.");

        RuleFor(x => x.ClientSector)
            .NotEmpty().WithMessage("Valor de clientSector é obrigatório.")
            .Must(s => allowedSectors.Contains(s))
            .WithMessage(_ => "Valor {PropertyValue} inválido.")
            .MaximumLength(100).WithMessage("Valor deve ter até 100 caracteres.");
        
    }
}
