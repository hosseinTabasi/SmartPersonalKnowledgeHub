namespace SmartKnowledgeHub.Core.Models;

public sealed class FileRecord
{
    public long Id { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string? VaultPath { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string TagsCsv { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public string EffectivePath =>
        !string.IsNullOrWhiteSpace(VaultPath) && File.Exists(VaultPath)
            ? VaultPath
            : OriginalPath;
}
