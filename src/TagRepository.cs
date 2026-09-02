using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;

namespace SmartKnowledgeHub.Core.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TagRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<Tag> GetAll()
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Tags ORDER BY Name COLLATE NOCASE;";
        var list = new List<Tag>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Tag { Id = reader.GetInt64(0), Name = reader.GetString(1) });
        }

        return list;
    }

    public IReadOnlyList<string> GetNamesForNote(long noteId)
    {
        using var conn = _factory.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.Name
            FROM Tags t
            INNER JOIN NoteTags nt ON nt.TagId = t.Id
            WHERE nt.NoteId = $id
            ORDER BY t.Name COLLATE NOCASE;
            """;
        cmd.Parameters.AddWithValue("$id", noteId);
        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public void ReplaceNoteTags(long noteId, IEnumerable<string> tagNames)
    {
        var names = tagNames
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var conn = _factory.Open();
        using var tx = conn.BeginTransaction();

        using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM NoteTags WHERE NoteId = $id;";
            del.Parameters.AddWithValue("$id", noteId);
            del.ExecuteNonQuery();
        }

        foreach (var name in names)
        {
            long tagId;
            using (var find = conn.CreateCommand())
            {
                find.Transaction = tx;
                find.CommandText = "SELECT Id FROM Tags WHERE Name = $name COLLATE NOCASE;";
                find.Parameters.AddWithValue("$name", name);
                var existing = find.ExecuteScalar();
                if (existing is long id)
                {
                    tagId = id;
                }
                else
                {
                    using var ins = conn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = "INSERT INTO Tags (Name) VALUES ($name); SELECT last_insert_rowid();";
                    ins.Parameters.AddWithValue("$name", name);
                    tagId = (long)ins.ExecuteScalar()!;
                }
            }

            using var link = conn.CreateCommand();
            link.Transaction = tx;
            link.CommandText = "INSERT OR IGNORE INTO NoteTags (NoteId, TagId) VALUES ($nid, $tid);";
            link.Parameters.AddWithValue("$nid", noteId);
            link.Parameters.AddWithValue("$tid", tagId);
            link.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
