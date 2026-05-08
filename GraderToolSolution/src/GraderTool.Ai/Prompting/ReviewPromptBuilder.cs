using GraderTool.Ai.Clients;

namespace GraderTool.Ai.Prompting;

public sealed class ReviewPromptBuilder
{
    public string BuildUserPrompt(AiReviewRequest request)
    {
        List<string> parts = [];

        if (!string.IsNullOrWhiteSpace(request.ReadmeText))
        {
            parts.Add("Aufgabenstellung / README:\n");
            parts.Add(request.ReadmeText.Trim());
            parts.Add("\n\n");
        }

        parts.Add($"""
Repository: {request.RepoName}

Zu prüfender Code:
{request.CodeBlob}

Antworte nur mit JSON im geforderten Format.
Alle Texte in summary und comment müssen auf Deutsch sein.
""");

        return string.Concat(parts);
    }
}
