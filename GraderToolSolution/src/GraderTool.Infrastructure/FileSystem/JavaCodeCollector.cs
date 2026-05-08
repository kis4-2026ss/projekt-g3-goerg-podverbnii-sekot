using System.Text;
using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class JavaCodeCollector : IJavaCodeCollector
{
    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "test",
        "tests",
        ".git"
    };

    public Task<IReadOnlyList<string>> FindRelevantJavaFilesAsync(
        string repositoryDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(repositoryDirectory))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        List<string> files = Directory
            .EnumerateFiles(repositoryDirectory, "*.java", SearchOption.AllDirectories)
            .Where(IsRelevantJavaFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public async Task<string> BuildNumberedCodeBlobAsync(
        string repositoryDirectory,
        IReadOnlyList<string> javaFiles,
        int maxChars,
        CancellationToken cancellationToken = default)
    {
        StringBuilder builder = new();

        foreach (string filePath in javaFiles)
        {
            string relativePath = Path.GetRelativePath(repositoryDirectory, filePath).Replace(Path.DirectorySeparatorChar, '/');
            string text = await ReadTextSafeAsync(filePath, cancellationToken);
            StringBuilder block = new();
            block.AppendLine();
            block.AppendLine($"===== FILE: {relativePath} =====");

            int lineNumber = 1;
            using StringReader reader = new(text);
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                block.AppendLine($"{lineNumber,4}: {line}");
                lineNumber++;
            }

            if (builder.Length + block.Length > maxChars)
            {
                int remaining = Math.Max(0, maxChars - builder.Length);
                if (remaining > 0)
                {
                    builder.Append(block.ToString()[..remaining]);
                }

                break;
            }

            builder.Append(block);
        }

        return builder.ToString();
    }

    private static bool IsRelevantJavaFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        if (fileName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !parts.Any(part => IgnoredDirectoryNames.Contains(part));
    }

    private static async Task<string> ReadTextSafeAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (DecoderFallbackException)
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
