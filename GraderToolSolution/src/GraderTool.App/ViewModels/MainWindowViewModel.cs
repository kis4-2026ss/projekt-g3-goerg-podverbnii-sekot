using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.App.ViewModels.Pages;

namespace GraderTool.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private PageViewModelBase _currentPage;

    public MainWindowViewModel(
        ValidationHeaderViewModel validationHeader,
        DashboardViewModel dashboard,
        ValidationViewModel validation,
        FetchReposViewModel fetchRepos,
        GenerateReviewsViewModel generateReviews,
        PushReviewsViewModel pushReviews,
        SettingsViewModel settings,
        LogsViewModel logs)
    {
        ValidationHeader = validationHeader;
        CurrentPage = dashboard;

        NavigationItems = new ObservableCollection<NavigationItemViewModel>
        {
            new("Dashboard", "⌂", dashboard, new RelayCommand(() => NavigateTo(dashboard))),
            new("Validierung", "✓", validation, new RelayCommand(() => NavigateTo(validation))),
            new("Fetch Repos", "↓", fetchRepos, new RelayCommand(() => NavigateTo(fetchRepos))),
            new("Generate Reviews", "✎", generateReviews, new RelayCommand(() => NavigateTo(generateReviews))),
            new("Push Reviews", "↑", pushReviews, new RelayCommand(() => NavigateTo(pushReviews))),
            new("Settings", "⚙", settings, new RelayCommand(() => NavigateTo(settings))),
            new("Logs", "≡", logs, new RelayCommand(() => NavigateTo(logs)))
        };
    }

    public ValidationHeaderViewModel ValidationHeader { get; }
    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    private void NavigateTo(PageViewModelBase page)
    {
        CurrentPage = page;
    }
}
