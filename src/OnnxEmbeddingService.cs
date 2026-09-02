namespace SmartKnowledgeHub.Core.Embedding;

/// <summary>
/// Optional ONNX MiniLM loader. The application does not ship a large model file.
/// If Assets/models/minilm.onnx is missing, <see cref="IsAvailable"/> is false and
/// callers must fall back to hashed TF-IDF plus FTS5. No network download is performed.
/// </summary>
public sealed class OnnxEmbeddingService : IEmbeddingService
{
    public OnnxEmbeddingService(string modelPath)
    {
        ModelPath = modelPath;
        IsAvailable = File.Exists(modelPath);
    }

    public string ModelPath { get; }
    public string Name => "ONNX MiniLM (optional local file)";
    public bool IsAvailable { get; }
    public int Dimensions => 384;

    public float[] Embed(string text)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Optional ONNX model was not found. Place minilm.onnx under Assets/models to enable it, " +
                "or use the default hashed TF-IDF embedding service.");
        }

        throw new InvalidOperationException(
            "An ONNX model file is present, but this build does not bundle ONNX Runtime. " +
            "Search continues with hashed TF-IDF. Add the optional runtime package in a later lab if needed.");
    }
}
