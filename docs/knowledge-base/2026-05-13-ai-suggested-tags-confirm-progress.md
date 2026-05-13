# AI Suggested Tags Confirm Progress

日期：2026-05-13

## 已完成

- `IMediaRepository` 新增 `AddTagsAsync`，用于给已有媒体合并 TAG。
- `SqliteMediaRepository.AddTagsAsync` 会保留原 TAG，合并新 TAG，并通过现有 `UpsertAsync` 刷新搜索索引。
- `MainWindowViewModel` 新增 AI 建议确认流程：
  - 获取选中媒体的 AI TAG 建议。
  - 用户确认后调用仓储写入建议 TAG。
  - 写入后重新加载索引并刷新媒体卡片、分类 facet。
- 主窗口 AI 建议按钮现在是两步确认：
  1. 调用前确认将发送的文本字段。
  2. AI 返回后展示建议 TAG，用户确认后才写入。
- 写入范围只限 TAG，不会自动移动文件、改标题、改简介或改其他媒体资料。

## 验证

已运行：

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：57 个测试通过，0 失败。

已运行：

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

## 尚未完成 / 风险

- GUI 人工点测未完成。
- AI 建议当前是整组 TAG 确认写入，还没有逐个勾选/编辑建议 TAG 的 UI。
- 真实 API 调用未人工验证。
