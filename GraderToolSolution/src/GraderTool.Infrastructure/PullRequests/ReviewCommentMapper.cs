using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.PullRequests;

public sealed class ReviewCommentMapper : IReviewCommentMapper
{
    public (IReadOnlyList<ReviewCommentTarget> InlineComments, IReadOnlyList<ReviewFinding> Leftovers) MapFindingsToPullRequestPositions(
        IReadOnlyList<ReviewFinding> findings,
        IReadOnlyList<PullRequestFile> pullRequestFiles)
    {
        Dictionary<string, PullRequestFile> filesByName = pullRequestFiles
            .Where(file => !string.IsNullOrWhiteSpace(file.FileName))
            .GroupBy(file => NormalizePath(file.FileName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        Dictionary<string, Dictionary<int, int>> positionMapsByFile = new(StringComparer.OrdinalIgnoreCase);

        foreach (PullRequestFile file in pullRequestFiles)
        {
            string normalizedPath = NormalizePath(file.FileName);
            if (string.IsNullOrWhiteSpace(normalizedPath) || string.IsNullOrWhiteSpace(file.Patch))
            {
                continue;
            }

            positionMapsByFile[normalizedPath] = BuildNewLineToPositionMap(file.Patch);
        }

        List<ReviewCommentTarget> inlineComments = new();
        List<ReviewFinding> leftovers = new();

        foreach (ReviewFinding finding in findings)
        {
            string normalizedFindingPath = NormalizePath(finding.File);

            if (!filesByName.TryGetValue(normalizedFindingPath, out PullRequestFile? targetFile)
                || !positionMapsByFile.TryGetValue(normalizedFindingPath, out Dictionary<int, int>? lineMap)
                || !lineMap.TryGetValue(finding.Line, out int position))
            {
                leftovers.Add(finding);
                continue;
            }

            inlineComments.Add(new ReviewCommentTarget(
                targetFile.FileName,
                position,
                finding.Comment));
        }

        return (inlineComments, leftovers);
    }

    private static Dictionary<int, int> BuildNewLineToPositionMap(string patch)
    {
        Dictionary<int, int> mapping = new();
        int position = 0;
        int? newLine = null;
        int? oldLine = null;

        foreach (string rawLine in patch.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            position++;

            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                (oldLine, newLine) = ParseHunkHeader(line);
                continue;
            }

            if (newLine is null || oldLine is null)
            {
                continue;
            }

            if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal))
            {
                mapping[newLine.Value] = position;
                newLine++;
            }
            else if (line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal))
            {
                oldLine++;
            }
            else
            {
                mapping[newLine.Value] = position;
                newLine++;
                oldLine++;
            }
        }

        return mapping;
    }

    private static (int OldStart, int NewStart) ParseHunkHeader(string header)
    {
        string[] parts = header.Split("@@", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new FormatException($"Invalid patch hunk header: {header}");
        }

        string[] ranges = parts[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (ranges.Length < 2)
        {
            throw new FormatException($"Invalid patch hunk header: {header}");
        }

        int oldStart = ParseRangeStart(ranges[0], '-');
        int newStart = ParseRangeStart(ranges[1], '+');
        return (oldStart, newStart);
    }

    private static int ParseRangeStart(string range, char expectedPrefix)
    {
        if (range.Length < 2 || range[0] != expectedPrefix)
        {
            throw new FormatException($"Invalid patch range: {range}");
        }

        string startText = range[1..].Split(',', 2)[0];
        return int.Parse(startText);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim().TrimStart('.', '/');
    }
}
