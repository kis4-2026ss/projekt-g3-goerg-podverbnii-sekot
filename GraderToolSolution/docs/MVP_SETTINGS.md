# MVP Settings

Dieses Paket macht die Settings-Seite funktional.

## Speicherort

Die Datei wird standardmäßig hier gespeichert:

- Windows: `%APPDATA%/GraderTool/appsettings.local.json`
- Linux/macOS: abhängig von `Environment.SpecialFolder.ApplicationData`, fallback auf App-Verzeichnis

## Enthaltene Settings

- Project Root Override
- Grader Root
- Students File
- Default Match Mode (`login` oder `roster`)
- Default Review Model
- Default Max Chars
- Default Temperature
- Dry Run by Default
- Require Submit Confirmation

## Test

```powershell
dotnet clean
dotnet build
dotnet run --project .\src\GraderTool.App\GraderTool.App.csproj
```

Danach Settings öffnen, Werte setzen, speichern und in der Settings-Seite die aufgelösten Pfade prüfen.
