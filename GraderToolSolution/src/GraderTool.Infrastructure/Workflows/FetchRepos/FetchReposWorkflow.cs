using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Core.Workflows.FetchRepos;

namespace GraderTool.Infrastructure.Workflows.FetchRepos;

public sealed class FetchReposWorkflow : IFetchReposWorkflow
{
    private readonly IPathResolver _pathResolver;
    private readonly IStudentListService _studentListService;
    private readonly IGitHubClassroomService _classroomService;
    private readonly IRepositoryCloneService _cloneService;
    private readonly IWorkflowLogger _logger;

    public FetchReposWorkflow(
        IPathResolver pathResolver,
        IStudentListService studentListService,
        IGitHubClassroomService classroomService,
        IRepositoryCloneService cloneService,
        IWorkflowLogger logger)
    {
        _pathResolver = pathResolver;
        _studentListService = studentListService;
        _classroomService = classroomService;
        _cloneService = cloneService;
        _logger = logger;
    }

    public async Task<FetchReposResult> RunAsync(
        FetchReposRequest request,
        IProgress<FetchReposProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        AppPaths paths = await _pathResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
        string graderRoot = ResolveOverrideOrDefault(request.GraderRootOverride, paths.GraderRoot);
        string studentsFile = ResolveOverrideOrDefault(request.StudentsFileOverride, paths.StudentsFile);
        string outputDirectory = ResolveOverrideOrDefault(
            request.OutputDirectoryOverride,
            Path.Combine(graderRoot, "repos", $"hue{request.HomeworkNumber}"));

        _logger.Info($"Grader root: {graderRoot}");
        _logger.Info($"Students file: {studentsFile}");
        _logger.Info($"Output directory: {outputDirectory}");
        _logger.Info($"Match mode: {request.MatchBy}");

        IReadOnlySet<Student> students = await _studentListService.LoadStudentsAsync(studentsFile, cancellationToken).ConfigureAwait(false);
        if (students.Count == 0)
        {
            throw new InvalidOperationException("The student list is empty.");
        }

        HashSet<string> studentIdentifiers = students
            .Select(student => student.Identifier.Trim())
            .Where(identifier => identifier.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<AcceptedAssignment> acceptedAssignments = await _classroomService
            .GetAcceptedAssignmentsAsync(request.AssignmentId, cancellationToken)
            .ConfigureAwait(false);

        List<StudentRepository> matchedRepositories = acceptedAssignments
            .Where(assignment => MatchesStudentList(assignment, studentIdentifiers, request.MatchBy))
            .Select(assignment => assignment.Repository)
            .GroupBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(repository => repository.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!request.DryRun)
        {
            Directory.CreateDirectory(outputDirectory);
        }

        progress?.Report(new FetchReposProgress(0, matchedRepositories.Count, $"Found {matchedRepositories.Count} matching repositories."));
        _logger.Info($"Found {matchedRepositories.Count} matching repositories.");

        List<string> skippedRepositories = new();
        int index = 0;
        foreach (StudentRepository repository in matchedRepositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;

            string destination = Path.Combine(outputDirectory, repository.Name);
            if (Directory.Exists(destination))
            {
                skippedRepositories.Add(repository.FullName);
                string skipMessage = $"Skip existing: {repository.FullName}";
                _logger.Info(skipMessage);
                progress?.Report(new FetchReposProgress(index, matchedRepositories.Count, skipMessage, repository.Name));
                continue;
            }

            string message = request.DryRun
                ? $"[DRY RUN] Would clone {repository.FullName}"
                : $"Cloning {repository.FullName}";
            _logger.Info(message);
            progress?.Report(new FetchReposProgress(index, matchedRepositories.Count, message, repository.Name));

            await _cloneService.CloneAsync(repository.FullName, outputDirectory, request.DryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        return new FetchReposResult
        {
            OutputDirectory = outputDirectory,
            MatchedRepositoryCount = matchedRepositories.Count,
            MatchedRepositories = matchedRepositories,
            SkippedRepositories = skippedRepositories,
            DryRun = request.DryRun
        };
    }

    private static bool MatchesStudentList(
        AcceptedAssignment assignment,
        HashSet<string> studentIdentifiers,
        StudentMatchMode matchBy)
    {
        return matchBy switch
        {
            StudentMatchMode.Login => assignment.StudentLogins.Any(studentIdentifiers.Contains),
            StudentMatchMode.RosterIdentifier => !string.IsNullOrWhiteSpace(assignment.RosterIdentifier)
                && studentIdentifiers.Contains(assignment.RosterIdentifier),
            _ => false
        };
    }

    private static void ValidateRequest(FetchReposRequest request)
    {
        if (request.AssignmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.AssignmentId), "Assignment ID must be greater than zero.");
        }

        if (request.HomeworkNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.HomeworkNumber), "Homework number must be greater than zero.");
        }
    }

    private static string ResolveOverrideOrDefault(string? overrideValue, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(overrideValue)
            ? Path.GetFullPath(defaultValue)
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(overrideValue));
    }
}
