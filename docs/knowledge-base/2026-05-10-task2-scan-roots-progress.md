# Movie Navigator Task 2 Scan Roots Progress

日期：2026-05-10

## 本次完成

- 新增 `ScanRoot` 核心模型。
- 新增 `IScanRootRepository` 抽象。
- SQLite 初始化新增 `scan_roots` 表。
- 新增 `SqliteScanRootRepository`，支持保存扫描目录、读取启用目录、更新最后扫描时间。
- `MainWindowViewModel.QuickScanFolderAsync` 会在扫描时保存扫描目录。
- 新增 `MainWindowViewModel.IncrementalScanAllRootsAsync`，可以复用已保存扫描目录。
- 增量扫描会重新扫描可访问目录。
- 增量扫描会把已索引但当前目录里找不到的文件标记为 `Offline`。
- 顶部新增 `增量扫描全部目录` 按钮。

## 用户可见变化

- 第一次选择目录扫描后，该目录会保存到 SQLite。
- 下次打开软件后，用户可以直接点 `增量扫描全部目录`，不需要再次选择同一个目录。
- 如果已索引文件被删除或硬盘目录不可访问，增量扫描会把它标记为离线。

## 仍未完成

- 还没有扫描目录管理界面。
- 还没有展示每个扫描目录的最后扫描时间。
- 还没有文件大小/修改时间级别的精确变更判断，目前增量扫描会重新 upsert 当前目录下符合规则的视频。
- 还没有真实分类 Facet。
- 还没有缩略图。

## 验证

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：36 个测试通过，0 失败。

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

