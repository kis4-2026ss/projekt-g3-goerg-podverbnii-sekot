namespace GraderTool.Infrastructure.Paths;

public sealed class GraderRootResolver
{
    public string Resolve(string projectRoot, string? explicitGraderRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitGraderRoot))
        {
            return Normalize(explicitGraderRoot);
        }

        string pathsFile = FindFileUpwards(projectRoot, "paths.txt") ?? Path.Combine(projectRoot, "paths.txt");
        if (File.Exists(pathsFile))
        {
            Dictionary<string, string> values = LoadKeyValueFile(pathsFile);
            if (values.TryGetValue("GRADER_ROOT", out string? graderRoot) && !string.IsNullOrWhiteSpace(graderRoot))
            {
                return Normalize(graderRoot);
            }
        }

        string? environmentValue = Environment.GetEnvironmentVariable("GRADER_ROOT");
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return Normalize(environmentValue);
        }

        return projectRoot;
    }

    private static string Normalize(string path)
    {
        string expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"', '\''));
        return Path.GetFullPath(expanded);
    }

    private static string? FindFileUpwards(string startDirectory, string fileName)
    {
        DirectoryInfo? current = new(startDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static Dictionary<string, string> LoadKeyValueFile(string filePath)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (string rawLine in File.ReadLines(filePath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim().Trim('"', '\'');
            values[key] = value;
        }

        return values;
    }
}
