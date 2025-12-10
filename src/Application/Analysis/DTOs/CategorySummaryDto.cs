namespace Application.Analysis.DTOs;

public class CategorySummaryDto
{
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
    public string? TopClient { get; set; }
}
