using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Infrastructure.Settings;

namespace GraderTool.Infrastructure.Paths;

public sealed class PathResolver : IPathResolver
{
    private readonly JsonSettingsService? _settingsService;
    private readonly ProjectRootDetector _projectRootDetector;
    private readonly GraderRootResolver _graderRootResolver;

    public PathResolver(
        JsonSettingsService? settingsService = null,
        ProjectRootDetector? projectRootDetector = null,
        GraderRootResolver? graderRootResolver = null)
    {
        _settingsService = settingsService;
        _projectRootDetector = projectRootDetector ?? new ProjectRootDetector();
        _graderRootResolver = graderRootResolver ?? new GraderRootResolver();
    }

    public async Task<AppPaths> ResolveAsync(CancellationToken cancellationToken = default)
    {
        AppSettings settings = _settingsService is null
            ? new AppSettings()
            : await _settingsService.LoadAsync(cancellationToken);

        string projectRoot = _projectRootDetector.Detect(settings.ProjectRoot);
        string graderRoot = _graderRootResolver.Resolve(projectRoot, settings.GraderRoot);
        string studentsFile = !string.IsNullOrWhiteSpace(settings.StudentsFile)
            ? Path.GetFullPath(settings.StudentsFile)
            : Path.Combine(graderRoot, "students_list.txt");

        return new AppPaths
        {
            ProjectRoot = projectRoot,
            GraderRoot = graderRoot,
            ReposDirectory = Path.Combine(graderRoot, "repos"),
            ReviewsDirectory = Path.Combine(graderRoot, "reviews"),
            LogsDirectory = Path.Combine(graderRoot, "logs"),
            StudentsFile = studentsFile
        };
    }
}
