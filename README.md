# Movie Navigator

Movie Navigator is a Windows personal local media indexer for managing large movie collections across internal drives, external drives, and offline disks.

## MVP Scope

- Local video scanning with duration and size thresholds.
- SQLite index with full-text search.
- Hierarchical TAG model using stable English keys and multilingual display names.
- Localization foundation with `zh-CN` and `en-US` resource files.
- Drive and directory browsing.
- Pending confirmation workbench for unidentified media.
- Adult library isolation design with password-based unlock.
- Safe file operation planning and operation logs.
- Default-player launch instead of built-in playback.

## Technology

- .NET 8
- WPF
- SQLite
- FFmpeg / ffprobe
- xUnit

## Development

```powershell
dotnet restore MovieNavigator.sln
dotnet test MovieNavigator.sln
dotnet run --project src/MovieNavigator.App/MovieNavigator.App.csproj
```

## Privacy Defaults

The application is designed to work locally first. Adult library content is not visible in normal mode, and cloud AI integration is disabled until the user explicitly configures an API key and confirms what text is sent.
