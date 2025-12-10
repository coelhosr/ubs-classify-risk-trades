using Application.Analysis;
using Application.Analysis.DTOs;
using Application.Analysis.Interfaces;
using Application.Analysis.Jobs;
using Application.Analysis.Services;
using Application.Risks;
using Application.Risks.Interfaces;
using Application.Risks.Rules;
using Application.Risks.Services;
using Domain.Interfaces;
using FluentValidation;
using Infrastructure.Queue;
using Infrastructure.Workers;
using System.Threading.Channels;
using WebApi.DTOs.Validators;

namespace WebApi.Extensions;

public static class DIExtension
{
    public static IServiceCollection AddDI(this IServiceCollection services)
    {

        services.AddSingleton(sp =>
        {
            var opts = new BoundedChannelOptions(capacity: 50_000)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            };
            return Channel.CreateBounded<AnalysisQueueItem>(opts);
        });

        // Regras/serviços de risco
        services.AddSingleton<IRiskRule, LowRiskRule>();
        services.AddSingleton<IRiskRule, MediumRiskRule>();
        services.AddSingleton<IRiskRule, HighRiskRule>();

        services.AddSingleton<IRiskSummaryService, RiskSummaryService>();
        
        services.AddSingleton<IRiskClassifier, RiskClassifier>();

        services.AddSingleton<IAnalysisJobStore, AnalysisJobStore>();
        services.AddSingleton<IJobIdGenerator, GuidJobIdGenerator>();

        services.AddScoped<IValidator<AnalysisDto>, AnalysisDtoValidator>();

        services.AddSingleton<IAnalysisQueue, ChannelAnalysisQueue>();
        services.AddScoped<IAnalysisInputValidator, AnalysisInputValidator>();
        services.AddScoped<IAnalysisIngestionService, AnalysisIngestionService>();

        services.AddHostedService<AnalysisBatchWorker>();

        return services;
    }
}
