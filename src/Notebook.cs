namespace SmartKnowledgeHub.Core.Models;

public sealed class Notebook
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public int NoteCount { get; set; }
}
