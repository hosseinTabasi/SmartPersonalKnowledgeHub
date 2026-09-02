namespace SmartKnowledgeHub.Core.Models;

public sealed class Note
{
    public long Id { get; set; }
    public long NotebookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string NotebookName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}
