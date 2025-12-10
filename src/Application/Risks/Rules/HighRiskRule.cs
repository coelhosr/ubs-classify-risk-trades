using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Risks.Rules;

public class HighRiskRule : IRiskRule
{
    public RiskCategory Category => RiskCategory.HIGHRISK;
    public bool IsMatch(Trade trade) => trade.Value >= 1_000_000m && trade.ClientSector.Equals("Private", StringComparison.OrdinalIgnoreCase);
}
