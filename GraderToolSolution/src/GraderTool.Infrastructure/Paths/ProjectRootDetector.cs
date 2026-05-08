namespace GraderTool.Infrastructure.Paths;

public sealed class ProjectRootDetector
{
    private static readonly string[] RootMarkers =
    {
        "GraderTool.sln",
        "src",
        "examples"
    };

    public string Detect(string? explicitProjectRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitProjectRoot))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitProjectRoot));
        }

        string startDirectory = AppContext.BaseDirectory;
        DirectoryInfo? current = new(startDirectory);

        while (current is not null)
        {
            if (LooksLikeProjectRoot(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Path.GetFullPath(startDirectory);
    }

    private static bool LooksLikeProjectRoot(string directory)
    {
        return RootMarkers.Any(marker => File.Exists(Path.Combine(directory, marker)) || Directory.Exists(Path.Combine(directory, marker)));
    }
}
