using Domain.Entities;
using Domain.Enums;

namespace Application.Risks.Interfaces;

public interface IRiskSummaryService
{
    (IEnumerable<RiskCategory> categories, RiskSummary summary) Analyze(IEnumerable<Trade> trades);
}
