using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;

namespace Application.Risks.Rules;

public class MediumRiskRule : IRiskRule
{
    public RiskCategory Category => RiskCategory.MEDIUMRISK;
    public bool IsMatch(Trade trade) => trade.Value >= 1_000_000m && trade.ClientSector.Equals("Public", StringComparison.OrdinalIgnoreCase);
}
