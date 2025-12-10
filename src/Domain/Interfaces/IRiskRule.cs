using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface IRiskRule
{
    bool IsMatch(Trade trade);
    RiskCategory Category { get; }
}
