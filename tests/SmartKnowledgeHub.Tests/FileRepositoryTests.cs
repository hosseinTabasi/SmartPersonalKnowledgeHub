namespace SmartKnowledgeHub.Tests;

public sealed class FileRepositoryTests
{
    [Fact]
    public void Register_TextFile_ExtractsContent()
    {
        using var hub = new TempHub();
        var path = Path.Combine(hub.Root, "note.md");
        File.WriteAllText(path, "# Hello\nfull text search with sqlite fts5");
        var record = hub.Vault.Register(path, "docs", copyIntoVault: true);
        Assert.True(File.Exists(record.VaultPath));
        Assert.Equal(".md", record.Extension);
        Assert.Equal(1, hub.Files.Count());
        var hits = hub.Search.KeywordSearch("sqlite");
        Assert.Contains(hits, h => h.EntityType == "file");
    }

    [Fact]
    public void Register_BinaryFile_SkipsExtraction()
    {
        using var hub = new TempHub();
        var path = Path.Combine(hub.Root, "blob.bin");
        File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0x02, 0xFF, 0x00 });
        var record = hub.Vault.Register(path, "bin", copyIntoVault: false);
        Assert.Equal(".bin", record.Extension);
        var hits = hub.Search.KeywordSearch("blob");
        Assert.DoesNotContain(hits, h => h.EntityId == record.Id && !string.IsNullOrWhiteSpace(h.Snippet) && h.Snippet.Contains('\0'));
    }

    [Fact]
    public void Delete_RemovesFtsRow()
    {
        using var hub = new TempHub();
        var path = Path.Combine(hub.Root, "gone.txt");
        File.WriteAllText(path, "uniqueTokenZebra");
        var record = hub.Vault.Register(path, "", copyIntoVault: false);
        hub.Vault.Delete(record, deleteVaultCopy: false);
        Assert.Empty(hub.Search.KeywordSearch("uniqueTokenZebra"));
    }
}
