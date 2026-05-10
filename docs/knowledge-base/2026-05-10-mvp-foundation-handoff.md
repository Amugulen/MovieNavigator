# Movie Navigator MVP Foundation Handoff

日期：2026-05-10

## 当前状态

Movie Navigator 的 MVP foundation 已在独立分支完成：

- 分支：`feature/mvp-foundation`
- 远程：`origin/feature/mvp-foundation`
- PR 地址：`https://github.com/Amugulen/MovieNavigator/pull/new/feature/mvp-foundation`
- 本地 worktree：`H:\CodexSoftware\自动搜索影视信息的软件\.worktrees\mvp-foundation`
- 主工作区：`H:\CodexSoftware\自动搜索影视信息的软件`

主分支 `main` 只保留设计文档和实施计划。实际代码在 `feature/mvp-foundation`。

## 已完成内容

### 项目骨架

- 创建 `.NET 8` solution：`MovieNavigator.sln`
- 创建核心项目：
  - `src/MovieNavigator.Core`
  - `src/MovieNavigator.Infrastructure`
  - `src/MovieNavigator.App`
  - `tests/MovieNavigator.Tests`
- WPF 桌面应用目标框架：`net8.0-windows`
- 测试项目目标框架：`net8.0-windows`

### Core 领域模型

关键文件：

- `src/MovieNavigator.Core/Media/MediaItem.cs`
- `src/MovieNavigator.Core/Media/MediaLibraryType.cs`
- `src/MovieNavigator.Core/Media/MediaStatus.cs`
- `src/MovieNavigator.Core/Tags/TagKey.cs`
- `src/MovieNavigator.Core/Tags/TagDefinition.cs`
- `src/MovieNavigator.Core/Tags/TagAssignment.cs`

已实现：

- 普通库 / 成人库枚举。
- 媒体状态：待确认、已确认、忽略、离线。
- 层级 TAG key 校验，例如 `country.soviet_union`。
- TAG 支持中英文显示名和别名。

### 扫描与收录规则

关键文件：

- `src/MovieNavigator.Core/Scanning/ScanRules.cs`
- `src/MovieNavigator.Core/Scanning/MediaScanner.cs`
- `src/MovieNavigator.Core/Abstractions/IFileSystem.cs`
- `src/MovieNavigator.Core/Abstractions/IVideoInspector.cs`

已实现：

- 默认只收录大于 20 分钟且大于 100 MB 的视频。
- 支持常见视频扩展名。
- 排除 cache、temp、tmp、node_modules、steamapps、game、games 等目录。
- 扫描器把合格视频转换为 `MediaItem`。

### SQLite 仓储与搜索

关键文件：

- `src/MovieNavigator.Infrastructure/Persistence/SqliteConnectionFactory.cs`
- `src/MovieNavigator.Infrastructure/Persistence/DatabaseInitializer.cs`
- `src/MovieNavigator.Infrastructure/Persistence/SqliteMediaRepository.cs`
- `src/MovieNavigator.Infrastructure/Persistence/SqliteTagRepository.cs`

已实现：

- SQLite schema 初始化。
- 媒体条目 upsert。
- TAG upsert。
- SQLite FTS5 搜索索引。
- 搜索内容包含文件名、路径、标题、年份、TAG key、TAG 中文名、TAG 英文名、别名。
- 增加中文搜索回退：FTS 不命中时使用 `LIKE` 回退，避免中文分词问题。
- 普通库搜索不会返回成人库条目。

### 成人库安全基础

关键文件：

- `src/MovieNavigator.Core/Security/PasswordHasher.cs`
- `src/MovieNavigator.Core/Security/AdultVaultState.cs`

已实现：

- PBKDF2-SHA256 密码哈希。
- 密码不保存明文。
- 成人库锁定状态阻止成人查询和成人 TAG 显示。
- 成人库解锁状态允许查询。

### 文件操作安全模型

关键文件：

- `src/MovieNavigator.Core/FileOperations/FileOperationPlan.cs`
- `src/MovieNavigator.Core/FileOperations/FileOperationType.cs`
- `src/MovieNavigator.Core/FileOperations/FileOperationResult.cs`
- `src/MovieNavigator.Core/FileOperations/FileOperationLogEntry.cs`

已实现：

- 复制、移动、重命名、删除到回收站的操作类型。
- 离线源文件禁止执行移动。
- 成人库和普通库之间移动需要额外确认。
- 操作日志模型已定义。

### Windows 适配器

关键文件：

- `src/MovieNavigator.Infrastructure/FileSystem/WindowsFileSystem.cs`
- `src/MovieNavigator.Infrastructure/FileSystem/DefaultProcessLauncher.cs`
- `src/MovieNavigator.Infrastructure/Video/FfprobeVideoInspector.cs`

已实现：

- Windows 文件枚举。
- 默认应用打开文件。
- Explorer 打开目录。
- ffprobe JSON 输出解析。

### 本地化基础

关键文件：

- `src/MovieNavigator.App/Localization/IAppLocalizer.cs`
- `src/MovieNavigator.App/Localization/JsonAppLocalizer.cs`
- `src/MovieNavigator.App/Localization/LocalizedStrings.cs`
- `src/MovieNavigator.App/Resources/Strings.zh-CN.json`
- `src/MovieNavigator.App/Resources/Strings.en-US.json`

已实现：

- `zh-CN` / `en-US` JSON 资源文件。
- 当前语言优先。
- 英文回退。
- 找不到资源 key 时回退 key 本身。

### WPF 界面壳

关键文件：

- `src/MovieNavigator.App/MainWindow.xaml`
- `src/MovieNavigator.App/MainWindow.xaml.cs`
- `src/MovieNavigator.App/ViewModels/MainWindowViewModel.cs`
- `src/MovieNavigator.App/ViewModels/MediaCardViewModel.cs`
- `src/MovieNavigator.App/ViewModels/TagNodeViewModel.cs`
- `src/MovieNavigator.App/ViewModels/PendingItemViewModel.cs`

已实现：

- 左侧导航。
- 硬盘列表。
- TAG 索引列表。
- 搜索框。
- 媒体卡片列表。
- 影片详情面板。
- 待确认工作台区域。
- 成人库入口显示为锁定状态。

当前界面数据仍是静态 mock 数据，真实扫描和数据库绑定尚未接入 UI。

## 测试覆盖

测试目录：`tests/MovieNavigator.Tests`

当前测试覆盖：

- `Tags/TagKeyTests.cs`
- `Scanning/ScanRulesTests.cs`
- `Scanning/MediaScannerTests.cs`
- `Persistence/SqliteRepositoryTests.cs`
- `Security/PasswordHasherTests.cs`
- `Security/AdultVaultVisibilityTests.cs`
- `FileOperations/FileOperationPlannerTests.cs`
- `Localization/JsonAppLocalizerTests.cs`
- `Search/SearchTests.cs`

最终验证结果：

```text
dotnet test MovieNavigator.sln
结果：29 个测试通过，0 失败

dotnet build MovieNavigator.sln
结果：0 警告，0 错误
```

## 环境注意事项

当前机器普通沙箱里直接运行 `dotnet build` 有时会因为 .NET CLI 首次运行目录权限或 NuGet 网络限制失败。

已验证可行方式：

- 在沙箱外运行 `dotnet build MovieNavigator.sln`
- 在沙箱外运行 `dotnet test MovieNavigator.sln`

如果必须在受限环境运行，可尝试设置：

```powershell
$env:DOTNET_CLI_HOME=(Resolve-Path '.dotnet-home').Path
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
```

但最终以沙箱外构建和测试结果为准。

## 重要设计决策

- 项目只考虑 Windows。
- 第一版不做内置播放器，使用系统默认播放器打开。
- AI 不直接接触成人截图，第一版只预留文本分类助手方向。
- 成人库在普通模式下必须不可见，不只是 UI 隐藏。
- TAG key 使用稳定英文，显示名支持多语言。
- 中文搜索不能只依赖 SQLite FTS，已加入 `LIKE` 回退。
- 文件移动/整理必须先生成计划并由用户确认。

## 下一步建议

推荐下一阶段做“真实扫描到 UI 的闭环”：

1. 在设置或主界面添加“添加扫描目录”入口。
2. 调用 `WindowsFileSystem` + `FfprobeVideoInspector` + `MediaScanner`。
3. 将扫描结果写入 `SqliteMediaRepository`。
4. 主界面从仓储加载真实媒体列表。
5. 支持按硬盘浏览真实数据。
6. 支持搜索框调用仓储搜索。
7. 增加成人库密码设置和解锁 UI。

不要下一步就做 AI 或复杂刮削。先把“本地扫描 -> 入库 -> 搜索 -> 展示 -> 默认播放器打开”的闭环跑通。

## 常用命令

进入 feature worktree：

```powershell
cd H:\CodexSoftware\自动搜索影视信息的软件\.worktrees\mvp-foundation
```

运行测试：

```powershell
dotnet test MovieNavigator.sln
```

构建：

```powershell
dotnet build MovieNavigator.sln
```

启动桌面应用：

```powershell
dotnet run --project src/MovieNavigator.App/MovieNavigator.App.csproj
```

查看 Git 状态：

```powershell
git status --short --branch
```

## 当前 Git 提交摘要

最近关键提交：

- `323985e` docs: add project README
- `e471b9e` feat: wire app database and search visibility
- `289507a` feat: add MVP desktop shell
- `d9fb1f1` feat: add localization foundation
- `b2b0b8f` feat: add windows integration adapters
- `685b433` feat: add safe file operation plans
- `6726f56` feat: add adult vault security rules
- `aae9193` feat: add sqlite media repository
- `179c9d3` feat: add media scanning rules
- `daf3e60` feat: add media and tag domain models
- `460202f` chore: scaffold Movie Navigator solution

## 2026-05-10 按钮无效问题修复记录

用户反馈：快速扫描能扫出结果，但界面上所有按钮/可点击区域看起来都不生效。

根因：
- 已提交版本的右侧详情按钮只有文字和样式，没有绑定 Click 处理逻辑。
- 媒体列表没有 SelectedItem 绑定，右侧详情面板一直显示静态示例数据。
- 搜索框只保存输入文本，没有触发列表过滤。
- 左侧硬盘列表、TAG 列表只显示数据，没有 SelectedItem 绑定和过滤行为。
- 左侧导航只会高亮，不会改变状态，也不会告诉用户下一步该怎么操作。

已修复：
- 媒体列表绑定 `SelectedMedia`，右侧详情面板显示真实选中视频的路径、时长、分辨率、TAG。
- “默认播放器打开”会用系统默认播放器打开选中文件。
- “打开所在目录”会用 Explorer 定位选中文件。
- “移动/整理”“重新识别”“添加TAG”在未实现真实功能前会弹出明确说明，不再静默无效。
- 搜索框按标题、路径、硬盘、TAG 即时过滤扫描结果。
- 硬盘列表绑定 `SelectedDrive`，点击硬盘后过滤该硬盘的视频。
- TAG 列表绑定 `SelectedTag`，点击 TAG 后过滤命中该 TAG 的视频。
- 点击“首页/普通库”会清除筛选并恢复全部扫描结果。
- 点击“硬盘浏览/TAG索引/待确认/设置/成人库锁定”会更新状态提示。

验证：
```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```
结果：32 个测试通过，0 失败。

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```
结果：0 警告，0 错误。

注意：
- 本轮没有把“移动/整理”“重新识别”“添加TAG”做成真实业务功能，只是保证按钮有明确反馈。
- 下一步如果继续做功能，应优先实现 TAG 编辑器、文件移动/复制计划确认、ffprobe 重新识别、启动时从 SQLite 重新加载历史扫描结果。
