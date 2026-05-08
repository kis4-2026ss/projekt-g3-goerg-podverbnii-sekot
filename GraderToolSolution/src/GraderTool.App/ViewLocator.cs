using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace GraderTool.App;

public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "Keine Ansicht ausgewählt." };
        }

        string viewModelName = data.GetType().FullName ?? string.Empty;
        string viewName = viewModelName
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        Type? type = Type.GetType(viewName);
        if (type is null)
        {
            return new TextBlock { Text = $"View nicht gefunden: {viewName}" };
        }

        return Activator.CreateInstance(type) as Control
            ?? new TextBlock { Text = $"View konnte nicht erstellt werden: {viewName}" };
    }

    public bool Match(object? data) => data is ViewModels.ViewModelBase;
}
