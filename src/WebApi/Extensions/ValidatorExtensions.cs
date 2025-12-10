using FluentValidation;
using WebApi.DTOs.Validators;

namespace WebApi.Extensions;

public static class ValidatorExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<TradeDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<AnalysisDtoValidator>();

        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        return services;
    }
}
