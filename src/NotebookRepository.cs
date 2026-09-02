using Microsoft.Data.Sqlite;
using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public sealed class NotebookRepository : INotebookRepository
{
    private readonly SqliteConnectionFactory _factory;

    public NotebookRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<Notebook> GetAll()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.Name, n.CreatedUtc,
                   (SELECT COUNT(*) FROM Notes x WHERE x.NotebookId = n.Id AND x.IsArchived = 0) AS NoteCount
            FROM Notebooks n
            ORDER BY n.Name COLLATE NOCASE;
            """;
        var list = new List<Notebook>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(Read(reader));
        }

        return list;
    }

    public Notebook? GetById(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT n.Id, n.Name, n.CreatedUtc,
                   (SELECT COUNT(*) FROM Notes x WHERE x.NotebookId = n.Id AND x.IsArchived = 0) AS NoteCount
            FROM Notebooks n
            WHERE n.Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    public Notebook GetOrCreate(string name)
    {
        var trimmed = name.Trim();
        using var conn = _factory.Open();
        using (var find = conn.CreateCommand())
        {
            find.CommandText = "SELECT Id, Name, CreatedUtc, 0 FROM Notebooks WHERE Name = $name COLLATE NOCASE;";
            find.Parameters.AddWithValue("$name", trimmed);
            using var reader = find.ExecuteReader();
            if (reader.Read())
            {
                return Read(reader);
            }
        }

        var id = Insert(trimmed);
        return GetById(id)!;
    }

    public long Insert(string name)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Notebooks (Name, CreatedUtc) VALUES ($name, $utc); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$utc", Utc.Format(DateTime.UtcNow));
        return (long)cmd.ExecuteScalar()!;
    }

    public void Rename(long id, string name)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Notebooks SET Name = $name WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Notebooks WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Notebook Read(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Name = reader.GetString(1),
        CreatedUtc = Utc.Parse(reader.GetString(2)),
        NoteCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
    };
}
