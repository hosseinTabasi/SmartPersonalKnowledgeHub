using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Tests;

public sealed class NoteRepositoryTests
{
    [Fact]
    public void Insert_Get_Update_Delete_RoundTrip()
    {
        using var hub = new TempHub();
        var notebookId = hub.Notebooks.Insert("Study");
        var note = new Note
        {
            NotebookId = notebookId,
            Title = "FTS5 notes",
            Body = "bm25 ranking",
            Tags = new List<string> { "sqlite", "search" }
        };

        var id = hub.Notes.Insert(note);
        var loaded = hub.Notes.GetById(id);
        Assert.NotNull(loaded);
        Assert.Equal("FTS5 notes", loaded!.Title);
        Assert.Contains("sqlite", loaded.Tags);
        Assert.Equal("Study", loaded.NotebookName);

        loaded.Title = "FTS5 revision";
        loaded.IsPinned = true;
        hub.Notes.Update(loaded);
        var updated = hub.Notes.GetById(id)!;
        Assert.Equal("FTS5 revision", updated.Title);
        Assert.True(updated.IsPinned);

        hub.Notes.SetArchived(id, true);
        Assert.Empty(hub.Notes.GetAll(includeArchived: false));
        Assert.Single(hub.Notes.GetAll(includeArchived: true));

        hub.Notes.Delete(id);
        Assert.Null(hub.Notes.GetById(id));
    }

    [Fact]
    public void Tags_AreReplacedNotAppended()
    {
        using var hub = new TempHub();
        var notebookId = hub.Notebooks.Insert("Personal");
        var note = new Note { NotebookId = notebookId, Title = "Tags", Body = "body", Tags = new List<string> { "a", "b" } };
        var id = hub.Notes.Insert(note);
        note.Id = id;
        note.Tags = new List<string> { "b", "c" };
        hub.Notes.Update(note);
        var names = hub.Tags.GetNamesForNote(id);
        Assert.Equal(new[] { "b", "c" }, names.OrderBy(x => x).ToArray());
    }
}
