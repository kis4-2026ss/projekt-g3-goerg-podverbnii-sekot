namespace GraderTool.Core.Models;

public sealed class AppPaths
{
    public string ProjectRoot { get; init; } = string.Empty;
    public string GraderRoot { get; init; } = string.Empty;
    public string ReposDirectory { get; init; } = string.Empty;
    public string ReviewsDirectory { get; init; } = string.Empty;
    public string LogsDirectory { get; init; } = string.Empty;
    public string StudentsFile { get; init; } = string.Empty;

    public string GetHomeworkReposDirectory(int homeworkNumber) =>
        Path.Combine(ReposDirectory, $"hue{homeworkNumber}");

    public string GetHomeworkReviewsDirectory(int homeworkNumber) =>
        Path.Combine(ReviewsDirectory, $"hue{homeworkNumber}");
}
