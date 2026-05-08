using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GraderTool.App.Composition;
using GraderTool.App.ViewModels;
using GraderTool.App.ViewModels.Pages;
using GraderTool.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.App;

public sealed partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();
        services.AddGraderToolAppServices();
        services.AddGraderToolViewModels();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal static class ViewModelServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ValidationHeaderViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ValidationViewModel>();
        services.AddSingleton<FetchReposViewModel>();
        services.AddSingleton<GenerateReviewsViewModel>();
        services.AddSingleton<PushReviewsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LogsViewModel>();
        return services;
    }
}
