namespace GraderTool.Infrastructure.Settings;

public sealed class AppSettings
{
    public string? ProjectRoot { get; set; }
    public string? GraderRoot { get; set; }
    public string? StudentsFile { get; set; }
    public string DefaultReviewModel { get; set; } = "gemini-2.5-flash";
    public bool DryRunByDefault { get; set; } = true;
    public bool RequireSubmitConfirmation { get; set; } = true;
}
