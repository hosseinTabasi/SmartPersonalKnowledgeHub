using Microsoft.Data.Sqlite;

namespace SmartKnowledgeHub.Core.Data;

public static class DbInitializer
{
    public static void EnsureCreated(SqliteConnectionFactory factory)
    {
        using var connection = factory.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = SchemaSql;
        cmd.ExecuteNonQuery();
    }

    public const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS Notebooks (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Notes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            NotebookId INTEGER NOT NULL REFERENCES Notebooks(Id) ON DELETE CASCADE,
            Title TEXT NOT NULL,
            Body TEXT NOT NULL DEFAULT '',
            IsPinned INTEGER NOT NULL DEFAULT 0,
            IsArchived INTEGER NOT NULL DEFAULT 0,
            CreatedUtc TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Tags (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS NoteTags (
            NoteId INTEGER NOT NULL REFERENCES Notes(Id) ON DELETE CASCADE,
            TagId INTEGER NOT NULL REFERENCES Tags(Id) ON DELETE CASCADE,
            PRIMARY KEY (NoteId, TagId)
        );

        CREATE TABLE IF NOT EXISTS Tasks (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Body TEXT NOT NULL DEFAULT '',
            DueUtc TEXT,
            Priority INTEGER NOT NULL DEFAULT 1,
            Status TEXT NOT NULL DEFAULT 'Todo',
            NoteId INTEGER REFERENCES Notes(Id) ON DELETE SET NULL,
            CreatedUtc TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Files (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            OriginalPath TEXT NOT NULL,
            VaultPath TEXT,
            FileName TEXT NOT NULL,
            Extension TEXT,
            SizeBytes INTEGER NOT NULL DEFAULT 0,
            TagsCsv TEXT,
            CreatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SearchIndex (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            EntityType TEXT NOT NULL,
            EntityId INTEGER NOT NULL,
            Text TEXT NOT NULL,
            EmbeddingBlob BLOB,
            UNIQUE (EntityType, EntityId)
        );

        CREATE INDEX IF NOT EXISTS IX_Notes_NotebookId ON Notes(NotebookId);
        CREATE INDEX IF NOT EXISTS IX_Notes_UpdatedUtc ON Notes(UpdatedUtc);
        CREATE INDEX IF NOT EXISTS IX_Tasks_Status ON Tasks(Status);
        CREATE INDEX IF NOT EXISTS IX_Tasks_DueUtc ON Tasks(DueUtc);
        CREATE INDEX IF NOT EXISTS IX_SearchIndex_Entity ON SearchIndex(EntityType, EntityId);

        CREATE VIRTUAL TABLE IF NOT EXISTS NotesFts USING fts5(
            Title,
            Body,
            content='Notes',
            content_rowid='Id',
            tokenize='porter unicode61'
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS TasksFts USING fts5(
            Title,
            Body,
            content='Tasks',
            content_rowid='Id',
            tokenize='porter unicode61'
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS FilesFts USING fts5(
            FileName,
            ExtractedText,
            TagsCsv,
            tokenize='porter unicode61'
        );

        CREATE TRIGGER IF NOT EXISTS notes_ai AFTER INSERT ON Notes BEGIN
            INSERT INTO NotesFts(rowid, Title, Body)
            VALUES (new.Id, new.Title, new.Body);
        END;

        CREATE TRIGGER IF NOT EXISTS notes_ad AFTER DELETE ON Notes BEGIN
            INSERT INTO NotesFts(NotesFts, rowid, Title, Body)
            VALUES ('delete', old.Id, old.Title, old.Body);
        END;

        CREATE TRIGGER IF NOT EXISTS notes_au AFTER UPDATE ON Notes BEGIN
            INSERT INTO NotesFts(NotesFts, rowid, Title, Body)
            VALUES ('delete', old.Id, old.Title, old.Body);
            INSERT INTO NotesFts(rowid, Title, Body)
            VALUES (new.Id, new.Title, new.Body);
        END;

        CREATE TRIGGER IF NOT EXISTS tasks_ai AFTER INSERT ON Tasks BEGIN
            INSERT INTO TasksFts(rowid, Title, Body)
            VALUES (new.Id, new.Title, new.Body);
        END;

        CREATE TRIGGER IF NOT EXISTS tasks_ad AFTER DELETE ON Tasks BEGIN
            INSERT INTO TasksFts(TasksFts, rowid, Title, Body)
            VALUES ('delete', old.Id, old.Title, old.Body);
        END;

        CREATE TRIGGER IF NOT EXISTS tasks_au AFTER UPDATE ON Tasks BEGIN
            INSERT INTO TasksFts(TasksFts, rowid, Title, Body)
            VALUES ('delete', old.Id, old.Title, old.Body);
            INSERT INTO TasksFts(rowid, Title, Body)
            VALUES (new.Id, new.Title, new.Body);
        END;
        """;
}
