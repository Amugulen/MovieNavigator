# Movie Navigator Core Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the current scan-only shell into a usable local video indexer with persistent index, incremental scanning, real classification filters, thumbnails, switchable layouts, and a text-only AI classification configuration path.

**Architecture:** Keep the current .NET 8 WPF + SQLite architecture. Domain rules stay in `MovieNavigator.Core`, SQLite and Windows/FFmpeg adapters stay in `MovieNavigator.Infrastructure`, and UI behavior stays in `MovieNavigator.App` view models. Implement in thin vertical slices so each task is testable and does not pretend unfinished features are complete.

**Tech Stack:** C# 12, .NET 8, WPF, SQLite via `Microsoft.Data.Sqlite`, xUnit, FluentAssertions, Windows Explorer/default player integration, external `ffprobe`/`ffmpeg` executables.

---

## Scope Decision

This plan fixes the core product gap the user reported. It does not implement online movie scraping, adult-library unlock UI, real file moving, or cloud image/video analysis. AI integration in this plan is text-only configuration and suggestion generation plumbing; the first usable classifier must work locally from filenames, paths, metadata, and user-entered text.

## Current Truth

- The app can scan a selected folder and write rows to SQLite.
- The app does not load existing SQLite rows on startup, so the user has to scan again.
- Left navigation, drive, and TAG clicks only have minimal filtering; they are not a real classification system.
- No thumbnail path exists in the data model or database.
- `ffprobe` adapter exists, but UI quick scan does not use it.
- No `ffmpeg` thumbnail generator exists.
- No AI settings model, storage, or client exists.
- The UI is a plain list with poor layout and no view-mode switch.

## Target User Workflow

1. User double-clicks `run-movie-navigator.bat`.
2. App opens and immediately shows the existing index from SQLite.
3. App shows saved scan roots and offers "增量扫描".
4. Incremental scan adds new videos, updates changed files, and marks missing files as offline/missing.
5. Left panel shows real classification groups generated from indexed media: hard drive, status, extension/type, decade, resolution, duration bucket, and TAG.
6. Main area shows thumbnail cards by default, with a switch to compact list and detail list.
7. User can select a video, open it with the default player, open its folder, or inspect details.
8. User can configure AI API settings, then send text-only clues for classification suggestions.

## File Structure

### Core

- Modify: `src/MovieNavigator.Core/Media/MediaItem.cs`
  Add metadata needed by index and UI: `ThumbnailPath`, `MissingSince`, `Extension`.
- Create: `src/MovieNavigator.Core/Indexing/ScanRoot.cs`
  Stores configured scan root paths and last scan time.
- Create: `src/MovieNavigator.Core/Indexing/IndexedFileSnapshot.cs`
  Represents file path, size, last write time, and existence state.
- Create: `src/MovieNavigator.Core/Classification/ClassificationFacet.cs`
  Represents a clickable classification group.
- Create: `src/MovieNavigator.Core/Classification/ClassificationFacetBuilder.cs`
  Builds drive/status/type/decade/resolution/duration/TAG facets from indexed media.
- Create: `src/MovieNavigator.Core/Ai/AiSettings.cs`
  Stores provider, base URL, model, API key presence, and enabled state.
- Create: `src/MovieNavigator.Core/Ai/AiClassificationRequest.cs`
  Text-only request payload.
- Create: `src/MovieNavigator.Core/Ai/AiClassificationSuggestion.cs`
  Suggested title, summary, TAGs, year, confidence, and notes.
- Create: `src/MovieNavigator.Core/Abstractions/IAiClassificationClient.cs`
  Interface for text-only AI suggestions.

### Infrastructure

- Modify: `src/MovieNavigator.Core/Abstractions/IMediaRepository.cs`
  Add `GetAllAsync`, `GetByPathAsync`, and `MarkMissingAsync`.
- Modify: `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`
  Add migration-safe columns and tables for thumbnails, scan roots, app settings.
- Modify: `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`
  Implement startup load, path lookup, missing-state update, and preserve tags in reads.
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteScanRootRepository.cs`
  Save and load scan roots.
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteAppSettingsRepository.cs`
  Save AI settings without logging secrets.
- Create: `src/MovieNavigator.Infrastructure/Video/FfmpegThumbnailGenerator.cs`
  Generate local `.jpg` thumbnails into app cache.
- Create: `src/MovieNavigator.Infrastructure/Ai/OpenAiCompatibleClassificationClient.cs`
  Call OpenAI-compatible chat completions with text-only payload.

### App

- Modify: `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`
  Add thumbnail path, extension, size, missing state, and source classification fields.
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
  Split responsibilities if it keeps growing; add startup loading, incremental scan, classification facets, and view mode.
- Create: `src/MovieNavigator.App/ViewModels/ClassificationFacetViewModel.cs`
  One clickable left-panel classification item.
- Create: `src/MovieNavigator.App/ViewModels/ViewMode.cs`
  `ThumbnailGrid`, `CompactList`, `DetailList`.
- Create: `src/MovieNavigator.App/ViewModels/AiSettingsViewModel.cs`
  Settings fields and validation.
- Modify: `src/MovieNavigator.App/MainWindow.xaml`
  Replace static list with thumbnail grid/list templates and view-mode buttons.
- Modify: `src/MovieNavigator.App/MainWindow.xaml.cs`
  Wire lifecycle calls and keep UI event handlers thin.

### Tests

- Create: `tests/MovieNavigator.Tests/Indexing/PersistentIndexTests.cs`
- Create: `tests/MovieNavigator.Tests/Indexing/IncrementalScanTests.cs`
- Create: `tests/MovieNavigator.Tests/Classification/ClassificationFacetBuilderTests.cs`
- Create: `tests/MovieNavigator.Tests/Video/ThumbnailGeneratorTests.cs`
- Create: `tests/MovieNavigator.Tests/Ai/AiSettingsTests.cs`
- Create: `tests/MovieNavigator.Tests/Ai/AiClassificationClientTests.cs`
- Extend: `tests/MovieNavigator.Tests/App/MainWindowViewModelTests.cs`

---

## Task 1: Persistent Startup Index

**Files:**
- Modify: `src/MovieNavigator.Core/Abstractions/IMediaRepository.cs`
- Modify: `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml.cs`
- Test: `tests/MovieNavigator.Tests/Persistence/SqliteRepositoryTests.cs`
- Test: `tests/MovieNavigator.Tests/App/MainWindowViewModelTests.cs`

- [x] **Step 1: Write repository test for loading saved media**

Add a test named `Get_all_returns_saved_media_after_repository_recreated`. It should save two `MediaItem` rows, create a new `SqliteMediaRepository` over the same factory, call `GetAllAsync(MediaLibraryType.Normal, false, CancellationToken.None)`, and assert both rows are returned.

- [x] **Step 2: Verify test fails**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter Get_all_returns_saved_media_after_repository_recreated -v minimal
```

Expected: compile failure because `GetAllAsync` does not exist.

- [x] **Step 3: Add repository API**

Add to `IMediaRepository`:

```csharp
Task<IReadOnlyList<MediaItem>> GetAllAsync(MediaLibraryType libraryType, bool includeAdultWhenUnlocked, CancellationToken cancellationToken);
Task<MediaItem?> GetByPathAsync(string filePath, CancellationToken cancellationToken);
Task MarkMissingAsync(string filePath, DateTimeOffset missingSince, CancellationToken cancellationToken);
```

- [x] **Step 4: Implement SQLite loading**

Implement `GetAllAsync` in `SqliteMediaRepository` using:

```sql
SELECT id, library_type, status, file_path, file_name, drive_key, size_bytes, duration_seconds, width, height, title, original_title, year, summary, created_at, updated_at
FROM media_items
WHERE library_type = $libraryType
  AND ($includeAdult = 1 OR library_type <> 1)
ORDER BY updated_at DESC;
```

Also update `ReadMediaItemsAsync` to load `media_tags` for each returned item instead of always returning `Array.Empty<TagKey>()`.

- [x] **Step 5: Add startup load in ViewModel**

Add `LoadIndexAsync(CancellationToken)` to `MainWindowViewModel`. It calls repository `GetAllAsync`, converts rows to cards, builds drive and classification lists, and sets `ResultSummary` to `索引中已有 N 个视频`.

- [x] **Step 6: Call startup load from window**

In `MainWindow.xaml.cs`, after setting `DataContext`, call an async loaded handler:

```csharp
Loaded += async (_, _) => await _viewModel.LoadIndexAsync(CancellationToken.None);
```

- [x] **Step 7: Verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
dotnet build .\MovieNavigator.sln -v minimal
```

Expected: all tests pass, build has 0 errors.

- [x] **Step 8: Commit**

```powershell
git add src tests
git commit -m "feat: load persistent media index on startup"
```

---

## Task 2: Scan Roots and Incremental Scan

**Files:**
- Create: `src/MovieNavigator.Core/Indexing/ScanRoot.cs`
- Create: `src/MovieNavigator.Core/Indexing/IndexedFileSnapshot.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteScanRootRepository.cs`
- Modify: `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Test: `tests/MovieNavigator.Tests/Indexing/IncrementalScanTests.cs`

- [x] **Step 1: Add schema for scan roots**

Add table:

```sql
CREATE TABLE IF NOT EXISTS scan_roots (
    path TEXT PRIMARY KEY,
    library_type INTEGER NOT NULL,
    enabled INTEGER NOT NULL,
    last_scan_at TEXT NULL
);
```

- [x] **Step 2: Add failing test for saved scan root**

Test saves `D:\Movies` as enabled normal root, recreates repository, then loads it and asserts the path is present.

- [x] **Step 3: Implement scan root repository**

Implement:

```csharp
public sealed class SqliteScanRootRepository
{
    Task UpsertAsync(ScanRoot root, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanRoot>> GetEnabledAsync(MediaLibraryType libraryType, CancellationToken cancellationToken);
    Task UpdateLastScanAtAsync(string path, DateTimeOffset scannedAt, CancellationToken cancellationToken);
}
```

- [x] **Step 4: Add incremental scan behavior**

Change quick scan into:

```text
For each saved root:
  enumerate video files
  for each file:
    if path not indexed -> insert
    if size or last write time changed -> update
  for indexed files under root not seen in enumeration:
    mark missing/offline
```

If the current schema does not store last write time, add `last_write_time_utc TEXT NULL` to `media_items` with `ALTER TABLE` guarded by `PRAGMA table_info`.

- [x] **Step 5: UI behavior**

The top button text should become `添加目录并扫描`. Add a second button `增量扫描全部目录`. On startup, if scan roots exist, show `已加载 N 个视频，M 个扫描目录`.

- [x] **Step 6: Verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter IncrementalScanTests -v minimal
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

- [x] **Step 7: Commit**

```powershell
git add src tests
git commit -m "feat: add scan roots and incremental scanning"
```

---

## Task 3: Real Classification Facets

**Files:**
- Create: `src/MovieNavigator.Core/Classification/ClassificationFacet.cs`
- Create: `src/MovieNavigator.Core/Classification/ClassificationFacetBuilder.cs`
- Create: `src/MovieNavigator.App/ViewModels/ClassificationFacetViewModel.cs`
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml`
- Test: `tests/MovieNavigator.Tests/Classification/ClassificationFacetBuilderTests.cs`

- [x] **Step 1: Write tests for facets**

Create tests proving the builder returns:

```text
storage.drive.d
status.pending
type.mkv
decade.1970s
resolution.1080p
duration.long
country.soviet_union
```

from media rows containing those properties or TAGs.

- [x] **Step 2: Implement facet model**

```csharp
public sealed record ClassificationFacet(
    string Key,
    string DisplayName,
    string Group,
    int Count);
```

- [x] **Step 3: Implement facet builder**

Build facets from indexed media. Do not invent movie countries from filenames in this task. Only use explicit `MediaItem.Tags`, file extension, drive, duration, resolution, and status.

- [x] **Step 4: Bind facets in UI**

Replace separate fake drive/TAG lists with a single grouped classification list or two real sections:

```text
硬盘
状态
类型
年代
清晰度
时长
TAG
```

Clicking a facet filters the card list and updates `ResultSummary`.

- [x] **Step 5: Verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter ClassificationFacetBuilderTests -v minimal
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

- [x] **Step 6: Commit**

```powershell
git add src tests
git commit -m "feat: build real classification facets"
```

---

## Task 4: Local Video Analysis and Thumbnails

**Files:**
- Modify: `src/MovieNavigator.Core/Media/MediaItem.cs`
- Modify: `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`
- Modify: `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`
- Create: `src/MovieNavigator.Infrastructure/Video/FfmpegThumbnailGenerator.cs`
- Modify: `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`
- Test: `tests/MovieNavigator.Tests/Video/ThumbnailGeneratorTests.cs`
- Test: `tests/MovieNavigator.Tests/Persistence/SqliteRepositoryTests.cs`

- [x] **Step 1: Add thumbnail and file metadata columns**

Add migration-safe columns:

```sql
thumbnail_path TEXT NULL
extension TEXT NULL
last_write_time_utc TEXT NULL
missing_since TEXT NULL
```

- [x] **Step 2: Add `ThumbnailPath` to media/card models**

Extend `MediaItem` and `MediaCardViewModel` with nullable `ThumbnailPath`.

- [x] **Step 3: Generate thumbnails with ffmpeg**

Create `FfmpegThumbnailGenerator` that runs:

```powershell
ffmpeg -y -ss 00:00:10 -i input.mp4 -frames:v 1 -vf scale=360:-1 output.jpg
```

Use app cache directory:

```text
%LOCALAPPDATA%\MovieNavigator\thumbnails
```

Use a stable file name from SHA256 of the video path.

- [x] **Step 4: Use ffprobe and ffmpeg during scan**

During scan:

```text
if duration/resolution missing:
  call ffprobe
if thumbnail missing:
  call ffmpeg thumbnail generator
save updated media item
```

If ffmpeg/ffprobe is not installed or fails, keep the item and show `待分析`, not a crash.

- [x] **Step 5: Verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter ThumbnailGeneratorTests -v minimal
dotnet build .\MovieNavigator.sln -v minimal
```

- [x] **Step 6: Commit**

```powershell
git add src tests
git commit -m "feat: generate local video thumbnails"
```

---

## Task 5: Usable Thumbnail UI and View Modes

**Files:**
- Create: `src/MovieNavigator.App/ViewModels/ViewMode.cs`
- Modify: `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml`
- Test: `tests/MovieNavigator.Tests/App/MainWindowViewModelTests.cs`

- [x] **Step 1: Add view mode state**

Create:

```csharp
public enum ViewMode
{
    ThumbnailGrid,
    CompactList,
    DetailList
}
```

Add `SelectedViewMode` to `MainWindowViewModel`.

- [x] **Step 2: Write test**

Test switching `SelectedViewMode` updates property changed and preserves the current filtered media list.

- [x] **Step 3: Replace list UI**

In XAML, add three buttons:

```text
缩略图
紧凑列表
详情列表
```

Default to thumbnail grid. Use `WrapPanel` or `UniformGrid` inside an `ItemsControl` for thumbnail cards.

- [x] **Step 4: Thumbnail fallback**

If `ThumbnailPath` is null or missing, show a styled placeholder containing extension and title. Do not show a broken image icon.

- [x] **Step 5: Verify manually**

Manual checks:

```text
Start app
Scan folder
Cards show thumbnail or placeholder
Switch view modes
Select card
Open default player
Open folder
Search and classification filters still work
```

- [x] **Step 6: Automated verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
dotnet build .\MovieNavigator.sln -v minimal
```

- [x] **Step 7: Commit**

```powershell
git add src tests
git commit -m "feat: add thumbnail grid and view modes"
```

---

## Task 6: Text-Only AI Classification Settings

**Files:**
- Create: `src/MovieNavigator.Core/Ai/AiSettings.cs`
- Create: `src/MovieNavigator.Core/Ai/AiClassificationRequest.cs`
- Create: `src/MovieNavigator.Core/Ai/AiClassificationSuggestion.cs`
- Create: `src/MovieNavigator.Core/Abstractions/IAiClassificationClient.cs`
- Create: `src/MovieNavigator.Infrastructure/Persistence/SqliteAppSettingsRepository.cs`
- Create: `src/MovieNavigator.Infrastructure/Ai/OpenAiCompatibleClassificationClient.cs`
- Create: `src/MovieNavigator.App/ViewModels/AiSettingsViewModel.cs`
- Modify: `src/MovieNavigator.App/MainWindow.xaml`
- Test: `tests/MovieNavigator.Tests/Ai/AiSettingsTests.cs`
- Test: `tests/MovieNavigator.Tests/Ai/AiClassificationClientTests.cs`

- [ ] **Step 1: Store AI settings**

Add settings table:

```sql
CREATE TABLE IF NOT EXISTS app_settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
```

Store provider/base URL/model/enabled. Store API key only if user explicitly saves it; never print it in logs or status.

- [ ] **Step 2: Define text-only request**

Fields:

```text
file name
folder path
manual title
manual identifier / 番号
manual URL
existing TAGs
duration
resolution
library type
```

Do not include screenshots, thumbnails, audio, or video bytes.

- [ ] **Step 3: Add settings UI**

Settings page must show:

```text
Provider
Base URL
Model
API Key
Enable AI
Test connection
```

Also show this warning:

```text
当前版本只发送文本线索，不发送截图、视频、音频。成人库建议使用单独配置。
```

- [ ] **Step 4: Add AI suggestion button in pending workbench**

Button text: `用AI根据文本线索建议TAG`.

Before calling AI, show a confirmation dialog listing exactly which text fields will be sent.

- [ ] **Step 5: Parse AI response into suggestions**

Expected JSON:

```json
{
  "title": "string",
  "year": 1970,
  "summary": "string",
  "tags": ["country.soviet_union", "genre.war"],
  "confidence": 0.72,
  "notes": "string"
}
```

If model returns invalid JSON, show error and keep the media item unchanged.

- [ ] **Step 6: Verify**

Run:

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter Ai -v minimal
dotnet build .\MovieNavigator.sln -v minimal
```

- [ ] **Step 7: Commit**

```powershell
git add src tests
git commit -m "feat: add text-only AI classification settings"
```

---

## Acceptance Checklist

- [ ] Reopening the app shows old indexed videos without rescanning.
- [ ] Incremental scan finds new files and marks missing files.
- [ ] Left classification is built from real indexed data, not fake labels.
- [ ] At least drive, status, extension/type, duration bucket, resolution bucket, and TAG filters work.
- [ ] Scanned videos show generated thumbnails or clear placeholders.
- [ ] User can switch thumbnail/list/detail layouts.
- [ ] AI settings exist and are disabled by default.
- [ ] AI sends text-only data after explicit confirmation.
- [ ] Adult screenshots/video/audio are not sent to AI in this plan.
- [ ] Tests and build pass after every task.

## Explicit Non-Goals For This Plan

- No automatic online scraping from TMDb/JavDB/etc.
- No adult vault unlock UI.
- No physical move/copy/delete operations.
- No cloud image/video analysis.
- No attempt to infer sensitive adult visual content from thumbnails.
- No final installer or single EXE packaging.
