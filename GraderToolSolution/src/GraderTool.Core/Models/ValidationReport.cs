namespace GraderTool.Core.Models;

public sealed class ValidationReport
{
    public List<ValidationItem> Items { get; init; } = new();

    public bool HasErrors => Items.Any(item => !item.IsSuccessful && item.Severity == ValidationSeverity.Error);
    public bool IsSuccessful => !HasErrors;
}
