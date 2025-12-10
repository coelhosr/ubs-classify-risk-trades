using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Risks;

public class RiskClassifier(IEnumerable<IRiskRule> rules) : Interfaces.IRiskClassifier
{
    private readonly IReadOnlyList<IRiskRule> _rules = [.. rules];

    public IEnumerable<RiskCategory> ClassifyMany(IEnumerable<Trade> trades) => trades.Select(Classify);

    public RiskCategory Classify(Trade trade)
    {
        foreach (var rule in _rules)
        {
            if (rule.IsMatch(trade)) 
                return rule.Category;
        }
        throw new InvalidOperationException("Nenhuma categoria encontrada.");
    }
}
