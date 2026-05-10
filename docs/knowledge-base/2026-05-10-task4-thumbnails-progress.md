# Movie Navigator Task 4 Thumbnails Progress

日期：2026-05-10

## 本次完成

- `MediaItem` 增加 `ThumbnailPath`、`Extension`、`LastWriteTimeUtc`、`MissingSince`。
- `MediaCardViewModel` 增加 `ThumbnailPath`、`Extension`、`IsMissing`。
- SQLite `media_items` 增加迁移安全字段：
  - `thumbnail_path`
  - `extension`
  - `last_write_time_utc`
  - `missing_since`
- `SqliteMediaRepository` 可以保存和读取这些新字段。
- `MarkMissingAsync` 现在会写入 `missing_since`。
- 新增 `IThumbnailGenerator` 抽象。
- 新增 `FfmpegThumbnailGenerator`，使用外部 `ffmpeg` 生成 jpg 缩略图。
- 扫描时会尝试生成缩略图并写入索引。
- 扫描时会在可用时调用 `ffprobe` 分析时长、宽度和高度。
- 如果 `ffmpeg` 不存在或生成失败，扫描不会崩溃，缩略图字段为空。
- 如果 `ffprobe` 不存在或分析失败，扫描不会崩溃，时长和分辨率继续显示为待分析。

## 用户可见变化

- 具备缩略图生成和持久化基础。
- 如果系统 PATH 中有 `ffmpeg`，后续扫描会在 `%LOCALAPPDATA%\MovieNavigator\thumbnails` 生成 jpg。
- 当前 UI 仍然没有把缩略图显示成网格卡片；这是 Task 5。

## 仍未完成

- 还没有缩略图网格 UI。
- 还没有排版切换。
- 还没有给用户配置 ffmpeg 路径的设置项。
- 还没有给用户配置 ffprobe 路径的设置项。

## 验证

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：41 个测试通过，0 失败。

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。
