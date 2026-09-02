namespace SmartKnowledgeHub.Core.Search;

/// <summary>
/// Extracts plain text from registered files. Only .txt, .md and .csv are read.
/// Binary files (NUL in the header) are skipped. No PDF parser is included.
/// </summary>
public static class TextExtractor
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv"
    };

    public static bool CanExtract(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Allowed.Contains(Path.GetExtension(path));
    }

    public static string Extract(string path, int maxChars = 200_000)
    {
        if (!CanExtract(path) || !File.Exists(path))
        {
            return string.Empty;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[Math.Min(512, stream.Length)];
            var read = stream.Read(header, 0, header.Length);
            for (var i = 0; i < read; i++)
            {
                if (header[i] == 0)
                {
                    return string.Empty;
                }
            }
        }
        catch (IOException)
        {
            return string.Empty;
        }

        try
        {
            var text = File.ReadAllText(path);
            if (text.Length <= maxChars)
            {
                return text;
            }

            return text[..maxChars];
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }
}
