using Domain.Entities;
using Domain.Enums;

namespace Application.Risks.Interfaces;

public interface IRiskClassifier
{
    IEnumerable<RiskCategory> ClassifyMany(IEnumerable<Trade> trades);

    RiskCategory Classify(Trade trade);
}
