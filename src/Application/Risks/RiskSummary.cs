using Domain.Enums;
using System.Text.Json.Serialization;

namespace Application.Risks;

public class RiskSummary
{
    public Dictionary<RiskCategory, CategorySummary> Summary { get; set; } = [];
    public long ProcessingTimeMs { get; set; }
    public class CategorySummary
    {
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public string? TopClient { get; set; }
        
        [JsonIgnore]
        public decimal TopValue { get; set; }
    }
}
