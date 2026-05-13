# Task 6 AI Text Settings Progress

日期：2026-05-13

## 已完成

- 新增 AI 设置模型：`AiSettings`。
- 新增文本-only AI 请求和建议模型：`AiClassificationRequest`、`AiClassificationSuggestion`。
- 新增接口：`IAiSettingsRepository`、`IAiClassificationClient`。
- SQLite 初始化增加 `app_settings` 表。
- 新增 `SqliteAppSettingsRepository`：
  - 保存 Provider、Base URL、Model、Enable AI。
  - API Key 只有用户显式填写保存时才写入。
  - 保存普通设置时不会覆盖已有 API Key。
  - `AiSettings.ToString()` 不输出密钥内容。
- 新增 OpenAI-compatible chat completions 客户端：
  - 请求地址为 `{BaseUrl}/chat/completions`。
  - 只发送文本 payload：文件名、文件夹、手动标题、手动标识/番号、手动网址、已有 TAG、时长、分辨率、库类型。
  - 不发送截图、缩略图、视频、音频或媒体字节。
  - 解析 JSON 建议：title、year、summary、tags、confidence、notes。
  - 无效 JSON 会抛出错误，调用方不会修改媒体数据。
- 新增 `AiSettingsViewModel`：
  - AI 默认关闭。
  - 支持 Provider、Base URL、Model、API Key、Enable AI。
  - `Test connection` 会通过 AI 客户端发送文本-only 探测请求，不是空按钮。
- 主窗口右侧加入最小 AI 设置面板。
- 待确认工作台加入 `用AI根据文本线索建议TAG` 按钮。
- 调用 AI 前会弹窗列出将发送的文本字段。
- AI 建议结果当前只弹窗展示，不自动写入数据库。

## 验证

已运行：

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj --filter Ai -v minimal
```

结果：23 个测试通过，0 失败。

已运行全量测试：

```powershell
dotnet test .\tests\MovieNavigator.Tests\MovieNavigator.Tests.csproj -v minimal
```

结果：55 个测试通过，0 失败。

已运行构建：

```powershell
dotnet build .\MovieNavigator.sln -v minimal
```

结果：0 警告，0 错误。

## 尚未完成 / 风险

- GUI 人工点测未完成，不应标记为已人工验证。
- AI 设置 UI 是最小可用版本，视觉仍需要后续打磨。
- API Key 当前按普通 SQLite 设置保存，适合本地个人 MVP；后续如果要提高安全性，应接入 Windows Credential Manager 或 DPAPI。
- 当前 AI 建议不会写入媒体 TAG；仍需后续做用户确认后的写入流程。
- 成人库仍未产品化，不能声称完成成人内容隔离 UI。
