using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TaskRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<TaskItem> GetAll(string? status = null)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Id, t.Title, t.Body, t.DueUtc, t.Priority, t.Status, t.NoteId,
                   t.CreatedUtc, t.UpdatedUtc, n.Title AS LinkedNoteTitle
            FROM Tasks t
            LEFT JOIN Notes n ON n.Id = t.NoteId
            WHERE ($status = '' OR t.Status = $status)
            ORDER BY
                CASE t.Status WHEN 'Todo' THEN 0 WHEN 'Doing' THEN 1 ELSE 2 END,
                t.Priority DESC,
                CASE WHEN t.DueUtc IS NULL OR t.DueUtc = '' THEN 1 ELSE 0 END,
                t.DueUtc ASC;
            """;
        cmd.Parameters.AddWithValue("$status", status ?? string.Empty);
        return ReadAll(cmd);
    }

    public IReadOnlyList<TaskItem> GetDueSoon(int days = 7)
    {
        var until = DateTime.UtcNow.Date.AddDays(days + 1);
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Id, t.Title, t.Body, t.DueUtc, t.Priority, t.Status, t.NoteId,
                   t.CreatedUtc, t.UpdatedUtc, n.Title AS LinkedNoteTitle
            FROM Tasks t
            LEFT JOIN Notes n ON n.Id = t.NoteId
            WHERE t.Status != 'Done'
              AND t.DueUtc IS NOT NULL AND t.DueUtc != ''
              AND t.DueUtc <= $until
            ORDER BY t.DueUtc ASC, t.Priority DESC;
            """;
        cmd.Parameters.AddWithValue("$until", Utc.Format(until));
        return ReadAll(cmd);
    }

    public TaskItem? GetById(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Id, t.Title, t.Body, t.DueUtc, t.Priority, t.Status, t.NoteId,
                   t.CreatedUtc, t.UpdatedUtc, n.Title AS LinkedNoteTitle
            FROM Tasks t
            LEFT JOIN Notes n ON n.Id = t.NoteId
            WHERE t.Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public long Insert(TaskItem task)
    {
        var now = DateTime.UtcNow;
        task.CreatedUtc = now;
        task.UpdatedUtc = now;
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Tasks (Title, Body, DueUtc, Priority, Status, NoteId, CreatedUtc, UpdatedUtc)
            VALUES ($title, $body, $due, $priority, $status, $noteId, $created, $updated);
            SELECT last_insert_rowid();
            """;
        Bind(cmd, task);
        var id = (long)cmd.ExecuteScalar()!;
        task.Id = id;
        return id;
    }

    public void Update(TaskItem task)
    {
        task.UpdatedUtc = DateTime.UtcNow;
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Tasks
            SET Title = $title,
                Body = $body,
                DueUtc = $due,
                Priority = $priority,
                Status = $status,
                NoteId = $noteId,
                UpdatedUtc = $updated
            WHERE Id = $id;
            """;
        Bind(cmd, task);
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Tasks WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public int CountByStatus(string status)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Tasks WHERE Status = $status;";
        cmd.Parameters.AddWithValue("$status", status);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static List<TaskItem> ReadAll(SqliteCommand cmd)
    {
        var list = new List<TaskItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Map(reader));
        }

        return list;
    }

    private static TaskItem Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Title = reader.GetString(1),
        Body = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
        DueUtc = Utc.ParseOptional(reader.IsDBNull(3) ? null : reader.GetString(3)),
        Priority = reader.GetInt32(4),
        Status = reader.GetString(5),
        NoteId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
        CreatedUtc = Utc.Parse(reader.GetString(7)),
        UpdatedUtc = Utc.Parse(reader.GetString(8)),
        LinkedNoteTitle = reader.IsDBNull(9) ? null : reader.GetString(9)
    };

    private static void Bind(SqliteCommand cmd, TaskItem task)
    {
        cmd.Parameters.AddWithValue("$title", task.Title.Trim());
        cmd.Parameters.AddWithValue("$body", task.Body ?? string.Empty);
        cmd.Parameters.AddWithValue("$due", task.DueUtc is null ? DBNull.Value : Utc.Format(task.DueUtc));
        cmd.Parameters.AddWithValue("$priority", task.Priority);
        cmd.Parameters.AddWithValue("$status", string.IsNullOrWhiteSpace(task.Status) ? TaskStatuses.Todo : task.Status);
        cmd.Parameters.AddWithValue("$noteId", task.NoteId is null ? DBNull.Value : task.NoteId.Value);
        cmd.Parameters.AddWithValue("$created", Utc.Format(task.CreatedUtc));
        cmd.Parameters.AddWithValue("$updated", Utc.Format(task.UpdatedUtc));
    }
}
