using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;

namespace SmartKnowledgeHub.Tests;

public sealed class DbInitializerTests
{
    [Fact]
    public void EnsureCreated_CreatesExpectedTables()
    {
        using var hub = new TempHub();
        using var conn = hub.Factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type IN ('table','trigger') ORDER BY name;";
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        Assert.Contains("Notebooks", names);
        Assert.Contains("Notes", names);
        Assert.Contains("Tags", names);
        Assert.Contains("NoteTags", names);
        Assert.Contains("Tasks", names);
        Assert.Contains("Files", names);
        Assert.Contains("SearchIndex", names);
        Assert.Contains("NotesFts", names);
        Assert.Contains("TasksFts", names);
        Assert.Contains("FilesFts", names);
        Assert.Contains("notes_ai", names);
        Assert.Contains("tasks_ai", names);
    }

    [Fact]
    public void EnsureCreated_IsIdempotent()
    {
        using var hub = new TempHub();
        DbInitializer.EnsureCreated(hub.Factory);
        DbInitializer.EnsureCreated(hub.Factory);
        Assert.True(File.Exists(hub.Factory.DatabasePath));
    }

    [Fact]
    public void Open_EnablesWalAndForeignKeys()
    {
        using var hub = new TempHub();
        using var conn = hub.Factory.Open();
        using var wal = conn.CreateCommand();
        wal.CommandText = "PRAGMA journal_mode;";
        var mode = (string)wal.ExecuteScalar()!;
        Assert.Equal("wal", mode.ToLowerInvariant());

        using var fk = conn.CreateCommand();
        fk.CommandText = "PRAGMA foreign_keys;";
        var enabled = Convert.ToInt32(fk.ExecuteScalar());
        Assert.Equal(1, enabled);
    }
}
