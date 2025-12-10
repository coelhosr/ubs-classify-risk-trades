
using System.Diagnostics;
using Application.Risks.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Risks.Services;

public class RiskSummaryService(IRiskClassifier classifier) : IRiskSummaryService
{
    public (IEnumerable<RiskCategory> categories, RiskSummary summary) Analyze(IEnumerable<Trade> trades)
    {
        var sw = Stopwatch.StartNew();
        var categories = new List<RiskCategory>();
        var summary = new RiskSummary();
        var perClient = new Dictionary<(RiskCategory, string?), decimal>();

        foreach (var trade in trades)
        {
            var c = classifier.Classify(trade);
            categories.Add(c);

            if (!summary.Summary.TryGetValue(c, out var cat))
            {
                cat = new RiskSummary.CategorySummary
                {
                    TopValue = trade.Value,
                    TopClient = trade.ClientId
                };
                summary.Summary[c] = cat;
            }
            else if (trade.Value > cat.TopValue)
            {
                cat.TopValue = trade.Value;
                cat.TopClient = trade.ClientId;
            }

            cat.Count++;
            cat.TotalValue += trade.Value;

            if (!string.IsNullOrEmpty(trade.ClientId))
            {
                var key = (c, trade.ClientId!);
                perClient[key] = perClient.GetValueOrDefault(key) + trade.Value;
            }
        }

        sw.Stop();
        summary.ProcessingTimeMs = sw.ElapsedMilliseconds;
        return (categories, summary);
    }
}
