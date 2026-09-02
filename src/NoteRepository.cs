using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public sealed class NoteRepository : INoteRepository
{
    private readonly SqliteConnectionFactory _factory;
    private readonly ITagRepository _tags;

    public NoteRepository(SqliteConnectionFactory factory, ITagRepository tags)
    {
        _factory = factory;
        _tags = tags;
    }

    public IReadOnlyList<Note> GetAll(bool includeArchived = false, long? notebookId = null)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.NotebookId, n.Title, n.Body, n.IsPinned, n.IsArchived,
                   n.CreatedUtc, n.UpdatedUtc, b.Name AS NotebookName
            FROM Notes n
            INNER JOIN Notebooks b ON b.Id = n.NotebookId
            WHERE ($includeArchived = 1 OR n.IsArchived = 0)
              AND ($notebookId = 0 OR n.NotebookId = $notebookId)
            ORDER BY n.IsPinned DESC, n.UpdatedUtc DESC;
            """;
        cmd.Parameters.AddWithValue("$includeArchived", includeArchived ? 1 : 0);
        cmd.Parameters.AddWithValue("$notebookId", notebookId ?? 0);
        return ReadAll(cmd);
    }

    public IReadOnlyList<Note> GetRecent(int take = 5)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.NotebookId, n.Title, n.Body, n.IsPinned, n.IsArchived,
                   n.CreatedUtc, n.UpdatedUtc, b.Name AS NotebookName
            FROM Notes n
            INNER JOIN Notebooks b ON b.Id = n.NotebookId
            WHERE n.IsArchived = 0
            ORDER BY n.UpdatedUtc DESC
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$take", take);
        return ReadAll(cmd);
    }

    public Note? GetById(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.NotebookId, n.Title, n.Body, n.IsPinned, n.IsArchived,
                   n.CreatedUtc, n.UpdatedUtc, b.Name AS NotebookName
            FROM Notes n
            INNER JOIN Notebooks b ON b.Id = n.NotebookId
            WHERE n.Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var note = Map(reader);
        reader.Close();
        note.Tags = _tags.GetNamesForNote(id).ToList();
        return note;
    }

    public long Insert(Note note)
    {
        var now = DateTime.UtcNow;
        note.CreatedUtc = now;
        note.UpdatedUtc = now;
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Notes (NotebookId, Title, Body, IsPinned, IsArchived, CreatedUtc, UpdatedUtc)
            VALUES ($notebookId, $title, $body, $pinned, $archived, $created, $updated);
            SELECT last_insert_rowid();
            """;
        Bind(cmd, note);
        var id = (long)cmd.ExecuteScalar()!;
        note.Id = id;
        _tags.ReplaceNoteTags(id, note.Tags);
        return id;
    }

    public void Update(Note note)
    {
        note.UpdatedUtc = DateTime.UtcNow;
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Notes
            SET NotebookId = $notebookId,
                Title = $title,
                Body = $body,
                IsPinned = $pinned,
                IsArchived = $archived,
                UpdatedUtc = $updated
            WHERE Id = $id;
            """;
        Bind(cmd, note);
        cmd.Parameters.AddWithValue("$id", note.Id);
        cmd.ExecuteNonQuery();
        _tags.ReplaceNoteTags(note.Id, note.Tags);
    }

    public void Delete(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Notes WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void SetPinned(long id, bool isPinned) => SetFlag(id, "IsPinned", isPinned);

    public void SetArchived(long id, bool isArchived) => SetFlag(id, "IsArchived", isArchived);

    public int Count(bool includeArchived = true)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = includeArchived
            ? "SELECT COUNT(*) FROM Notes;"
            : "SELECT COUNT(*) FROM Notes WHERE IsArchived = 0;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int CountPinned()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Notes WHERE IsPinned = 1 AND IsArchived = 0;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int CountArchived()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Notes WHERE IsArchived = 1;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void SetFlag(long id, string column, bool value)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE Notes SET {column} = $val, UpdatedUtc = $utc WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$val", value ? 1 : 0);
        cmd.Parameters.AddWithValue("$utc", Utc.Format(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private List<Note> ReadAll(SqliteCommand cmd)
    {
        var list = new List<Note>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }

        foreach (var note in list)
        {
            note.Tags = _tags.GetNamesForNote(note.Id).ToList();
        }

        return list;
    }

    private static Note Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        NotebookId = reader.GetInt64(1),
        Title = reader.GetString(2),
        Body = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
        IsPinned = reader.GetInt32(4) != 0,
        IsArchived = reader.GetInt32(5) != 0,
        CreatedUtc = Utc.Parse(reader.GetString(6)),
        UpdatedUtc = Utc.Parse(reader.GetString(7)),
        NotebookName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
    };

    private static void Bind(SqliteCommand cmd, Note note)
    {
        cmd.Parameters.AddWithValue("$notebookId", note.NotebookId);
        cmd.Parameters.AddWithValue("$title", note.Title.Trim());
        cmd.Parameters.AddWithValue("$body", note.Body ?? string.Empty);
        cmd.Parameters.AddWithValue("$pinned", note.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("$archived", note.IsArchived ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", Utc.Format(note.CreatedUtc));
        cmd.Parameters.AddWithValue("$updated", Utc.Format(note.UpdatedUtc));
    }
}
