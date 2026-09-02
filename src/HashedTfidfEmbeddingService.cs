using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartKnowledgeHub.Core.Embedding;

/// <summary>
/// Offline hashed bag-of-words / TF-IDF embedding. Tokens are hashed into a
/// fixed-size vector (feature hashing). Optional inverse-document-frequency
/// weights can be supplied from the local corpus. No model download is required.
/// </summary>
public sealed class HashedTfidfEmbeddingService : IEmbeddingService
{
    public const int DefaultDimensions = 256;
    private static readonly Regex TokenSplit = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    private readonly Dictionary<int, int> _documentFrequency = new();
    private int _documentCount;

    public HashedTfidfEmbeddingService(int dimensions = DefaultDimensions)
    {
        if (dimensions < 32)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be at least 32.");
        }

        Dimensions = dimensions;
    }

    public string Name => "Hashed TF-IDF (local, offline)";
    public bool IsAvailable => true;
    public int Dimensions { get; }

    public void ResetCorpus()
    {
        _documentFrequency.Clear();
        _documentCount = 0;
    }

    public void AddDocumentToCorpus(string text)
    {
        var seen = new HashSet<int>();
        foreach (var token in Tokenize(text))
        {
            var index = HashToken(token);
            if (seen.Add(index))
            {
                _documentFrequency[index] = _documentFrequency.GetValueOrDefault(index) + 1;
            }
        }

        _documentCount++;
    }

    public float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        var tokens = Tokenize(text);
        if (tokens.Count == 0)
        {
            return vector;
        }

        var tf = new Dictionary<int, int>();
        foreach (var token in tokens)
        {
            var index = HashToken(token);
            tf[index] = tf.GetValueOrDefault(index) + 1;
        }

        double length = tokens.Count;
        foreach (var pair in tf)
        {
            double termFrequency = pair.Value / length;
            double idf = 1.0;
            if (_documentCount > 0)
            {
                var df = _documentFrequency.GetValueOrDefault(pair.Key, 0);
                idf = Math.Log((_documentCount + 1.0) / (df + 1.0)) + 1.0;
            }

            vector[pair.Key] = (float)(termFrequency * idf);
        }

        Normalize(vector);
        return vector;
    }

    public static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na <= 0 || nb <= 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    public static byte[] ToBlob(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] FromBlob(byte[]? blob, int dimensions)
    {
        var vector = new float[dimensions];
        if (blob is null || blob.Length == 0)
        {
            return vector;
        }

        var count = Math.Min(dimensions, blob.Length / sizeof(float));
        Buffer.BlockCopy(blob, 0, vector, 0, count * sizeof(float));
        return vector;
    }

    public IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var parts = TokenSplit.Split(text.ToLowerInvariant());
        var list = new List<string>();
        foreach (var part in parts)
        {
            if (part.Length < 2 || Stopwords.Contains(part))
            {
                continue;
            }

            list.Add(part);
        }

        return list;
    }

    private int HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % (uint)Dimensions);
    }

    private static void Normalize(float[] vector)
    {
        double sum = 0;
        foreach (var v in vector)
        {
            sum += v * v;
        }

        if (sum <= 0)
        {
            return;
        }

        var norm = (float)Math.Sqrt(sum);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her",
        "was", "one", "our", "out", "has", "have", "been", "this", "that", "with",
        "from", "they", "will", "would", "there", "their", "what", "when", "your",
        "into", "just", "than", "then", "them", "some", "about"
    };
}
