namespace GraderTool.Infrastructure.Settings;

public sealed class AppSettings
{
    public string? ProjectRoot { get; set; }
    public string? GraderRoot { get; set; }
    public string? StudentsFile { get; set; }

    public string DefaultMatchBy { get; set; } = "login";
    public string DefaultReviewModel { get; set; } = "gemini-2.5-flash";
    public int DefaultMaxChars { get; set; } = 50000;
    public double DefaultTemperature { get; set; } = 0.2;

    public bool DryRunByDefault { get; set; } = true;
    public bool RequireSubmitConfirmation { get; set; } = true;
}
