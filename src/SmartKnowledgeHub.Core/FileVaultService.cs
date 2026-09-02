using SmartKnowledgeHub.Core.Data;
using SmartKnowledgeHub.Core.Models;
using SmartKnowledgeHub.Core.Repositories;
using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.Core.Services;

public sealed class FileVaultService
{
    private readonly AppPaths _paths;
    private readonly IFileRepository _files;
    private readonly ISearchService _search;

    public FileVaultService(AppPaths paths, IFileRepository files, ISearchService search)
    {
        _paths = paths;
        _files = files;
        _search = search;
    }

    public FileRecord Register(string originalPath, string? tagsCsv, bool copyIntoVault)
    {
        if (!File.Exists(originalPath))
        {
            throw new FileNotFoundException("The selected file does not exist.", originalPath);
        }

        var info = new FileInfo(originalPath);
        string? vaultPath = null;
        if (copyIntoVault)
        {
            Directory.CreateDirectory(_paths.VaultDirectory);
            var destName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Sanitize(info.Name)}";
            vaultPath = Path.Combine(_paths.VaultDirectory, destName);
            File.Copy(originalPath, vaultPath, overwrite: false);
        }

        var record = new FileRecord
        {
            OriginalPath = originalPath,
            VaultPath = vaultPath,
            FileName = info.Name,
            Extension = info.Extension,
            SizeBytes = info.Length,
            TagsCsv = tagsCsv ?? string.Empty,
            CreatedUtc = DateTime.UtcNow
        };

        var extractFrom = vaultPath ?? originalPath;
        var extracted = TextExtractor.Extract(extractFrom);
        _files.Insert(record, extracted);
        _search.UpsertFile(record, extracted);
        return record;
    }

    public void Delete(FileRecord record, bool deleteVaultCopy)
    {
        if (deleteVaultCopy && !string.IsNullOrWhiteSpace(record.VaultPath) && File.Exists(record.VaultPath))
        {
            File.Delete(record.VaultPath);
        }

        _files.Delete(record.Id);
        _search.Remove("file", record.Id);
    }

    private static string Sanitize(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }
}
