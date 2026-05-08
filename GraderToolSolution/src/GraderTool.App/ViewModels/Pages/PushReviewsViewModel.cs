using CommunityToolkit.Mvvm.ComponentModel;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class PushReviewsViewModel : PageViewModelBase
{
    [ObservableProperty]
    private int _homeworkNumber = 1;

    [ObservableProperty]
    private string _repoFilter = string.Empty;

    [ObservableProperty]
    private bool _dryRun = true;

    [ObservableProperty]
    private bool _submitImmediately;

    public PushReviewsViewModel()
        : base("Push Reviews", "Erstellt Pending Reviews auf GitHub und kann sie optional direkt submitten.")
    {
    }
}
