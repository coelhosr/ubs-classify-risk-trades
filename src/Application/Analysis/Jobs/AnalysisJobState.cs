using Application.Risks;
using Domain.Entities;
using Domain.Enums;
using System.Diagnostics;

namespace Application.Analysis.Jobs;

public sealed class AnalysisJobState
{
    public long Total { get; init; }
    public long Processed { get; private set; }
    public bool Completed { get; private set; }

    public List<RiskCategory> Categories { get; } = [];
    public RiskSummary Summary { get; } = new()
    {
        Summary = []
    };

    private readonly Stopwatch _sw = new();
    public void Start() => _sw.Start();

    public void Register(RiskCategory cat, Trade trade)
    {
        Categories.Add(cat);

        if (!Summary.Summary.TryGetValue(cat, out var cs))
        {
            cs = new RiskSummary.CategorySummary { TopValue = trade.Value, TopClient = trade.ClientId };
            Summary.Summary[cat] = cs;
        }
        else if (trade.Value > cs.TopValue)
        {
            cs.TopValue = trade.Value;
            cs.TopClient = trade.ClientId;
        }

        cs.Count++;
        cs.TotalValue += trade.Value;

        Processed++;
        if (Processed >= Total && !Completed)
        {
            _sw.Stop();
            Summary.ProcessingTimeMs = _sw.ElapsedMilliseconds;
            Completed = true;
        }
    }
}
