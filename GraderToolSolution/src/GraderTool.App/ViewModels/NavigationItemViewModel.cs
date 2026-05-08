using System.Windows.Input;

namespace GraderTool.App.ViewModels;

public sealed class NavigationItemViewModel
{
    public NavigationItemViewModel(string title, string icon, PageViewModelBase page, ICommand command)
    {
        Title = title;
        Icon = icon;
        Page = page;
        Command = command;
    }

    public string Title { get; }
    public string Icon { get; }
    public PageViewModelBase Page { get; }
    public ICommand Command { get; }
}
