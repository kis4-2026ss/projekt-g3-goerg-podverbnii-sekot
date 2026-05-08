using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class ReviewNormalizer : IReviewNormalizer
{
    public ReviewDocument Normalize(string repoName, IReadOnlyList<string> validFiles, ReviewDocument review)
    {
        HashSet<string> validFileSet = validFiles
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<ReviewFile> normalizedFiles = new();

        foreach (ReviewFile file in review.Files)
        {
            string fileName = NormalizePath(file.File);
            if (!validFileSet.Contains(fileName))
            {
                continue;
            }

            List<ReviewFinding> findings = file.Findings
                .Where(finding => NormalizePath(finding.File) == fileName && finding.Line > 0 && !string.IsNullOrWhiteSpace(finding.Comment))
                .Select(finding => new ReviewFinding
                {
                    File = fileName,
                    Line = finding.Line,
                    Comment = finding.Comment.Trim()
                })
                .ToList();

            normalizedFiles.Add(new ReviewFile
            {
                File = fileName,
                Summary = file.Summary?.Trim() ?? string.Empty,
                Findings = findings
            });
        }

        HashSet<string> presentFiles = normalizedFiles.Select(file => file.File).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string validFile in validFileSet)
        {
            if (!presentFiles.Contains(validFile))
            {
                normalizedFiles.Add(new ReviewFile
                {
                    File = validFile,
                    Summary = string.Empty,
                    Findings = new List<ReviewFinding>()
                });
            }
        }

        return new ReviewDocument
        {
            RepoName = repoName,
            Summary = review.Summary?.Trim() ?? string.Empty,
            Files = normalizedFiles.OrderBy(file => file.File, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim().TrimStart('.', '/');
    }
}
