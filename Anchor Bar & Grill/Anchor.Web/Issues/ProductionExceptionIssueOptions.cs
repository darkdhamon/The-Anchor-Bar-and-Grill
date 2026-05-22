namespace Anchor.Web.Issues;

public sealed class ProductionExceptionIssueOptions
{
    public const string SectionName = "ProductionExceptionIssues";

    public bool Enabled { get; set; }

    public string TitlePrefix { get; set; } = "Production Exception";

    public string ProjectStatusName { get; set; } = "Backlog";

    public string[] Labels { get; set; } = ["bug"];

    public int DuplicateSuppressionWindowMinutes { get; set; } = 15;
}
