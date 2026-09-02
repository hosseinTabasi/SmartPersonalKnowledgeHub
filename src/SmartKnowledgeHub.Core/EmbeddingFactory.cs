namespace SmartKnowledgeHub.Core.Embedding;

public static class EmbeddingFactory
{
    public static IEmbeddingService CreateDefault(string? onnxModelPath = null)
    {
        if (!string.IsNullOrWhiteSpace(onnxModelPath))
        {
            var onnx = new OnnxEmbeddingService(onnxModelPath);
            if (onnx.IsAvailable)
            {
                try
                {
                    _ = onnx.Embed("availability probe");
                    return onnx;
                }
                catch (InvalidOperationException)
                {
                    // Model file may exist without a usable runtime; fall back.
                }
            }
        }

        return new HashedTfidfEmbeddingService();
    }
}
