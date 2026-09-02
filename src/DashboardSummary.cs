namespace SmartKnowledgeHub.Core.Models;

public sealed class DashboardSummary
{
    public int NoteCount { get; set; }
    public int PinnedNoteCount { get; set; }
    public int ArchivedNoteCount { get; set; }
    public int TaskTodoCount { get; set; }
    public int TaskDoingCount { get; set; }
    public int TaskDoneCount { get; set; }
    public int FileCount { get; set; }
    public int DueSoonCount { get; set; }
    public List<Note> RecentNotes { get; set; } = new();
    public List<TaskItem> DueTasks { get; set; } = new();
}
