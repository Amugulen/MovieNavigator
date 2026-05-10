# Movie Navigator Task 5 Thumbnail UI Progress

日期：2026-05-10

## 本次完成

- 新增 `ViewMode`：`ThumbnailGrid`、`CompactList`、`DetailList`。
- `MainWindowViewModel` 增加 `SelectedViewMode` 和三个视图状态属性。
- 顶部媒体区域新增三个排版按钮：
  - `缩略图`
  - `紧凑列表`
  - `详情列表`
- 中间媒体区改为三种可切换视图。
- 缩略图视图会显示本地缩略图；没有缩略图时显示扩展名占位。
- 切换排版不会破坏当前搜索/分类筛选结果。

## 用户可见变化

- 默认不再只是纯文字列表，而是缩略图卡片视图。
- 用户可以在三种排版之间切换。
- 扫描时如果 ffmpeg 生成了缩略图，卡片会显示图片；否则显示扩展名占位。

## 仍未完成

- 缩略图卡片设计还比较基础。
- 分类列表还没有分组折叠。
- 还没有 AI 配置和 AI 文本分类。
- 还没有成人库完整解锁/隔离 UI。

## 验证

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：42 个测试通过，0 失败。

