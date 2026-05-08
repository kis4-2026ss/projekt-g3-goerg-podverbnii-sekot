using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Core.Workflows.GenerateReviews;

namespace GraderTool.Infrastructure.Workflows.GenerateReviews;

public sealed class GenerateReviewsWorkflow : IGenerateReviewsWorkflow
{
    private readonly IPathResolver _pathResolver;
    private readonly IRepositoryDiscoveryService _repositoryDiscoveryService;
    private readonly IReadmeFinder _readmeFinder;
    private readonly IJavaCodeCollector _javaCodeCollector;
    private readonly IAiReviewClient _aiReviewClient;
    private readonly IReviewNormalizer _reviewNormalizer;
    private readonly IReviewStore _reviewStore;
    private readonly IWorkflowLogger _logger;

    public GenerateReviewsWorkflow(
        IPathResolver pathResolver,
        IRepositoryDiscoveryService repositoryDiscoveryService,
        IReadmeFinder readmeFinder,
        IJavaCodeCollector javaCodeCollector,
        IAiReviewClient aiReviewClient,
        IReviewNormalizer reviewNormalizer,
        IReviewStore reviewStore,
        IWorkflowLogger logger)
    {
        _pathResolver = pathResolver;
        _repositoryDiscoveryService = repositoryDiscoveryService;
        _readmeFinder = readmeFinder;
        _javaCodeCollector = javaCodeCollector;
        _aiReviewClient = aiReviewClient;
        _reviewNormalizer = reviewNormalizer;
        _reviewStore = reviewStore;
        _logger = logger;
    }

    public async Task<GenerateReviewsResult> RunAsync(
        GenerateReviewsRequest request,
        IProgress<GenerateReviewsProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        AppPaths paths = await _pathResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
        string repositoriesDirectory = paths.GetHomeworkReposDirectory(request.HomeworkNumber);
        string reviewsDirectory = paths.GetHomeworkReviewsDirectory(request.HomeworkNumber);

        if (!Directory.Exists(repositoriesDirectory))
        {
            throw new DirectoryNotFoundException($"Repositories directory was not found: {repositoriesDirectory}");
        }

        Directory.CreateDirectory(reviewsDirectory);
        _logger.Info($"Repositories directory: {repositoriesDirectory}");
        _logger.Info($"Reviews directory: {reviewsDirectory}");

        string? readmePath = await _readmeFinder.FindFirstReadmeAsync(repositoriesDirectory, cancellationToken).ConfigureAwait(false);
        string? readmeText = null;
        if (readmePath is not null)
        {
            readmeText = await _readmeFinder.ReadAsync(readmePath, cancellationToken).ConfigureAwait(false);
            _logger.Info($"README loaded: {readmePath}");
        }
        else
        {
            _logger.Warning("No README.md found. Reviews will be generated without assignment text.");
        }

        IReadOnlyList<LocalRepository> repositories = await _repositoryDiscoveryService
            .FindRepositoriesAsync(repositoriesDirectory, request.RepoFilter, cancellationToken)
            .ConfigureAwait(false);

        if (repositories.Count == 0)
        {
            _logger.Warning("No repositories found.");
            return new GenerateReviewsResult
            {
                RepositoriesDirectory = repositoriesDirectory,
                ReviewsDirectory = reviewsDirectory
            };
        }

        List<string> writtenReviewFiles = new();
        List<string> errors = new();
        int processedCount = 0;
        int failedCount = 0;
        int index = 0;

        foreach (LocalRepository repository in repositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            progress?.Report(new GenerateReviewsProgress(index, repositories.Count, $"Processing {repository.Name} ...", repository.Name));
            _logger.Info($"[{index}/{repositories.Count}] Repo: {repository.Name}");

            try
            {
                IReadOnlyList<string> javaFiles = await _javaCodeCollector
                    .FindRelevantJavaFilesAsync(repository.DirectoryPath, cancellationToken)
                    .ConfigureAwait(false);

                if (javaFiles.Count == 0)
                {
                    _logger.Warning($"Skip {repository.Name}: no relevant Java files found.");
                    continue;
                }

                foreach (string javaFile in javaFiles)
                {
                    _logger.Info($"  - {Path.GetRelativePath(repository.DirectoryPath, javaFile)}");
                }

                string codeBlob = await _javaCodeCollector
                    .BuildNumberedCodeBlobAsync(repository.DirectoryPath, javaFiles, request.MaxChars, cancellationToken)
                    .ConfigureAwait(false);

                ReviewDocument rawReview = await _aiReviewClient.GenerateReviewAsync(
                    repository.Name,
                    codeBlob,
                    readmeText,
                    request.Model,
                    request.Temperature,
                    cancellationToken).ConfigureAwait(false);

                IReadOnlyList<string> validFiles = javaFiles
                    .Select(file => Path.GetRelativePath(repository.DirectoryPath, file).Replace(Path.DirectorySeparatorChar, '/'))
                    .ToList();

                ReviewDocument normalizedReview = _reviewNormalizer.Normalize(repository.Name, validFiles, rawReview);
                string reviewFilePath = Path.Combine(reviewsDirectory, $"{repository.Name}.json");

                await _reviewStore.SaveAsync(reviewFilePath, normalizedReview, cancellationToken).ConfigureAwait(false);
                writtenReviewFiles.Add(reviewFilePath);
                processedCount++;

                int findingCount = normalizedReview.Files.Sum(file => file.Findings.Count);
                _logger.Info($"Saved: {reviewFilePath}");
                _logger.Info($"Findings: {findingCount}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failedCount++;
                string message = $"{repository.Name}: {exception.Message}";
                errors.Add(message);
                _logger.Error(message, exception);
            }

            if (request.SleepSeconds > 0 && index < repositories.Count)
            {
                int delayMilliseconds = (int)Math.Round(request.SleepSeconds * 1000.0);
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }

        return new GenerateReviewsResult
        {
            RepositoriesDirectory = repositoriesDirectory,
            ReviewsDirectory = reviewsDirectory,
            ProcessedRepositoryCount = processedCount,
            FailedRepositoryCount = failedCount,
            WrittenReviewFiles = writtenReviewFiles,
            Errors = errors
        };
    }

    private static void ValidateRequest(GenerateReviewsRequest request)
    {
        if (request.HomeworkNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.HomeworkNumber), "Homework number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("Model must not be empty.", nameof(request));
        }

        if (request.MaxChars <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxChars), "Max chars must be greater than zero.");
        }

        if (request.Temperature < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Temperature), "Temperature must not be negative.");
        }

        if (request.SleepSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.SleepSeconds), "Sleep seconds must not be negative.");
        }
    }
}
