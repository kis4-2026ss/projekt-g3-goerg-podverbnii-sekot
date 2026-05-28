using System.Text.Json;
using System.Text.Json.Serialization;
using GraderTool.Core.Models;
using GraderTool.Core.Services;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class ReviewJsonStore : IReviewStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<ReviewDocument?> LoadAsync(string reviewFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(reviewFilePath))
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(reviewFilePath);
        return await JsonSerializer.DeserializeAsync<ReviewDocument>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(string reviewFilePath, ReviewDocument review, CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(reviewFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = File.Create(reviewFilePath);
        await JsonSerializer.SerializeAsync(stream, review, JsonOptions, cancellationToken);
    }
}
