using SmartKnowledgeHub.Core.Embedding;

namespace SmartKnowledgeHub.Tests;

public sealed class EmbeddingTests
{
    [Fact]
    public void Cosine_Of_Self_IsOne()
    {
        var svc = new HashedTfidfEmbeddingService();
        svc.AddDocumentToCorpus("sqlite fts5 search notes");
        var v = svc.Embed("sqlite fts5 search notes");
        Assert.InRange(HashedTfidfEmbeddingService.Cosine(v, v), 0.999, 1.001);
    }

    [Fact]
    public void RelatedDocuments_ScoreHigherThanUnrelated()
    {
        var svc = new HashedTfidfEmbeddingService();
        svc.AddDocumentToCorpus("sqlite database full text search fts5 bm25 ranking");
        svc.AddDocumentToCorpus("cardamom ginger tea recipe milk");
        var query = svc.Embed("sqlite full text search ranking");
        var db = svc.Embed("sqlite database full text search fts5 bm25 ranking");
        var tea = svc.Embed("cardamom ginger tea recipe milk");
        Assert.True(HashedTfidfEmbeddingService.Cosine(query, db) > HashedTfidfEmbeddingService.Cosine(query, tea));
    }

    [Fact]
    public void Onnx_IsUnavailable_WhenFileMissing()
    {
        var onnx = new OnnxEmbeddingService(Path.Combine(Path.GetTempPath(), "no-such-minilm.onnx"));
        Assert.False(onnx.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => onnx.Embed("hello"));
    }

    [Fact]
    public void Factory_FallsBackToHashedTfidf()
    {
        var svc = EmbeddingFactory.CreateDefault("/tmp/missing-minilm.onnx");
        Assert.IsType<HashedTfidfEmbeddingService>(svc);
        Assert.True(svc.IsAvailable);
    }

    [Fact]
    public void Blob_RoundTrip()
    {
        var svc = new HashedTfidfEmbeddingService(64);
        var v = svc.Embed("round trip vector");
        var blob = HashedTfidfEmbeddingService.ToBlob(v);
        var back = HashedTfidfEmbeddingService.FromBlob(blob, 64);
        Assert.Equal(v.Length, back.Length);
        Assert.InRange(HashedTfidfEmbeddingService.Cosine(v, back), 0.999, 1.001);
    }
}
