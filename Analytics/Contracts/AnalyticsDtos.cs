namespace Analytics.Contracts;

public sealed class AnalyticsSummaryDto
{
    public int Total { get; set; }
    public int New { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Cancelled { get; set; }
    public double? AverageCompletionDays { get; set; }
}

public sealed class ProjectAnalyticsDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Total { get; set; }
    public int New { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public int Cancelled { get; set; }
}
