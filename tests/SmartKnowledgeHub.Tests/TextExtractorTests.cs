using SmartKnowledgeHub.Core.Search;

namespace SmartKnowledgeHub.Tests;

public sealed class TextExtractorTests
{
    [Fact]
    public void CanExtract_OnlyTxtMdCsv()
    {
        Assert.True(TextExtractor.CanExtract("a.md"));
        Assert.True(TextExtractor.CanExtract("a.TXT"));
        Assert.True(TextExtractor.CanExtract("a.csv"));
        Assert.False(TextExtractor.CanExtract("a.pdf"));
        Assert.False(TextExtractor.CanExtract("a.png"));
        Assert.False(TextExtractor.CanExtract("a.bin"));
    }

    [Fact]
    public void Extract_ReadsMarkdown()
    {
        var dir = Path.Combine(Path.GetTempPath(), "skh-extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "welcome.md");
        File.WriteAllText(path, "hello extractor");
        Assert.Contains("hello extractor", TextExtractor.Extract(path));
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Extract_ReturnsEmpty_ForMissingFile()
    {
        Assert.Equal(string.Empty, TextExtractor.Extract("/tmp/does-not-exist-skh.md"));
    }
}
