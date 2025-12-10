using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Risks.Rules;

public class LowRiskRule : IRiskRule
{
    public RiskCategory Category => RiskCategory.LOWRISK;
    public bool IsMatch(Trade trade) => trade.Value < 1_000_000m;
}
