using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Tests;

public sealed class SeederAndMaintenanceTests
{
    [Fact]
    public void Seeder_InsertsExpectedCounts()
    {
        using var hub = new TempHub();
        var sampleDir = Path.Combine(hub.Root, "sample-notes");
        var result = hub.Seeder.Seed(sampleDir);
        Assert.True(result.Inserted);
        Assert.Equal(10, hub.Notes.Count(includeArchived: true));
        Assert.Equal(9, hub.Notes.Count(includeArchived: false));
        Assert.Equal(6, hub.Tasks.GetAll().Count);
        Assert.Equal(3, hub.Files.Count());
        Assert.Equal(3, hub.Notebooks.GetAll().Count);

        var second = hub.Seeder.Seed(sampleDir);
        Assert.False(second.Inserted);
        Assert.Equal(10, hub.Notes.Count(includeArchived: true));
    }

    [Fact]
    public void Seeder_ThenKeywordSearchWorks()
    {
        using var hub = new TempHub();
        hub.Seeder.Seed(Path.Combine(hub.Root, "sample-notes"));
        var hits = hub.Search.KeywordSearch("fts5");
        Assert.NotEmpty(hits);
    }

    [Fact]
    public void Dashboard_ReflectsSeededData()
    {
        using var hub = new TempHub();
        hub.Seeder.Seed(Path.Combine(hub.Root, "sample-notes"));
        var summary = hub.Dashboard.GetSummary();
        Assert.Equal(9, summary.NoteCount);
        Assert.True(summary.PinnedNoteCount >= 1);
        Assert.Equal(1, summary.ArchivedNoteCount);
        Assert.Equal(3, summary.FileCount);
        Assert.True(summary.TaskTodoCount >= 1);
        Assert.NotEmpty(summary.RecentNotes);
        Assert.NotEmpty(summary.DueTasks);
    }

    [Fact]
    public void Backup_CreatesCopy_AndVacuumRuns()
    {
        using var hub = new TempHub();
        hub.Notebooks.Insert("X");
        hub.Maintenance.Vacuum();
        var backup = hub.Maintenance.Backup();
        Assert.True(File.Exists(backup));
        Assert.True(new FileInfo(backup).Length > 0);
    }

    [Fact]
    public void AppPaths_CreatesFolders()
    {
        using var hub = new TempHub();
        Assert.True(Directory.Exists(hub.Paths.VaultDirectory));
        Assert.True(Directory.Exists(hub.Paths.BackupDirectory));
        Assert.Equal("hub.db", Path.GetFileName(hub.Paths.DatabasePath));
    }
}
