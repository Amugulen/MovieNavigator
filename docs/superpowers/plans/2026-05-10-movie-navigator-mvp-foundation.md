# Movie Navigator MVP Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first working Windows MVP vertical slice for Movie Navigator: scan authorized folders, index eligible videos, manage hierarchical TAGs, search/filter results, lock adult library data, open files with the default player, and log safe file operations.

**Architecture:** Use a .NET 8 solution with a WPF shell and testable core services. Keep domain logic in `MovieNavigator.Core`, persistence and OS integration in `MovieNavigator.Infrastructure`, UI and view models in `MovieNavigator.App`, and automated tests in `MovieNavigator.Tests`.

**Tech Stack:** C# 12, .NET 8, WPF, SQLite via `Microsoft.Data.Sqlite`, xUnit, FluentAssertions, FFmpeg/ffprobe process adapter behind an interface.

---

## Scope Check

The full design contains multiple subsystems: online metadata scraping, AI classification, adult isolation, file operations, multi-drive indexing, TAG management, and UI workflows. This plan implements the first testable MVP foundation. It includes extension interfaces for metadata and AI, but the first working build only uses local scanning, local screenshots/metadata hooks, manual metadata fields, TAG suggestions as user-confirmed data, and a disabled-by-default AI settings surface.

This plan produces working software on its own:

- A desktop app launches.
- A user can add scan folders.
- The scanner indexes eligible video files using duration/size/path rules.
- Items can be searched by file name, path, TAG key, TAG display name, and media fields.
- Items can be browsed by drive and directory.
- Adult library data remains locked and invisible until password unlock.
- Users can add/edit TAGs and mark items as confirmed or pending.
- Users can open a file with the default player.
- Move/copy/rename operations are planned, confirmed, executed, and logged.

## File Structure

Create this solution structure:

```text
MovieNavigator.sln
src/MovieNavigator.Core/MovieNavigator.Core.csproj
src/MovieNavigator.Core/Media/MediaItem.cs
src/MovieNavigator.Core/Media/MediaLibraryType.cs
src/MovieNavigator.Core/Media/MediaStatus.cs
src/MovieNavigator.Core/Scanning/ScanRules.cs
src/MovieNavigator.Core/Scanning/VideoFileCandidate.cs
src/MovieNavigator.Core/Scanning/MediaScanner.cs
src/MovieNavigator.Core/Tags/TagDefinition.cs
src/MovieNavigator.Core/Tags/TagAssignment.cs
src/MovieNavigator.Core/Tags/TagKey.cs
src/MovieNavigator.Core/Search/SearchQuery.cs
src/MovieNavigator.Core/Search/SearchResult.cs
src/MovieNavigator.Core/Security/AdultVaultState.cs
src/MovieNavigator.Core/Security/PasswordHasher.cs
src/MovieNavigator.Core/FileOperations/FileOperationPlan.cs
src/MovieNavigator.Core/FileOperations/FileOperationType.cs
src/MovieNavigator.Core/FileOperations/FileOperationResult.cs
src/MovieNavigator.Core/FileOperations/FileOperationLogEntry.cs
src/MovieNavigator.Core/Abstractions/IMediaRepository.cs
src/MovieNavigator.Core/Abstractions/ITagRepository.cs
src/MovieNavigator.Core/Abstractions/IFileSystem.cs
src/MovieNavigator.Core/Abstractions/IProcessLauncher.cs
src/MovieNavigator.Core/Abstractions/IVideoInspector.cs
src/MovieNavigator.Core/Abstractions/IClock.cs
src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj
src/MovieNavigator.Infrastructure/Persistence/SqliteConnectionFactory.cs
src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs
src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs
src/MovieNavigator.Infrastructure/Persistence/SqliteTagRepository.cs
src/MovieNavigator.Infrastructure/FileSystem/WindowsFileSystem.cs
src/MovieNavigator.Infrastructure/FileSystem/DefaultProcessLauncher.cs
src/MovieNavigator.Infrastructure/Video/FfprobeVideoInspector.cs
src/MovieNavigator.Infrastructure/Time/SystemClock.cs
src/MovieNavigator.App/MovieNavigator.App.csproj
src/MovieNavigator.App/App.xaml
src/MovieNavigator.App/App.xaml.cs
src/MovieNavigator.App/MainWindow.xaml
src/MovieNavigator.App/MainWindow.xaml.cs
src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs
src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs
src/MovieNavigator.App/ViewModels/TagNodeViewModel.cs
src/MovieNavigator.App/ViewModels/PendingItemViewModel.cs
src/MovieNavigator.App/Services/AppBootstrapper.cs
src/MovieNavigator.App/Services/DialogService.cs
tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj
tests/MovieNavigator.Tests/Scanning/ScanRulesTests.cs
tests/MovieNavigator.Tests/Scanning/MediaScannerTests.cs
tests/MovieNavigator.Tests/Tags/TagKeyTests.cs
tests/MovieNavigator.Tests/Persistence/SqliteRepositoryTests.cs
tests/MovieNavigator.Tests/Search/SearchTests.cs
tests/MovieNavigator.Tests/Security/PasswordHasherTests.cs
tests/MovieNavigator.Tests/Security/AdultVaultVisibilityTests.cs
tests/MovieNavigator.Tests/FileOperations/FileOperationPlannerTests.cs
tests/MovieNavigator.Tests/TestDoubles/FakeFileSystem.cs
tests/MovieNavigator.Tests/TestDoubles/FakeVideoInspector.cs
tests/MovieNavigator.Tests/TestDoubles/FakeClock.cs
```

Responsibility boundaries:

- `Core` contains pure business rules and interfaces. It must not reference WPF, SQLite, Windows APIs, or FFmpeg directly.
- `Infrastructure` implements persistence, filesystem access, process launching, and ffprobe integration.
- `App` wires services and displays workflows. UI logic should stay in view models where possible.
- `Tests` covers domain rules, persistence behavior, search, adult visibility, and file-operation safety.

## Task 1: Create .NET Solution Skeleton

**Files:**
- Create: `MovieNavigator.sln`
- Create: `src/MovieNavigator.Core/MovieNavigator.Core.csproj`
- Create: `src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj`
- Create: `src/MovieNavigator.App/MovieNavigator.App.csproj`
- Create: `tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n MovieNavigator
dotnet new classlib -n MovieNavigator.Core -o src/MovieNavigator.Core --framework net8.0
dotnet new classlib -n MovieNavigator.Infrastructure -o src/MovieNavigator.Infrastructure --framework net8.0
dotnet new wpf -n MovieNavigator.App -o src/MovieNavigator.App --framework net8.0-windows
dotnet new xunit -n MovieNavigator.Tests -o tests/MovieNavigator.Tests --framework net8.0
dotnet sln MovieNavigator.sln add src/MovieNavigator.Core/MovieNavigator.Core.csproj
dotnet sln MovieNavigator.sln add src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj
dotnet sln MovieNavigator.sln add src/MovieNavigator.App/MovieNavigator.App.csproj
dotnet sln MovieNavigator.sln add tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj
dotnet add src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj reference src/MovieNavigator.Core/MovieNavigator.Core.csproj
dotnet add src/MovieNavigator.App/MovieNavigator.App.csproj reference src/MovieNavigator.Core/MovieNavigator.Core.csproj
dotnet add src/MovieNavigator.App/MovieNavigator.App.csproj reference src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj
dotnet add tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj reference src/MovieNavigator.Core/MovieNavigator.Core.csproj
dotnet add tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj reference src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj
```

Expected: all projects are added to the solution.

- [ ] **Step 2: Add required packages**

Run:

```powershell
dotnet add src/MovieNavigator.Infrastructure/MovieNavigator.Infrastructure.csproj package Microsoft.Data.Sqlite
dotnet add tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj package FluentAssertions
```

Expected: package references are added.

- [ ] **Step 3: Build empty solution**

Run:

```powershell
dotnet build MovieNavigator.sln
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```powershell
git add MovieNavigator.sln src tests
git commit -m "chore: scaffold Movie Navigator solution"
```

## Task 2: Add Core Media and TAG Domain Models

**Files:**
- Create: `src/MovieNavigator.Core/Media/MediaLibraryType.cs`
- Create: `src/MovieNavigator.Core/Media/MediaStatus.cs`
- Create: `src/MovieNavigator.Core/Media/MediaItem.cs`
- Create: `src/MovieNavigator.Core/Tags/TagKey.cs`
- Create: `src/MovieNavigator.Core/Tags/TagDefinition.cs`
- Create: `src/MovieNavigator.Core/Tags/TagAssignment.cs`
- Test: `tests/MovieNavigator.Tests/Tags/TagKeyTests.cs`

- [ ] **Step 1: Write failing TAG key tests**

Create `tests/MovieNavigator.Tests/Tags/TagKeyTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Tests.Tags;

public sealed class TagKeyTests
{
    [Theory]
    [InlineData("country.soviet_union")]
    [InlineData("person.director.akira_kurosawa")]
    [InlineData("storage.drive.d")]
    public void Parse_accepts_lowercase_dotted_tag_keys(string raw)
    {
        var key = TagKey.Parse(raw);

        key.Value.Should().Be(raw);
        key.ParentKey.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Country.Soviet")]
    [InlineData("country..soviet")]
    [InlineData(".country")]
    [InlineData("country.")]
    [InlineData("country/soviet")]
    public void Parse_rejects_invalid_tag_keys(string raw)
    {
        var act = () => TagKey.Parse(raw);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Root_key_has_no_parent()
    {
        var key = TagKey.Parse("country");

        key.ParentKey.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter TagKeyTests
```

Expected: fails because `TagKey` does not exist.

- [ ] **Step 3: Add domain model code**

Create `src/MovieNavigator.Core/Tags/TagKey.cs`:

```csharp
using System.Text.RegularExpressions;

namespace MovieNavigator.Core.Tags;

public readonly record struct TagKey(string Value)
{
    private static readonly Regex ValidPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)*(?:\\.[a-z0-9]+(?:_[a-z0-9]+)*)*$", RegexOptions.Compiled);

    public string? ParentKey
    {
        get
        {
            var lastDot = Value.LastIndexOf('.');
            return lastDot < 0 ? null : Value[..lastDot];
        }
    }

    public static TagKey Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !ValidPattern.IsMatch(raw))
        {
            throw new ArgumentException($"Invalid tag key: {raw}", nameof(raw));
        }

        return new TagKey(raw);
    }

    public override string ToString() => Value;
}
```

Create `src/MovieNavigator.Core/Media/MediaLibraryType.cs`:

```csharp
namespace MovieNavigator.Core.Media;

public enum MediaLibraryType
{
    Normal = 0,
    Adult = 1
}
```

Create `src/MovieNavigator.Core/Media/MediaStatus.cs`:

```csharp
namespace MovieNavigator.Core.Media;

public enum MediaStatus
{
    Pending = 0,
    Confirmed = 1,
    Ignored = 2,
    Offline = 3
}
```

Create `src/MovieNavigator.Core/Media/MediaItem.cs`:

```csharp
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Media;

public sealed record MediaItem(
    Guid Id,
    MediaLibraryType LibraryType,
    MediaStatus Status,
    string FilePath,
    string FileName,
    string DriveKey,
    long SizeBytes,
    TimeSpan Duration,
    int? Width,
    int? Height,
    string? Title,
    string? OriginalTitle,
    int? Year,
    string? Summary,
    IReadOnlyCollection<TagKey> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

Create `src/MovieNavigator.Core/Tags/TagDefinition.cs`:

```csharp
namespace MovieNavigator.Core.Tags;

public sealed record TagDefinition(
    TagKey Key,
    string DisplayNameZh,
    string DisplayNameEn,
    IReadOnlyCollection<string> Aliases,
    TagKey? ParentKey);
```

Create `src/MovieNavigator.Core/Tags/TagAssignment.cs`:

```csharp
namespace MovieNavigator.Core.Tags;

public sealed record TagAssignment(Guid MediaId, TagKey TagKey);
```

- [ ] **Step 4: Run test and verify it passes**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter TagKeyTests
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/MovieNavigator.Core tests/MovieNavigator.Tests/Tags/TagKeyTests.cs
git commit -m "feat: add media and tag domain models"
```

## Task 3: Implement Scan Rules and Media Scanner

**Files:**
- Create: `src/MovieNavigator.Core/Scanning/ScanRules.cs`
- Create: `src/MovieNavigator.Core/Scanning/VideoFileCandidate.cs`
- Create: `src/MovieNavigator.Core/Scanning/MediaScanner.cs`
- Create: `src/MovieNavigator.Core/Abstractions/IFileSystem.cs`
- Create: `src/MovieNavigator.Core/Abstractions/IVideoInspector.cs`
- Test: `tests/MovieNavigator.Tests/Scanning/ScanRulesTests.cs`
- Test: `tests/MovieNavigator.Tests/Scanning/MediaScannerTests.cs`
- Test: `tests/MovieNavigator.Tests/TestDoubles/FakeFileSystem.cs`
- Test: `tests/MovieNavigator.Tests/TestDoubles/FakeVideoInspector.cs`

- [ ] **Step 1: Write failing scan rule tests**

Create `tests/MovieNavigator.Tests/Scanning/ScanRulesTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Scanning;

namespace MovieNavigator.Tests.Scanning;

public sealed class ScanRulesTests
{
    [Fact]
    public void Default_rules_accept_long_video_in_authorized_directory()
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        var accepted = rules.ShouldConsiderPath(@"D:\Movies\Soviet\film.mkv");

        accepted.Should().BeTrue();
    }

    [Theory]
    [InlineData(@"D:\Games\cutscene.mp4")]
    [InlineData(@"D:\Movies\Cache\clip.mp4")]
    [InlineData(@"D:\Movies\Temp\clip.mp4")]
    public void Default_rules_reject_excluded_directories(string path)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies", @"D:\Games"]);

        rules.ShouldConsiderPath(path).Should().BeFalse();
    }

    [Theory]
    [InlineData("clip.txt")]
    [InlineData("image.jpg")]
    public void Default_rules_reject_non_video_extensions(string fileName)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        rules.ShouldConsiderPath($@"D:\Movies\{fileName}").Should().BeFalse();
    }

    [Theory]
    [InlineData(19, 200_000_000, false)]
    [InlineData(40, 50_000_000, false)]
    [InlineData(40, 200_000_000, true)]
    public void Default_rules_apply_duration_and_size_thresholds(int minutes, long bytes, bool expected)
    {
        var rules = ScanRules.CreateDefault([@"D:\Movies"]);

        var result = rules.ShouldIncludeAnalyzedVideo(TimeSpan.FromMinutes(minutes), bytes);

        result.Should().Be(expected);
    }
}
```

- [ ] **Step 2: Add scan rule implementation**

Create `src/MovieNavigator.Core/Scanning/ScanRules.cs`:

```csharp
namespace MovieNavigator.Core.Scanning;

public sealed class ScanRules
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m2ts"
    };

    private static readonly string[] ExcludedPathParts =
    [
        @"\cache\",
        @"\temp\",
        @"\tmp\",
        @"\node_modules\",
        @"\steamapps\",
        @"\game\",
        @"\games\"
    ];

    public IReadOnlyCollection<string> AuthorizedRoots { get; }
    public TimeSpan MinimumDuration { get; }
    public long MinimumSizeBytes { get; }

    private ScanRules(IReadOnlyCollection<string> authorizedRoots, TimeSpan minimumDuration, long minimumSizeBytes)
    {
        AuthorizedRoots = authorizedRoots;
        MinimumDuration = minimumDuration;
        MinimumSizeBytes = minimumSizeBytes;
    }

    public static ScanRules CreateDefault(IReadOnlyCollection<string> authorizedRoots)
    {
        return new ScanRules(
            authorizedRoots.Select(NormalizeRoot).ToArray(),
            TimeSpan.FromMinutes(20),
            100L * 1024L * 1024L);
    }

    public bool ShouldConsiderPath(string path)
    {
        var normalized = NormalizePath(path);
        if (!AuthorizedRoots.Any(root => normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (ExcludedPathParts.Any(part => normalized.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return VideoExtensions.Contains(Path.GetExtension(normalized));
    }

    public bool ShouldIncludeAnalyzedVideo(TimeSpan duration, long sizeBytes)
    {
        return duration >= MinimumDuration && sizeBytes >= MinimumSizeBytes;
    }

    private static string NormalizeRoot(string root)
    {
        var normalized = NormalizePath(root);
        return normalized.EndsWith('\\') ? normalized : normalized + "\\";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', '\\').Trim();
    }
}
```

- [ ] **Step 3: Run scan rule tests**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter ScanRulesTests
```

Expected: all tests pass.

- [ ] **Step 4: Write failing scanner tests**

Create `tests/MovieNavigator.Tests/Scanning/MediaScannerTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Scanning;
using MovieNavigator.Tests.TestDoubles;

namespace MovieNavigator.Tests.Scanning;

public sealed class MediaScannerTests
{
    [Fact]
    public async Task ScanAsync_indexes_only_eligible_videos()
    {
        var fileSystem = new FakeFileSystem([
            new FakeFileEntry(@"D:\Movies\War\film.mkv", 2_000_000_000),
            new FakeFileEntry(@"D:\Movies\War\clip.mp4", 200_000_000),
            new FakeFileEntry(@"D:\Movies\War\notes.txt", 1_000)
        ]);
        var inspector = new FakeVideoInspector(new Dictionary<string, VideoInspectionResult>
        {
            [@"D:\Movies\War\film.mkv"] = new(TimeSpan.FromMinutes(125), 1920, 1080, "h264"),
            [@"D:\Movies\War\clip.mp4"] = new(TimeSpan.FromMinutes(4), 1280, 720, "h264")
        });
        var scanner = new MediaScanner(fileSystem, inspector);

        var result = await scanner.ScanAsync(ScanRules.CreateDefault([@"D:\Movies"]), MediaLibraryType.Normal, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FilePath.Should().Be(@"D:\Movies\War\film.mkv");
        result[0].Status.Should().Be(MediaStatus.Pending);
        result[0].DriveKey.Should().Be("D:");
    }
}
```

- [ ] **Step 5: Add scanner abstractions and implementation**

Create `src/MovieNavigator.Core/Abstractions/IFileSystem.cs`:

```csharp
namespace MovieNavigator.Core.Abstractions;

public interface IFileSystem
{
    IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(IReadOnlyCollection<string> roots, CancellationToken cancellationToken);
}

public sealed record FileSystemVideoCandidate(string FullPath, long SizeBytes);
```

Create `src/MovieNavigator.Core/Abstractions/IVideoInspector.cs`:

```csharp
namespace MovieNavigator.Core.Abstractions;

public interface IVideoInspector
{
    Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken);
}

public sealed record VideoInspectionResult(TimeSpan Duration, int? Width, int? Height, string? Codec);
```

Create `src/MovieNavigator.Core/Scanning/VideoFileCandidate.cs`:

```csharp
namespace MovieNavigator.Core.Scanning;

public sealed record VideoFileCandidate(
    string FilePath,
    long SizeBytes,
    TimeSpan Duration,
    int? Width,
    int? Height,
    string? Codec);
```

Create `src/MovieNavigator.Core/Scanning/MediaScanner.cs`:

```csharp
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Scanning;

public sealed class MediaScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly IVideoInspector _videoInspector;

    public MediaScanner(IFileSystem fileSystem, IVideoInspector videoInspector)
    {
        _fileSystem = fileSystem;
        _videoInspector = videoInspector;
    }

    public async Task<IReadOnlyList<MediaItem>> ScanAsync(ScanRules rules, MediaLibraryType libraryType, CancellationToken cancellationToken)
    {
        var items = new List<MediaItem>();

        await foreach (var file in _fileSystem.EnumerateFilesAsync(rules.AuthorizedRoots, cancellationToken))
        {
            if (!rules.ShouldConsiderPath(file.FullPath))
            {
                continue;
            }

            var inspection = await _videoInspector.InspectAsync(file.FullPath, cancellationToken);
            if (!rules.ShouldIncludeAnalyzedVideo(inspection.Duration, file.SizeBytes))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            items.Add(new MediaItem(
                Guid.NewGuid(),
                libraryType,
                MediaStatus.Pending,
                file.FullPath,
                Path.GetFileName(file.FullPath),
                GetDriveKey(file.FullPath),
                file.SizeBytes,
                inspection.Duration,
                inspection.Width,
                inspection.Height,
                null,
                null,
                null,
                null,
                Array.Empty<Tags.TagKey>(),
                now,
                now));
        }

        return items;
    }

    private static string GetDriveKey(string path)
    {
        var root = Path.GetPathRoot(path);
        return string.IsNullOrWhiteSpace(root) ? "unknown" : root.TrimEnd('\\');
    }
}
```

Create `tests/MovieNavigator.Tests/TestDoubles/FakeFileSystem.cs`:

```csharp
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Tests.TestDoubles;

public sealed record FakeFileEntry(string FullPath, long SizeBytes);

public sealed class FakeFileSystem : IFileSystem
{
    private readonly IReadOnlyList<FakeFileEntry> _files;

    public FakeFileSystem(IReadOnlyList<FakeFileEntry> files)
    {
        _files = files;
    }

    public async IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(IReadOnlyCollection<string> roots, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var file in _files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new FileSystemVideoCandidate(file.FullPath, file.SizeBytes);
        }
    }
}
```

Create `tests/MovieNavigator.Tests/TestDoubles/FakeVideoInspector.cs`:

```csharp
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Tests.TestDoubles;

public sealed class FakeVideoInspector : IVideoInspector
{
    private readonly IReadOnlyDictionary<string, VideoInspectionResult> _results;

    public FakeVideoInspector(IReadOnlyDictionary<string, VideoInspectionResult> results)
    {
        _results = results;
    }

    public Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken)
    {
        return Task.FromResult(_results[filePath]);
    }
}
```

- [ ] **Step 6: Run scanner tests**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter "ScanRulesTests|MediaScannerTests"
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/MovieNavigator.Core tests/MovieNavigator.Tests
git commit -m "feat: add media scanning rules"
```

## Task 4: Add SQLite Schema and Repositories

**Files:**
- Create: `src/MovieNavigator.Core/Abstractions/IMediaRepository.cs`
- Create: `src/MovieNavigator.Core/Abstractions/ITagRepository.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteConnectionFactory.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteTagRepository.cs`
- Test: `tests/MovieNavigator.Tests/Persistence/SqliteRepositoryTests.cs`

- [ ] **Step 1: Write failing repository test**

Create `tests/MovieNavigator.Tests/Persistence/SqliteRepositoryTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Persistence;

public sealed class SqliteRepositoryTests
{
    [Fact]
    public async Task Save_and_search_media_by_tag_and_path()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var mediaRepository = new SqliteMediaRepository(factory);
        var tagRepository = new SqliteTagRepository(factory);

        var tag = new TagDefinition(TagKey.Parse("country.soviet_union"), "苏联", "Soviet Union", ["USSR", "СССР"], TagKey.Parse("country"));
        await tagRepository.UpsertAsync(tag, CancellationToken.None);

        var item = new MediaItem(
            Guid.NewGuid(),
            MediaLibraryType.Normal,
            MediaStatus.Pending,
            @"D:\Movies\Soviet\film.mkv",
            "film.mkv",
            "D:",
            2_000_000_000,
            TimeSpan.FromMinutes(125),
            1920,
            1080,
            "未识别影片",
            null,
            1972,
            null,
            [tag.Key],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await mediaRepository.UpsertAsync(item, CancellationToken.None);

        var results = await mediaRepository.SearchAsync("苏联", MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);

        results.Should().ContainSingle();
        results[0].FilePath.Should().Be(item.FilePath);
    }
}
```

- [ ] **Step 2: Add repository interfaces**

Create `src/MovieNavigator.Core/Abstractions/IMediaRepository.cs`:

```csharp
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.Abstractions;

public interface IMediaRepository
{
    Task UpsertAsync(MediaItem item, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> SearchAsync(string query, MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken);
    Task<IReadOnlyList<MediaItem>> GetByDriveAsync(string driveKey, MediaLibraryType libraryType, CancellationToken cancellationToken);
}
```

Create `src/MovieNavigator.Core/Abstractions/ITagRepository.cs`:

```csharp
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Core.Abstractions;

public interface ITagRepository
{
    Task UpsertAsync(TagDefinition tag, CancellationToken cancellationToken);
    Task<IReadOnlyList<TagDefinition>> GetTreeAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Add SQLite initializer and repositories**

Create `src/MovieNavigator.Infrastructure/Persistence/SqliteConnectionFactory.cs`:

```csharp
using Microsoft.Data.Sqlite;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _heldConnection;

    private SqliteConnectionFactory(string connectionString, SqliteConnection? heldConnection = null)
    {
        _connectionString = connectionString;
        _heldConnection = heldConnection;
    }

    public static SqliteConnectionFactory File(string path)
    {
        return new SqliteConnectionFactory($"Data Source={path}");
    }

    public static SqliteConnectionFactory InMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new SqliteConnectionFactory("Data Source=:memory:", connection);
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (_heldConnection is not null)
        {
            return _heldConnection;
        }

        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public ValueTask DisposeAsync()
    {
        return _heldConnection is null ? ValueTask.CompletedTask : _heldConnection.DisposeAsync();
    }
}
```

Create `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`:

```csharp
using Microsoft.Data.Sqlite;

namespace MovieNavigator.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(SqliteConnectionFactory factory, CancellationToken cancellationToken)
    {
        var connection = await factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        CREATE TABLE IF NOT EXISTS media_items (
            id TEXT PRIMARY KEY,
            library_type INTEGER NOT NULL,
            status INTEGER NOT NULL,
            file_path TEXT NOT NULL UNIQUE,
            file_name TEXT NOT NULL,
            drive_key TEXT NOT NULL,
            size_bytes INTEGER NOT NULL,
            duration_seconds REAL NOT NULL,
            width INTEGER NULL,
            height INTEGER NULL,
            title TEXT NULL,
            original_title TEXT NULL,
            year INTEGER NULL,
            summary TEXT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS tags (
            key TEXT PRIMARY KEY,
            display_zh TEXT NOT NULL,
            display_en TEXT NOT NULL,
            aliases TEXT NOT NULL,
            parent_key TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS media_tags (
            media_id TEXT NOT NULL,
            tag_key TEXT NOT NULL,
            PRIMARY KEY (media_id, tag_key)
        );

        CREATE VIRTUAL TABLE IF NOT EXISTS media_search USING fts5(
            media_id UNINDEXED,
            content
        );
        """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
```

Create `src/MovieNavigator.Infrastructure/Persistence/SqliteTagRepository.cs`:

```csharp
using Microsoft.Data.Sqlite;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteTagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteTagRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task UpsertAsync(TagDefinition tag, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        INSERT INTO tags(key, display_zh, display_en, aliases, parent_key)
        VALUES ($key, $displayZh, $displayEn, $aliases, $parentKey)
        ON CONFLICT(key) DO UPDATE SET
            display_zh = excluded.display_zh,
            display_en = excluded.display_en,
            aliases = excluded.aliases,
            parent_key = excluded.parent_key;
        """;
        command.Parameters.AddWithValue("$key", tag.Key.Value);
        command.Parameters.AddWithValue("$displayZh", tag.DisplayNameZh);
        command.Parameters.AddWithValue("$displayEn", tag.DisplayNameEn);
        command.Parameters.AddWithValue("$aliases", string.Join("|", tag.Aliases));
        command.Parameters.AddWithValue("$parentKey", (object?)tag.ParentKey?.Value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TagDefinition>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT key, display_zh, display_en, aliases, parent_key FROM tags ORDER BY key;";
        var tags = new List<TagDefinition>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var parent = reader.IsDBNull(4) ? (TagKey?)null : TagKey.Parse(reader.GetString(4));
            tags.Add(new TagDefinition(
                TagKey.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3).Split('|', StringSplitOptions.RemoveEmptyEntries),
                parent));
        }

        return tags;
    }
}
```

Create `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`:

```csharp
using Microsoft.Data.Sqlite;
using MovieNavigator.Core.Abstractions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;

namespace MovieNavigator.Infrastructure.Persistence;

public sealed class SqliteMediaRepository : IMediaRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteMediaRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task UpsertAsync(MediaItem item, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var upsert = connection.CreateCommand();
        upsert.Transaction = (SqliteTransaction)transaction;
        upsert.CommandText = """
        INSERT INTO media_items(id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at)
        VALUES ($id, $libraryType, $status, $filePath, $fileName, $driveKey, $sizeBytes, $durationSeconds, $width, $height, $title, $originalTitle, $year, $summary, $createdAt, $updatedAt)
        ON CONFLICT(file_path) DO UPDATE SET
            library_type = excluded.library_type,
            status = excluded.status,
            file_name = excluded.file_name,
            drive_key = excluded.drive_key,
            size_bytes = excluded.size_bytes,
            duration_seconds = excluded.duration_seconds,
            width = excluded.width,
            height = excluded.height,
            title = excluded.title,
            original_title = excluded.original_title,
            year = excluded.year,
            summary = excluded.summary,
            updated_at = excluded.updated_at;
        """;
        AddMediaParameters(upsert, item);
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        var deleteTags = connection.CreateCommand();
        deleteTags.Transaction = (SqliteTransaction)transaction;
        deleteTags.CommandText = "DELETE FROM media_tags WHERE media_id = $id;";
        deleteTags.Parameters.AddWithValue("$id", item.Id.ToString());
        await deleteTags.ExecuteNonQueryAsync(cancellationToken);

        foreach (var tag in item.Tags)
        {
            var insertTag = connection.CreateCommand();
            insertTag.Transaction = (SqliteTransaction)transaction;
            insertTag.CommandText = "INSERT OR IGNORE INTO media_tags(media_id, tag_key) VALUES ($id, $tagKey);";
            insertTag.Parameters.AddWithValue("$id", item.Id.ToString());
            insertTag.Parameters.AddWithValue("$tagKey", tag.Value);
            await insertTag.ExecuteNonQueryAsync(cancellationToken);
        }

        var deleteSearch = connection.CreateCommand();
        deleteSearch.Transaction = (SqliteTransaction)transaction;
        deleteSearch.CommandText = "DELETE FROM media_search WHERE media_id = $id;";
        deleteSearch.Parameters.AddWithValue("$id", item.Id.ToString());
        await deleteSearch.ExecuteNonQueryAsync(cancellationToken);

        var insertSearch = connection.CreateCommand();
        insertSearch.Transaction = (SqliteTransaction)transaction;
        insertSearch.CommandText = "INSERT INTO media_search(media_id, content) VALUES ($id, $content);";
        insertSearch.Parameters.AddWithValue("$id", item.Id.ToString());
        insertSearch.Parameters.AddWithValue("$content", await BuildSearchContentAsync(connection, (SqliteTransaction)transaction, item, cancellationToken));
        await insertSearch.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaItem>> SearchAsync(string query, MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT m.id, m.library_type, m.status, m.file_path, m.file_name, m.drive_key, m.size_bytes, m.duration_seconds, m.width, m.height, m.title, m.original_title, m.year, m.summary, m.created_at, m.updated_at
        FROM media_items m
        JOIN media_search s ON s.media_id = m.id
        WHERE s.content MATCH $query
          AND m.library_type = $libraryType
          AND ($includeAdult = 1 OR m.library_type <> 1)
        ORDER BY m.updated_at DESC;
        """;
        command.Parameters.AddWithValue("$query", EscapeFtsQuery(query));
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);
        command.Parameters.AddWithValue("$includeAdult", includeAdultWhenUnlocked ? 1 : 0);
        return await ReadMediaItemsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MediaItem>> GetByDriveAsync(string driveKey, MediaLibraryType libraryType, CancellationToken cancellationToken)
    {
        var connection = await _factory.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at
        FROM media_items
        WHERE drive_key = $driveKey AND library_type = $libraryType
        ORDER BY file_path;
        """;
        command.Parameters.AddWithValue("$driveKey", driveKey);
        command.Parameters.AddWithValue("$libraryType", (int)libraryType);
        return await ReadMediaItemsAsync(command, cancellationToken);
    }

    private static void AddMediaParameters(SqliteCommand command, MediaItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$libraryType", (int)item.LibraryType);
        command.Parameters.AddWithValue("$status", (int)item.Status);
        command.Parameters.AddWithValue("$filePath", item.FilePath);
        command.Parameters.AddWithValue("$fileName", item.FileName);
        command.Parameters.AddWithValue("$driveKey", item.DriveKey);
        command.Parameters.AddWithValue("$sizeBytes", item.SizeBytes);
        command.Parameters.AddWithValue("$durationSeconds", item.Duration.TotalSeconds);
        command.Parameters.AddWithValue("$width", (object?)item.Width ?? DBNull.Value);
        command.Parameters.AddWithValue("$height", (object?)item.Height ?? DBNull.Value);
        command.Parameters.AddWithValue("$title", (object?)item.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("$originalTitle", (object?)item.OriginalTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("$year", (object?)item.Year ?? DBNull.Value);
        command.Parameters.AddWithValue("$summary", (object?)item.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", item.UpdatedAt.ToString("O"));
    }

    private static async Task<string> BuildSearchContentAsync(SqliteConnection connection, SqliteTransaction transaction, MediaItem item, CancellationToken cancellationToken)
    {
        var tagText = new List<string>();
        foreach (var tag in item.Tags)
        {
            tagText.Add(tag.Value);
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT display_zh, display_en, aliases FROM tags WHERE key = $key;";
            command.Parameters.AddWithValue("$key", tag.Value);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                tagText.Add(reader.GetString(0));
                tagText.Add(reader.GetString(1));
                tagText.AddRange(reader.GetString(2).Split('|', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        return string.Join(' ', new[]
        {
            item.FilePath,
            item.FileName,
            item.Title,
            item.OriginalTitle,
            item.Year?.ToString(),
            item.Summary,
            string.Join(' ', tagText)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string EscapeFtsQuery(string query)
    {
        return "\"" + query.Replace("\"", "\"\"") + "\"";
    }

    private static async Task<IReadOnlyList<MediaItem>> ReadMediaItemsAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var items = new List<MediaItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new MediaItem(
                Guid.Parse(reader.GetString(0)),
                (MediaLibraryType)reader.GetInt32(1),
                (MediaStatus)reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetInt64(6),
                TimeSpan.FromSeconds(reader.GetDouble(7)),
                reader.IsDBNull(8) ? null : reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetInt32(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                Array.Empty<TagKey>(),
                DateTimeOffset.Parse(reader.GetString(14)),
                DateTimeOffset.Parse(reader.GetString(15))));
        }

        return items;
    }
}
```

- [ ] **Step 4: Run repository test**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter SqliteRepositoryTests
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/MovieNavigator.Core src/MovieNavigator.Infrastructure tests/MovieNavigator.Tests/Persistence
git commit -m "feat: add sqlite media repository"
```

## Task 5: Add Adult Vault Password and Visibility Rules

**Files:**
- Create: `src/MovieNavigator.Core/Security/AdultVaultState.cs`
- Create: `src/MovieNavigator.Core/Security/PasswordHasher.cs`
- Test: `tests/MovieNavigator.Tests/Security/PasswordHasherTests.cs`
- Test: `tests/MovieNavigator.Tests/Security/AdultVaultVisibilityTests.cs`

- [ ] **Step 1: Write failing security tests**

Create `tests/MovieNavigator.Tests/Security/PasswordHasherTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Security;

namespace MovieNavigator.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_correct_password_and_rejects_wrong_password()
    {
        var hash = PasswordHasher.Hash("123456");

        PasswordHasher.Verify("123456", hash).Should().BeTrue();
        PasswordHasher.Verify("000000", hash).Should().BeFalse();
        hash.Should().NotContain("123456");
    }
}
```

Create `tests/MovieNavigator.Tests/Security/AdultVaultVisibilityTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Security;

namespace MovieNavigator.Tests.Security;

public sealed class AdultVaultVisibilityTests
{
    [Fact]
    public void Locked_vault_blocks_adult_queries()
    {
        var state = AdultVaultState.Locked();

        state.CanQueryAdultLibrary.Should().BeFalse();
        state.CanShowAdultTags.Should().BeFalse();
    }

    [Fact]
    public void Unlocked_vault_allows_adult_queries()
    {
        var state = AdultVaultState.Unlocked(DateTimeOffset.UtcNow.AddMinutes(15));

        state.CanQueryAdultLibrary.Should().BeTrue();
        state.CanShowAdultTags.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Add password hasher and vault state**

Create `src/MovieNavigator.Core/Security/PasswordHasher.cs`:

```csharp
using System.Security.Cryptography;

namespace MovieNavigator.Core.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"pbkdf2-sha256.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('.');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
```

Create `src/MovieNavigator.Core/Security/AdultVaultState.cs`:

```csharp
namespace MovieNavigator.Core.Security;

public sealed record AdultVaultState(bool IsUnlocked, DateTimeOffset? UnlockExpiresAt)
{
    public bool CanQueryAdultLibrary => IsUnlocked;
    public bool CanShowAdultTags => IsUnlocked;

    public static AdultVaultState Locked() => new(false, null);

    public static AdultVaultState Unlocked(DateTimeOffset expiresAt) => new(true, expiresAt);

    public AdultVaultState LockIfExpired(DateTimeOffset now)
    {
        return UnlockExpiresAt is not null && now >= UnlockExpiresAt.Value ? Locked() : this;
    }
}
```

- [ ] **Step 3: Run security tests**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter "PasswordHasherTests|AdultVaultVisibilityTests"
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```powershell
git add src/MovieNavigator.Core/Security tests/MovieNavigator.Tests/Security
git commit -m "feat: add adult vault security rules"
```

## Task 6: Add File Operation Planning and Logging Model

**Files:**
- Create: `src/MovieNavigator.Core/FileOperations/FileOperationType.cs`
- Create: `src/MovieNavigator.Core/FileOperations/FileOperationPlan.cs`
- Create: `src/MovieNavigator.Core/FileOperations/FileOperationResult.cs`
- Create: `src/MovieNavigator.Core/FileOperations/FileOperationLogEntry.cs`
- Test: `tests/MovieNavigator.Tests/FileOperations/FileOperationPlannerTests.cs`

- [ ] **Step 1: Write failing file operation tests**

Create `tests/MovieNavigator.Tests/FileOperations/FileOperationPlannerTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.FileOperations;
using MovieNavigator.Core.Media;

namespace MovieNavigator.Tests.FileOperations;

public sealed class FileOperationPlannerTests
{
    [Fact]
    public void Move_plan_requires_confirmation_and_blocks_offline_source()
    {
        var plan = FileOperationPlan.Create(
            FileOperationType.Move,
            MediaLibraryType.Normal,
            @"D:\Movies\film.mkv",
            @"E:\Sorted\film.mkv",
            sourceIsOnline: false);

        plan.RequiresConfirmation.Should().BeTrue();
        plan.CanExecute.Should().BeFalse();
        plan.BlockReason.Should().Be("Source file is offline.");
    }

    [Fact]
    public void Adult_to_normal_transfer_requires_extra_confirmation()
    {
        var plan = FileOperationPlan.CreateLibraryTransfer(
            MediaLibraryType.Adult,
            MediaLibraryType.Normal,
            @"X:\Adult\film.mkv",
            @"D:\Movies\film.mkv",
            sourceIsOnline: true);

        plan.RequiresConfirmation.Should().BeTrue();
        plan.RequiresVisibilityBoundaryConfirmation.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Add file operation models**

Create `src/MovieNavigator.Core/FileOperations/FileOperationType.cs`:

```csharp
namespace MovieNavigator.Core.FileOperations;

public enum FileOperationType
{
    Copy = 0,
    Move = 1,
    Rename = 2,
    DeleteToRecycleBin = 3
}
```

Create `src/MovieNavigator.Core/FileOperations/FileOperationPlan.cs`:

```csharp
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationPlan(
    FileOperationType OperationType,
    MediaLibraryType SourceLibrary,
    MediaLibraryType TargetLibrary,
    string SourcePath,
    string TargetPath,
    bool RequiresConfirmation,
    bool RequiresVisibilityBoundaryConfirmation,
    bool CanExecute,
    string? BlockReason)
{
    public static FileOperationPlan Create(FileOperationType operationType, MediaLibraryType library, string sourcePath, string targetPath, bool sourceIsOnline)
    {
        return CreateLibraryTransfer(library, library, sourcePath, targetPath, sourceIsOnline, operationType);
    }

    public static FileOperationPlan CreateLibraryTransfer(MediaLibraryType sourceLibrary, MediaLibraryType targetLibrary, string sourcePath, string targetPath, bool sourceIsOnline, FileOperationType operationType = FileOperationType.Move)
    {
        var crossesVisibilityBoundary = sourceLibrary != targetLibrary;
        return new FileOperationPlan(
            operationType,
            sourceLibrary,
            targetLibrary,
            sourcePath,
            targetPath,
            RequiresConfirmation: true,
            RequiresVisibilityBoundaryConfirmation: crossesVisibilityBoundary,
            CanExecute: sourceIsOnline,
            BlockReason: sourceIsOnline ? null : "Source file is offline.");
    }
}
```

Create `src/MovieNavigator.Core/FileOperations/FileOperationResult.cs`:

```csharp
namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationResult(bool Succeeded, string? ErrorMessage)
{
    public static FileOperationResult Success() => new(true, null);
    public static FileOperationResult Failure(string errorMessage) => new(false, errorMessage);
}
```

Create `src/MovieNavigator.Core/FileOperations/FileOperationLogEntry.cs`:

```csharp
using MovieNavigator.Core.Media;

namespace MovieNavigator.Core.FileOperations;

public sealed record FileOperationLogEntry(
    Guid Id,
    FileOperationType OperationType,
    DateTimeOffset OccurredAt,
    string SourcePath,
    string TargetPath,
    long? FileSizeBytes,
    Guid? MediaIdBefore,
    Guid? MediaIdAfter,
    MediaLibraryType LibraryType,
    bool Succeeded,
    string? ErrorMessage);
```

- [ ] **Step 3: Run file operation tests**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter FileOperationPlannerTests
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```powershell
git add src/MovieNavigator.Core/FileOperations tests/MovieNavigator.Tests/FileOperations
git commit -m "feat: add safe file operation plans"
```

## Task 7: Add Windows Filesystem, Process Launcher, and ffprobe Adapter

**Files:**
- Create: `src/MovieNavigator.Core/Abstractions/IProcessLauncher.cs`
- Create: `src/MovieNavigator.Infrastructure/FileSystem/WindowsFileSystem.cs`
- Create: `src/MovieNavigator.Infrastructure/FileSystem/DefaultProcessLauncher.cs`
- Create: `src/MovieNavigator.Infrastructure/Video/FfprobeVideoInspector.cs`

- [ ] **Step 1: Add process launcher interface**

Create `src/MovieNavigator.Core/Abstractions/IProcessLauncher.cs`:

```csharp
namespace MovieNavigator.Core.Abstractions;

public interface IProcessLauncher
{
    void OpenWithDefaultApplication(string path);
    void OpenFolder(string folderPath);
}
```

- [ ] **Step 2: Add Windows filesystem implementation**

Create `src/MovieNavigator.Infrastructure/FileSystem/WindowsFileSystem.cs`:

```csharp
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.FileSystem;

public sealed class WindowsFileSystem : IFileSystem
{
    public async IAsyncEnumerable<FileSystemVideoCandidate> EnumerateFilesAsync(IReadOnlyCollection<string> roots, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileInfo info;
                try
                {
                    info = new FileInfo(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                await Task.Yield();
                yield return new FileSystemVideoCandidate(info.FullName, info.Length);
            }
        }
    }
}
```

- [ ] **Step 3: Add default process launcher**

Create `src/MovieNavigator.Infrastructure/FileSystem/DefaultProcessLauncher.cs`:

```csharp
using System.Diagnostics;
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.FileSystem;

public sealed class DefaultProcessLauncher : IProcessLauncher
{
    public void OpenWithDefaultApplication(string path)
    {
        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }

    public void OpenFolder(string folderPath)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folderPath}\"")
        {
            UseShellExecute = true
        });
    }
}
```

- [ ] **Step 4: Add ffprobe inspector**

Create `src/MovieNavigator.Infrastructure/Video/FfprobeVideoInspector.cs`:

```csharp
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using MovieNavigator.Core.Abstractions;

namespace MovieNavigator.Infrastructure.Video;

public sealed class FfprobeVideoInspector : IVideoInspector
{
    private readonly string _ffprobePath;

    public FfprobeVideoInspector(string ffprobePath)
    {
        _ffprobePath = ffprobePath;
    }

    public async Task<VideoInspectionResult> InspectAsync(string filePath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v error -select_streams v:0 -show_entries stream=width,height,codec_name -show_entries format=duration -of json \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("ffprobe process did not start.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            return new VideoInspectionResult(TimeSpan.Zero, null, null, null);
        }

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        var format = root.GetProperty("format");
        var durationText = format.GetProperty("duration").GetString() ?? "0";
        var duration = TimeSpan.FromSeconds(double.Parse(durationText, CultureInfo.InvariantCulture));

        var streams = root.GetProperty("streams");
        if (streams.GetArrayLength() == 0)
        {
            return new VideoInspectionResult(duration, null, null, null);
        }

        var stream = streams[0];
        var width = stream.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : (int?)null;
        var height = stream.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : (int?)null;
        var codec = stream.TryGetProperty("codec_name", out var codecElement) ? codecElement.GetString() : null;
        return new VideoInspectionResult(duration, width, height, codec);
    }
}
```

- [ ] **Step 5: Build solution**

Run:

```powershell
dotnet build MovieNavigator.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/MovieNavigator.Core/Abstractions src/MovieNavigator.Infrastructure
git commit -m "feat: add windows integration adapters"
```

## Task 8: Add WPF Shell and Main Workflows

**Files:**
- Modify: `src/MovieNavigator.App/App.xaml`
- Modify: `src/MovieNavigator.App/App.xaml.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml`
- Modify: `src/MovieNavigator.App/MainWindow.xaml.cs`
- Create: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Create: `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`
- Create: `src/MovieNavigator.App/ViewModels/TagNodeViewModel.cs`
- Create: `src/MovieNavigator.App/ViewModels/PendingItemViewModel.cs`
- Create: `src/MovieNavigator.App/Services/AppBootstrapper.cs`
- Create: `src/MovieNavigator.App/Services/DialogService.cs`

- [ ] **Step 1: Add simple view models**

Create `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`:

```csharp
namespace MovieNavigator.App.ViewModels;

public sealed record MediaCardViewModel(
    string Title,
    string FilePath,
    string Duration,
    string Resolution,
    string Status,
    string DriveKey,
    IReadOnlyList<string> Tags);
```

Create `src/MovieNavigator.App/ViewModels/TagNodeViewModel.cs`:

```csharp
namespace MovieNavigator.App.ViewModels;

public sealed record TagNodeViewModel(string Key, string DisplayName, IReadOnlyList<TagNodeViewModel> Children);
```

Create `src/MovieNavigator.App/ViewModels/PendingItemViewModel.cs`:

```csharp
namespace MovieNavigator.App.ViewModels;

public sealed record PendingItemViewModel(string FileName, string FilePath, string HintText);
```

Create `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MovieNavigator.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private string _selectedSection = "首页";

    public ObservableCollection<string> NavigationItems { get; } =
    [
        "首页",
        "普通库",
        "按硬盘浏览",
        "TAG索引",
        "待确认",
        "设置",
        "成人库（锁定）"
    ];

    public ObservableCollection<string> DriveItems { get; } =
    [
        "D盘 0部",
        "E盘 0部",
        "移动硬盘A 离线 0部"
    ];

    public ObservableCollection<TagNodeViewModel> Tags { get; } =
    [
        new("country.soviet_union", "国家 / 苏联", []),
        new("genre.war", "类型 / 战争", []),
        new("decade.1970s", "年代 / 1970年代", []),
        new("storage.drive.d", "存储 / D盘", []),
        new("status.unconfirmed", "状态 / 待确认", [])
    ];

    public ObservableCollection<MediaCardViewModel> MediaCards { get; } =
    [
        new("未识别影片", @"D:\Movies\example.mkv", "02:14:36", "1080p", "待补充", "D:", ["country.soviet_union", "genre.war"])
    ];

    public ObservableCollection<PendingItemViewModel> PendingItems { get; } =
    [
        new("example.mkv", @"D:\Movies\example.mkv", "填写标题 / 导演 / 介绍网址后可重新识别")
    ];

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public string SelectedSection
    {
        get => _selectedSection;
        set => SetField(ref _selectedSection, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

- [ ] **Step 2: Replace MainWindow XAML with operation-focused layout**

Replace `src/MovieNavigator.App/MainWindow.xaml`:

```xml
<Window x:Class="MovieNavigator.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="本地影视资料库" Height="820" Width="1280"
        Background="#111827">
    <Grid Margin="16">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220" />
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="340" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="64" />
            <RowDefinition Height="*" />
            <RowDefinition Height="170" />
        </Grid.RowDefinitions>

        <TextBlock Grid.ColumnSpan="3" Text="本地影视资料库" Foreground="#F9FAFB" FontSize="28" FontWeight="SemiBold" VerticalAlignment="Center" />

        <Border Grid.Row="1" Grid.RowSpan="2" Background="#1F2937" CornerRadius="14" Padding="12" Margin="0,0,12,0">
            <StackPanel>
                <TextBlock Text="导航" Foreground="#9CA3AF" Margin="0,0,0,8" />
                <ListBox ItemsSource="{Binding NavigationItems}" SelectedItem="{Binding SelectedSection}" Background="Transparent" Foreground="#F9FAFB" BorderThickness="0" />
                <TextBlock Text="硬盘" Foreground="#9CA3AF" Margin="0,20,0,8" />
                <ListBox ItemsSource="{Binding DriveItems}" Background="Transparent" Foreground="#F9FAFB" BorderThickness="0" />
                <TextBlock Text="TAG索引" Foreground="#9CA3AF" Margin="0,20,0,8" />
                <ListBox ItemsSource="{Binding Tags}" DisplayMemberPath="DisplayName" Background="Transparent" Foreground="#F9FAFB" BorderThickness="0" />
            </StackPanel>
        </Border>

        <Grid Grid.Column="1" Grid.Row="1" Margin="0,0,12,12">
            <Grid.RowDefinitions>
                <RowDefinition Height="48" />
                <RowDefinition Height="48" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" FontSize="16" Padding="12" Background="#0F172A" Foreground="#F9FAFB" BorderBrush="#374151" />
            <TextBlock Grid.Row="1" Text="筛选结果：苏联 + 战争 + 1970年代 + D盘 + 时长&gt;20分钟" Foreground="#FBBF24" VerticalAlignment="Center" />
            <ListBox Grid.Row="2" ItemsSource="{Binding MediaCards}" Background="#111827" BorderThickness="0">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Background="#1F2937" CornerRadius="12" Padding="12" Margin="0,0,0,10">
                            <StackPanel>
                                <TextBlock Text="{Binding Title}" Foreground="#F9FAFB" FontSize="18" FontWeight="SemiBold" />
                                <TextBlock Text="{Binding FilePath}" Foreground="#9CA3AF" />
                                <TextBlock Foreground="#D1D5DB">
                                    <Run Text="{Binding Duration}" />
                                    <Run Text=" · " />
                                    <Run Text="{Binding Resolution}" />
                                    <Run Text=" · " />
                                    <Run Text="{Binding Status}" />
                                </TextBlock>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Grid>

        <Border Grid.Column="2" Grid.Row="1" Background="#1F2937" CornerRadius="14" Padding="16" Margin="0,0,0,12">
            <StackPanel>
                <TextBlock Text="影片详情" Foreground="#F9FAFB" FontSize="22" FontWeight="SemiBold" />
                <TextBlock Text="路径：D:\Movies\example.mkv" Foreground="#D1D5DB" Margin="0,16,0,0" TextWrapping="Wrap" />
                <TextBlock Text="时长：02:14:36" Foreground="#D1D5DB" />
                <TextBlock Text="分辨率：1080p" Foreground="#D1D5DB" />
                <TextBlock Text="导演：待确认" Foreground="#D1D5DB" />
                <TextBlock Text="国家：苏联" Foreground="#D1D5DB" />
                <TextBlock Text="TAG：country.soviet_union" Foreground="#FBBF24" Margin="0,8,0,16" />
                <Button Content="默认播放器打开" Margin="0,0,0,8" />
                <Button Content="打开所在目录" Margin="0,0,0,8" />
                <Button Content="移动/整理" Margin="0,0,0,8" />
                <Button Content="重新识别" Margin="0,0,0,8" />
                <Button Content="添加TAG" />
            </StackPanel>
        </Border>

        <Border Grid.Column="1" Grid.ColumnSpan="2" Grid.Row="2" Background="#1F2937" CornerRadius="14" Padding="16">
            <StackPanel>
                <TextBlock Text="待确认工作台" Foreground="#F9FAFB" FontSize="20" FontWeight="SemiBold" />
                <TextBlock Text="填写线索：标题 / 番号 / 导演 / 介绍网址 -> AI文本分类 -> 用户确认后入库" Foreground="#D1D5DB" Margin="0,8,0,8" />
                <ListBox ItemsSource="{Binding PendingItems}" DisplayMemberPath="HintText" Height="72" Background="#0F172A" Foreground="#F9FAFB" BorderThickness="0" />
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 3: Wire DataContext**

Replace `src/MovieNavigator.App/MainWindow.xaml.cs`:

```csharp
using System.Windows;
using MovieNavigator.App.ViewModels;

namespace MovieNavigator.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
```

- [ ] **Step 4: Build and run**

Run:

```powershell
dotnet build MovieNavigator.sln
dotnet run --project src/MovieNavigator.App/MovieNavigator.App.csproj
```

Expected: desktop window opens with navigation, drive list, TAG list, media cards, details, and pending workbench.

- [ ] **Step 5: Commit**

```powershell
git add src/MovieNavigator.App
git commit -m "feat: add MVP desktop shell"
```

## Task 9: Add Search, Drive Browse, and Adult Visibility Integration

**Files:**
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/MovieNavigator.App/Services/AppBootstrapper.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml.cs`
- Test: `tests/MovieNavigator.Tests/Search/SearchTests.cs`

- [ ] **Step 1: Write search behavior test against repository**

Create `tests/MovieNavigator.Tests/Search/SearchTests.cs`:

```csharp
using FluentAssertions;
using MovieNavigator.Core.Media;
using MovieNavigator.Core.Tags;
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.Tests.Search;

public sealed class SearchTests
{
    [Fact]
    public async Task Normal_search_does_not_return_adult_items_when_vault_locked()
    {
        await using var factory = SqliteConnectionFactory.InMemory();
        await DatabaseInitializer.InitializeAsync(factory, CancellationToken.None);
        var repository = new SqliteMediaRepository(factory);

        await repository.UpsertAsync(CreateItem(MediaLibraryType.Normal, @"D:\Movies\normal.mkv", "普通影片", "D:", [TagKey.Parse("genre.war")]), CancellationToken.None);
        await repository.UpsertAsync(CreateItem(MediaLibraryType.Adult, @"X:\Adult\adult.mkv", "成人影片", "X:", [TagKey.Parse("adult.topic.sample")]), CancellationToken.None);

        var results = await repository.SearchAsync("影片", MediaLibraryType.Normal, includeAdultWhenUnlocked: false, CancellationToken.None);

        results.Should().ContainSingle();
        results[0].LibraryType.Should().Be(MediaLibraryType.Normal);
    }

    private static MediaItem CreateItem(MediaLibraryType libraryType, string path, string title, string drive, IReadOnlyCollection<TagKey> tags)
    {
        return new MediaItem(Guid.NewGuid(), libraryType, MediaStatus.Confirmed, path, Path.GetFileName(path), drive, 200_000_000, TimeSpan.FromMinutes(90), 1920, 1080, title, null, 1970, null, tags, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }
}
```

- [ ] **Step 2: Run search test**

Run:

```powershell
dotnet test tests/MovieNavigator.Tests/MovieNavigator.Tests.csproj --filter SearchTests
```

Expected: test passes with repository visibility rules.

- [ ] **Step 3: Add bootstrapper for local app database**

Create `src/MovieNavigator.App/Services/AppBootstrapper.cs`:

```csharp
using MovieNavigator.Infrastructure.Persistence;

namespace MovieNavigator.App.Services;

public static class AppBootstrapper
{
    public static async Task<SqliteConnectionFactory> InitializeAsync(CancellationToken cancellationToken)
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MovieNavigator");
        Directory.CreateDirectory(appData);
        var databasePath = Path.Combine(appData, "normal.db");
        var factory = SqliteConnectionFactory.File(databasePath);
        await DatabaseInitializer.InitializeAsync(factory, cancellationToken);
        return factory;
    }
}
```

- [ ] **Step 4: Update app startup to initialize database**

Replace `src/MovieNavigator.App/App.xaml.cs`:

```csharp
using System.Windows;
using MovieNavigator.App.Services;

namespace MovieNavigator.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await AppBootstrapper.InitializeAsync(CancellationToken.None);
        var window = new MainWindow();
        window.Show();
    }
}
```

Replace `src/MovieNavigator.App/App.xaml`:

```xml
<Application x:Class="MovieNavigator.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
    </Application.Resources>
</Application>
```

- [ ] **Step 5: Build and test**

Run:

```powershell
dotnet test MovieNavigator.sln
dotnet build MovieNavigator.sln
```

Expected: all tests pass and build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/MovieNavigator.App tests/MovieNavigator.Tests/Search
git commit -m "feat: wire app database and search visibility"
```

## Task 10: Add README and First-Run Notes

**Files:**
- Create: `README.md`

- [ ] **Step 1: Create README**

Create `README.md`:

```markdown
# Movie Navigator

Movie Navigator is a Windows personal local media indexer for managing large movie collections across internal drives, external drives, and offline disks.

## MVP Scope

- Local video scanning with duration and size thresholds.
- SQLite index with full-text search.
- Hierarchical TAG model using stable English keys and multilingual display names.
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
```

- [ ] **Step 2: Run final validation**

Run:

```powershell
dotnet test MovieNavigator.sln
dotnet build MovieNavigator.sln
git status --short
```

Expected: tests pass, build succeeds, and only `README.md` is uncommitted.

- [ ] **Step 3: Commit**

```powershell
git add README.md
git commit -m "docs: add project README"
```

## Task 11: Push MVP Plan and Prepare Execution

**Files:**
- No code files.

- [ ] **Step 1: Verify clean state**

Run:

```powershell
git status --short --branch
```

Expected:

```text
## main...origin/main
```

- [ ] **Step 2: Push commits**

Run:

```powershell
git push
```

Expected: local commits are pushed to `origin/main`.

- [ ] **Step 3: Record execution notes**

At the end of implementation, summarize:

```text
Implemented MVP foundation:
- .NET/WPF solution scaffold
- Scan rules and media scanner
- SQLite repository and FTS search
- Hierarchical TAG domain model
- Adult vault password/visibility rules
- Safe file operation planning
- Windows adapters for filesystem, default-player open, and ffprobe
- First WPF shell
- README

Validation:
- dotnet test MovieNavigator.sln
- dotnet build MovieNavigator.sln
```
