using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;

namespace SmartKnowledgeHub.Core.Services;

public sealed class DashboardService
{
    private readonly INoteRepository _notes;
    private readonly ITaskRepository _tasks;
    private readonly IFileRepository _files;

    public DashboardService(INoteRepository notes, ITaskRepository tasks, IFileRepository files)
    {
        _notes = notes;
        _tasks = tasks;
        _files = files;
    }

    public DashboardSummary GetSummary()
    {
        var due = _tasks.GetDueSoon(7).ToList();
        return new DashboardSummary
        {
            NoteCount = _notes.Count(includeArchived: false),
            PinnedNoteCount = _notes.CountPinned(),
            ArchivedNoteCount = _notes.CountArchived(),
            TaskTodoCount = _tasks.CountByStatus(TaskStatuses.Todo),
            TaskDoingCount = _tasks.CountByStatus(TaskStatuses.Doing),
            TaskDoneCount = _tasks.CountByStatus(TaskStatuses.Done),
            FileCount = _files.Count(),
            DueSoonCount = due.Count,
            RecentNotes = _notes.GetRecent(6).ToList(),
            DueTasks = due
        };
    }
}
