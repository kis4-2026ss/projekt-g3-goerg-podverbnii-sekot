using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Ai.Normalization;

public sealed class AiReviewNormalizer : IReviewNormalizer
{
    public ReviewDocument Normalize(string repoName, IReadOnlyList<string> validFiles, ReviewDocument review)
    {
        var validFileSet = validFiles
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var normalizedFiles = new List<ReviewFile>();

        foreach (var file in review.Files)
        {
            string normalizedFileName = NormalizePath(file.File);
            if (!validFileSet.Contains(normalizedFileName))
            {
                continue;
            }

            var findings = file.Findings
                .Where(finding =>
                    NormalizePath(finding.File).Equals(normalizedFileName, StringComparison.OrdinalIgnoreCase)
                    && finding.Line > 0
                    && !string.IsNullOrWhiteSpace(finding.Comment))
                .Select(finding => new ReviewFinding
                {
                    File = normalizedFileName,
                    Line = finding.Line,
                    Comment = finding.Comment.Trim()
                })
                .ToList();

            normalizedFiles.Add(new ReviewFile
            {
                File = normalizedFileName,
                Summary = file.Summary.Trim(),
                Findings = findings
            });
        }

        var existing = normalizedFiles
            .Select(file => file.File)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string validFile in validFileSet.Where(validFile => !existing.Contains(validFile)))
        {
            normalizedFiles.Add(new ReviewFile
            {
                File = validFile,
                Summary = string.Empty,
                Findings = []
            });
        }

        return new ReviewDocument
        {
            RepoName = repoName,
            Summary = review.Summary.Trim(),
            Files = normalizedFiles
                .OrderBy(file => file.File, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim().TrimStart('.', '/');
    }
}
