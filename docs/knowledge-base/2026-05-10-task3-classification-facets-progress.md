# Movie Navigator Task 3 Classification Facets Progress

日期：2026-05-10

## 本次完成

- 新增 `ClassificationFacet` 核心模型。
- 新增 `ClassificationFacetBuilder`，从已索引媒体生成真实分类。
- 分类来源包括硬盘、状态、文件类型、年代、清晰度、时长和 TAG。
- 新增 `ClassificationFacetViewModel`。
- `MediaCardViewModel` 增加 `ClassificationKeys`，用于点击分类后过滤。
- `MainWindowViewModel` 增加 `ClassificationFacets` 和 `SelectedClassificationFacet`。
- 左侧 UI 从静态硬盘/TAG 区域改为 `真实分类` 列表。
- 点击真实分类项会过滤中间媒体列表。

## 用户可见变化

- 左侧不再只是固定写死的假分类。
- 扫描或加载索引后，左侧会根据真实媒体生成分类项，并显示数量。
- 可以按 `硬盘`、`状态`、`类型`、`年代`、`清晰度`、`时长`、`TAG` 过滤。

## 仍未完成

- 分类还没有分组折叠 UI。
- 分类显示名还比较朴素。
- 国家、导演、演员等需要影片识别/人工补充/AI 建议后才能准确生成。
- 缩略图和排版切换还没有做。

## 验证

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：38 个测试通过，0 失败。

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

