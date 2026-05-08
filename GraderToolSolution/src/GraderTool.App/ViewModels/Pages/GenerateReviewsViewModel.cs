using CommunityToolkit.Mvvm.ComponentModel;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class GenerateReviewsViewModel : PageViewModelBase
{
    [ObservableProperty]
    private int _homeworkNumber = 1;

    [ObservableProperty]
    private string _model = "gemini-2.5-flash";

    [ObservableProperty]
    private string _repoFilter = string.Empty;

    [ObservableProperty]
    private int _maxChars = 50000;

    [ObservableProperty]
    private double _temperature = 0.2;

    public GenerateReviewsViewModel()
        : base("Generate Reviews", "Erzeugt Review-JSON-Dateien für lokale Java-Repositories.")
    {
    }
}
