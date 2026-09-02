namespace SmartKnowledgeHub.Core.Embedding;

public interface IEmbeddingService
{
    string Name { get; }
    bool IsAvailable { get; }
    int Dimensions { get; }
    float[] Embed(string text);
}
