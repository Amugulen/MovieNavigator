# Movie Navigator Agent Handoff

日期：2026-05-13

## 当前代码位置

- 仓库：`https://github.com/Amugulen/MovieNavigator.git`
- 本地 worktree：`H:\CodexSoftware\自动搜索影视信息的软件\.worktrees\mvp-foundation`
- 当前分支：`feature/mvp-foundation`
- 当前状态：工作区干净，已同步 `origin/feature/mvp-foundation`
- 最新提交：`e88510a docs: correct manual verification status`

## 用户核心诉求

用户要的是 Windows 个人自用本地影视索引软件，不是播放器。

必须重视：

- 打开软件后能看到已有索引，不要每次重新扫描。
- 扫描目录需要保存，后续可以增量扫描。
- 左侧分类必须来自真实索引数据，不能是假按钮。
- 视频要有缩略图和可切换排版。
- 影片分析、TAG、AI 文本分类是核心方向。
- 成人内容必须默认隔离，未授权时小朋友不能看到成人内容。
- AI 默认不发送截图/视频/音频，优先做文本线索分类，避免审核拒绝。
- 用户不懂技术，需要双击 BAT 可运行。

## 当前已完成能力

### 启动与索引

- `run-movie-navigator.bat` 可双击启动。
- App 使用 SQLite：`%LOCALAPPDATA%\MovieNavigator\normal.db`。
- 启动时会从 SQLite 加载已有普通库索引。
- 不再必须每次打开后重新扫描。

关键文件：

- `src/MovieNavigator.App/App.xaml.cs`
- `src/MovieNavigator.App/MainWindow.xaml.cs`
- `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`

### 扫描目录与增量扫描

- 首次选择目录扫描后，会保存扫描目录到 `scan_roots`。
- 顶部有 `增量扫描全部目录` 按钮。
- 增量扫描会复用已保存扫描目录。
- 已索引但当前扫描找不到的文件会标记为 `Offline`。

关键文件：

- `src/MovieNavigator.Core/Indexing/ScanRoot.cs`
- `src/MovieNavigator.Core/Abstractions/IScanRootRepository.cs`
- `src/MovieNavigator.Infrastructure/Persistence/SqliteScanRootRepository.cs`
- `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`

### 真实分类 Facet

- 左侧现在是 `真实分类`，不再只是写死硬盘/TAG。
- 分类从已索引媒体生成：
  - 硬盘
  - 状态
  - 文件类型
  - 年代
  - 清晰度
  - 时长
  - TAG
- 点击分类会过滤中间列表。

关键文件：

- `src/MovieNavigator.Core/Classification/ClassificationFacet.cs`
- `src/MovieNavigator.Core/Classification/ClassificationFacetBuilder.cs`
- `src/MovieNavigator.App/ViewModels/ClassificationFacetViewModel.cs`

### 本地分析和缩略图基础

- 扫描时会尝试调用 `ffprobe` 获取时长、宽度、高度。
- 扫描时会尝试调用 `ffmpeg` 生成 jpg 缩略图。
- 缩略图目录：`%LOCALAPPDATA%\MovieNavigator\thumbnails`
- 如果 `ffmpeg` 或 `ffprobe` 不存在，扫描不会崩溃；对应字段保持空或待分析。
- SQLite 已支持：
  - `thumbnail_path`
  - `extension`
  - `last_write_time_utc`
  - `missing_since`

关键文件：

- `src/MovieNavigator.Core/Abstractions/IThumbnailGenerator.cs`
- `src/MovieNavigator.Infrastructure/Video/FfmpegThumbnailGenerator.cs`
- `src/MovieNavigator.Infrastructure/Video/FfprobeVideoInspector.cs`
- `src/MovieNavigator.Core/Media/MediaItem.cs`

### UI 视图

- 中间媒体区支持三种排版：
  - `缩略图`
  - `紧凑列表`
  - `详情列表`
- 默认缩略图卡片视图。
- 没有缩略图时显示扩展名占位。
- 搜索、分类筛选和视图切换可以共存。

关键文件：

- `src/MovieNavigator.App/MainWindow.xaml`
- `src/MovieNavigator.App/MainWindow.xaml.cs`
- `src/MovieNavigator.App/ViewModels/ViewMode.cs`
- `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`

## 最近提交

```text
e88510a docs: correct manual verification status
e1faa40 feat: add thumbnail grid and view modes
5dec1ef feat: generate local video thumbnails
5d95e07 feat: build real classification facets
28d51da feat: add scan roots and incremental scanning
36f6e93 feat: load persistent media index on startup
df33a40 docs: add core recovery plan and progress log
8980936 feat: wire media actions and filtering
7683436 feat: add quick folder scan from UI
151c1ce chore: add double-click launcher
```

## 当前验证状态

最近一次自动验证：

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：42 个测试通过，0 失败。

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

注意：GUI 人工点测没有完成。PLAN 中 Task 5 的 `Verify manually` 仍然是未勾选，这是故意保留的真实状态。

## 已知问题和风险

### 1. 中文显示/源码查看可能出现乱码

部分 PowerShell 输出显示乱码，但项目可编译。不要因为终端乱码就随意批量改中文字符串。后续最好统一走 JSON 本地化资源，减少硬编码中文。

### 2. ffmpeg/ffprobe 依赖系统 PATH

当前写死使用：

```text
ffmpeg
ffprobe
```

如果用户机器未安装或 PATH 未配置，扫描不会崩溃，但不会生成缩略图/时长/分辨率。后续需要设置页配置路径。

### 3. 缩略图 UI 还比较粗糙

已经有缩略图卡片，但视觉仍然基础。用户之前明确嫌界面丑，后续 UI 需要继续改，不要把当前 UI 当最终设计。

### 4. 分类还不够细

当前分类只基于已有字段和 TAG。导演、演员、国家、影片评分等必须等元数据识别、人工补充或 AI 建议后才能准确生成。

### 5. 成人库还没产品化

已有成人库基础模型，但没有完整 UI 隔离流程。不要声称成人库已完成。

### 6. AI 还没开始实现

Task 6 还没有做。当前没有 API Key 配置、没有 AI 调用、没有 AI 建议写入。

## 下一步推荐

严格按 PLAN 继续：

`docs/superpowers/plans/2026-05-10-movie-navigator-core-recovery-plan.md`

下一项是：

## Task 6: Text-Only AI Classification Settings

目标：

- 增加 AI 设置存储。
- 支持 Provider、Base URL、Model、API Key、Enable AI。
- 默认关闭 AI。
- 只发送文本线索，不发送截图、视频、音频。
- AI 输出只作为建议，用户确认后才写入。

不要做：

- 不要先做云端图片/视频识别。
- 不要把成人截图发给 AI。
- 不要在没有确认前自动改数据库。
- 不要一次性做成人库、文件移动、在线刮削、AI、UI 大改混在一起。

## Task 6 建议执行顺序

1. 新建 `AiSettings`、`AiClassificationRequest`、`AiClassificationSuggestion`。
2. 新建 `IAiClassificationClient`。
3. SQLite 增加 `app_settings` 表。
4. 新建 `SqliteAppSettingsRepository`。
5. 新建 `AiSettingsViewModel`。
6. 设置页先做最小 UI：Base URL、Model、API Key、Enable AI、Test connection。
7. 新建 OpenAI-compatible 客户端，但测试中使用假 HTTP handler，不要依赖真实网络。
8. 待确认工作台加 `用AI根据文本线索建议TAG`，调用前弹窗确认将发送的文本字段。

## 常用命令

进入工作目录：

```powershell
cd H:\CodexSoftware\自动搜索影视信息的软件\.worktrees\mvp-foundation
```

运行测试：

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

构建：

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

启动：

```powershell
.\run-movie-navigator.bat
```

检查状态：

```powershell
git -c safe.directory=H:/CodexSoftware/自动搜索影视信息/software/.worktrees/mvp-foundation status --short --branch
```

## 给下个 Agent 的提醒

- 用户已经多次指出“不要乱搞，先搞清楚要做啥”。继续前先读 PLAN 和本日志。
- 用户不接受假按钮、假分类、只弹提示的假完成。
- 每个功能都要有真实可见行为和测试。
- 完成后必须更新知识库日志。
- 不要把未人工验证的 GUI 行为标记为已手动验证。
- 提交后推送到 `feature/mvp-foundation`。

