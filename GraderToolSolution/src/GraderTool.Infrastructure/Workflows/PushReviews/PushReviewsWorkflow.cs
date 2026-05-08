using System.Text;
using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Core.Workflows.PushReviews;

namespace GraderTool.Infrastructure.Workflows.PushReviews;

public sealed class PushReviewsWorkflow : IPushReviewsWorkflow
{
    private readonly IPathResolver _pathResolver;
    private readonly IRepositoryDiscoveryService _repositoryDiscoveryService;
    private readonly IReviewStore _reviewStore;
    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly IPullRequestService _pullRequestService;
    private readonly IReviewCommentMapper _reviewCommentMapper;
    private readonly IPullRequestReviewService _pullRequestReviewService;
    private readonly IWorkflowLogger _logger;

    public PushReviewsWorkflow(
        IPathResolver pathResolver,
        IRepositoryDiscoveryService repositoryDiscoveryService,
        IReviewStore reviewStore,
        IGitRepositoryService gitRepositoryService,
        IPullRequestService pullRequestService,
        IReviewCommentMapper reviewCommentMapper,
        IPullRequestReviewService pullRequestReviewService,
        IWorkflowLogger logger)
    {
        _pathResolver = pathResolver;
        _repositoryDiscoveryService = repositoryDiscoveryService;
        _reviewStore = reviewStore;
        _gitRepositoryService = gitRepositoryService;
        _pullRequestService = pullRequestService;
        _reviewCommentMapper = reviewCommentMapper;
        _pullRequestReviewService = pullRequestReviewService;
        _logger = logger;
    }

    public async Task<PushReviewsResult> RunAsync(
        PushReviewsRequest request,
        IProgress<PushReviewsProgress>? progress = null,
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

        if (!Directory.Exists(reviewsDirectory))
        {
            throw new DirectoryNotFoundException($"Reviews directory was not found: {reviewsDirectory}");
        }

        IReadOnlyList<LocalRepository> repositories = await _repositoryDiscoveryService
            .FindRepositoriesAsync(repositoriesDirectory, request.RepoFilter, cancellationToken)
            .ConfigureAwait(false);

        List<string> messages = new();
        int processedCount = 0;
        int createdPendingReviewCount = 0;
        int submittedReviewCount = 0;
        int skippedCount = 0;
        int failedCount = 0;
        int index = 0;

        foreach (LocalRepository repository in repositories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            string startMessage = $"Processing {repository.Name} ...";
            _logger.Info(startMessage);
            progress?.Report(new PushReviewsProgress(index, repositories.Count, startMessage, repository.Name));

            try
            {
                string reviewFilePath = Path.Combine(reviewsDirectory, $"{repository.Name}.json");
                ReviewDocument? review = await _reviewStore.LoadAsync(reviewFilePath, cancellationToken).ConfigureAwait(false);
                if (review is null)
                {
                    skippedCount++;
                    string message = $"Skip {repository.Name}: no review JSON found.";
                    messages.Add(message);
                    _logger.Warning(message);
                    continue;
                }

                string remoteUrl = await _gitRepositoryService.GetOriginRemoteUrlAsync(repository.DirectoryPath, cancellationToken).ConfigureAwait(false);
                (string owner, string repositoryName) = _gitRepositoryService.ParseGitHubRemoteUrl(remoteUrl);

                PullRequestInfo? pullRequest = await _pullRequestService.FindFeedbackPullRequestAsync(
                    owner,
                    repositoryName,
                    request.FeedbackBranchHint,
                    cancellationToken).ConfigureAwait(false);

                if (pullRequest is null)
                {
                    skippedCount++;
                    string message = $"Skip {repository.Name}: no open feedback PR found.";
                    messages.Add(message);
                    _logger.Warning(message);
                    continue;
                }

                IReadOnlyList<PullRequestFile> pullRequestFiles = await _pullRequestService
                    .GetPullRequestFilesAsync(owner, repositoryName, pullRequest.Number, cancellationToken)
                    .ConfigureAwait(false);

                List<ReviewFinding> findings = review.Files.SelectMany(file => file.Findings).ToList();
                (IReadOnlyList<ReviewCommentTarget> inlineComments, IReadOnlyList<ReviewFinding> leftovers) =
                    _reviewCommentMapper.MapFindingsToPullRequestPositions(findings, pullRequestFiles);

                string body = BuildReviewBody(review.Summary, leftovers);
                long? reviewId = await _pullRequestReviewService.CreatePendingReviewAsync(
                    owner,
                    repositoryName,
                    pullRequest.Number,
                    body,
                    inlineComments,
                    request.DryRun,
                    cancellationToken).ConfigureAwait(false);

                processedCount++;
                createdPendingReviewCount++;

                string createdMessage = request.DryRun
                    ? $"Dry run complete for {repository.Name}: {inlineComments.Count} inline comments, {leftovers.Count} leftovers."
                    : $"Pending review created for {repository.Name}: {inlineComments.Count} inline comments, {leftovers.Count} leftovers.";
                messages.Add(createdMessage);
                _logger.Info(createdMessage);

                if (request.SubmitImmediately && reviewId is not null)
                {
                    await _pullRequestReviewService.SubmitReviewAsync(
                        owner,
                        repositoryName,
                        pullRequest.Number,
                        reviewId.Value,
                        "Automatisches Erstfeedback.",
                        request.DryRun,
                        cancellationToken).ConfigureAwait(false);

                    submittedReviewCount++;
                    string submitMessage = $"Review submitted for {repository.Name}.";
                    messages.Add(submitMessage);
                    _logger.Info(submitMessage);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failedCount++;
                string message = $"{repository.Name}: {exception.Message}";
                messages.Add(message);
                _logger.Error(message, exception);
            }
        }

        return new PushReviewsResult
        {
            ProcessedRepositoryCount = processedCount,
            CreatedPendingReviewCount = createdPendingReviewCount,
            SubmittedReviewCount = submittedReviewCount,
            SkippedRepositoryCount = skippedCount,
            FailedRepositoryCount = failedCount,
            Messages = messages,
            DryRun = request.DryRun
        };
    }

    private static string BuildReviewBody(string summary, IReadOnlyList<ReviewFinding> leftovers)
    {
        List<string> parts = new();

        if (!string.IsNullOrWhiteSpace(summary))
        {
            parts.Add(summary.Trim());
        }

        if (leftovers.Count > 0)
        {
            StringBuilder builder = new();
            builder.AppendLine("Nicht inline zuordenbare Punkte:");
            foreach (ReviewFinding finding in leftovers)
            {
                builder.AppendLine($"- `{finding.File}:{finding.Line}` — {finding.Comment}");
            }

            parts.Add(builder.ToString().TrimEnd());
        }

        return parts.Count == 0
            ? "Automatisches Erstfeedback."
            : string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    private static void ValidateRequest(PushReviewsRequest request)
    {
        if (request.HomeworkNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.HomeworkNumber), "Homework number must be greater than zero.");
        }
    }
}
