using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Tests;

public sealed class TaskRepositoryTests
{
    [Fact]
    public void Insert_And_FilterByStatus()
    {
        using var hub = new TempHub();
        hub.Tasks.Insert(new TaskItem { Title = "Write notes", Status = TaskStatuses.Todo, Priority = TaskPriorities.High });
        hub.Tasks.Insert(new TaskItem { Title = "Review PR", Status = TaskStatuses.Doing, Priority = TaskPriorities.Normal });
        hub.Tasks.Insert(new TaskItem { Title = "Done item", Status = TaskStatuses.Done, Priority = TaskPriorities.Low });

        Assert.Single(hub.Tasks.GetAll(TaskStatuses.Todo));
        Assert.Equal(1, hub.Tasks.CountByStatus(TaskStatuses.Doing));
        Assert.Equal(3, hub.Tasks.GetAll().Count);
    }

    [Fact]
    public void Task_CanLinkToNote()
    {
        using var hub = new TempHub();
        var notebookId = hub.Notebooks.Insert("Study");
        var noteId = hub.Notes.Insert(new Note { NotebookId = notebookId, Title = "Linked", Body = "body" });
        var id = hub.Tasks.Insert(new TaskItem { Title = "Follow up", NoteId = noteId, Status = TaskStatuses.Todo });
        var loaded = hub.Tasks.GetById(id);
        Assert.Equal(noteId, loaded!.NoteId);
        Assert.Equal("Linked", loaded.LinkedNoteTitle);
    }

    [Fact]
    public void GetDueSoon_ExcludesDoneAndFuture()
    {
        using var hub = new TempHub();
        var today = DateTime.UtcNow.Date;
        hub.Tasks.Insert(new TaskItem { Title = "Soon", DueUtc = today.AddDays(2), Status = TaskStatuses.Todo });
        hub.Tasks.Insert(new TaskItem { Title = "Later", DueUtc = today.AddDays(20), Status = TaskStatuses.Todo });
        hub.Tasks.Insert(new TaskItem { Title = "Done soon", DueUtc = today.AddDays(1), Status = TaskStatuses.Done });
        var due = hub.Tasks.GetDueSoon(7);
        Assert.Single(due);
        Assert.Equal("Soon", due[0].Title);
    }
}
