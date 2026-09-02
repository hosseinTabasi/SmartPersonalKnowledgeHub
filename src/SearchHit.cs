namespace SmartKnowledgeHub.Core.Models;

public sealed class SearchHit
{
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Source { get; set; } = string.Empty;
}
