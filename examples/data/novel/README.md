# 小说文本数据目录

将小说文本文件（`.txt`）放入此目录，`AgentEffectReport.cs` 脚本会自动读取并分析。

## 使用方式

1. 将 `.txt` 文件放入此目录
2. 运行脚本：`dotnet run examples/AgentEffectReport.cs`

## 建议

- 文件名使用有意义的命名（如 `鲁迅-故乡.txt`）
- 编码使用 UTF-8
- 大文件（> 5000 字）会自动按段落拆分为多个分块，无需手动切割
