using System.Text.Json;
using GraderTool.Core.Models;

namespace GraderTool.Ai.Parsing;

public sealed class AiReviewJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReviewDocument Parse(string json, string fallbackRepoName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new AiReviewParseException("Die KI-Antwort ist leer.");
        }

        try
        {
            var dto = JsonSerializer.Deserialize<AiReviewDto>(json, JsonOptions)
                ?? throw new AiReviewParseException("Die KI-Antwort konnte nicht als Review-JSON gelesen werden.");

            return new ReviewDocument
            {
                RepoName = string.IsNullOrWhiteSpace(dto.RepoName) ? fallbackRepoName : dto.RepoName.Trim(),
                Summary = dto.Summary?.Trim() ?? string.Empty,
                Files = (dto.Files ?? [])
                    .Where(file => !string.IsNullOrWhiteSpace(file.File))
                    .Select(file => new ReviewFile
                    {
                        File = file.File!.Trim(),
                        Summary = file.Summary?.Trim() ?? string.Empty,
                        Findings = (file.Findings ?? [])
                            .Where(finding =>
                                !string.IsNullOrWhiteSpace(finding.File)
                                && finding.Line > 0
                                && !string.IsNullOrWhiteSpace(finding.Comment))
                            .Select(finding => new ReviewFinding
                            {
                                File = finding.File!.Trim(),
                                Line = finding.Line,
                                Comment = finding.Comment!.Trim()
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
        catch (JsonException exc)
        {
            throw new AiReviewParseException("Die KI-Antwort ist kein valides JSON.", exc);
        }
    }
}
