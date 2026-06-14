# Novap.JsonRepair

[English](README.md) | **简体中文**

从 LLM（大语言模型）原始响应文本中提取并修复不规范 JSON 的轻量级 .NET 库。

## 简介

当 LLM 输出包含 JSON 时，响应往往带有 markdown 代码块包裹、单引号字符串、无引号键名、尾部逗号、缺少闭合括号、注释等非标准格式，标准的 `System.Text.Json` 解析器无法直接解析。**Novap.JsonRepair** 通过自定义的递归下降解析器，将这些"破损" JSON 修复为合法的 JSON 字符串。

## 修复能力

| 问题类型 | 示例 |
|---------|------|
| Markdown 代码块包裹 | `` ```json {...} ``` `` |
| 单引号字符串 | `{'name': 'Alice'}` |
| 弯引号（中文引号） | `"..."` |
| 无引号键名 | `{name: "Alice"}` |
| 无引号字符串值 | `[hello world]` |
| 尾部逗号 | `{"a":1,}` 或 `[1,2,3,]` |
| 缺少闭合括号 | `{"a":1,"b":2` |
| 注释 | `# comment`、`// comment`、`/* comment */` |
| 前后附加文字 | `Sure! Here is JSON:\n{...}\nLet me know.` |
| 缺少值 | `{key:}` → 空字符串 |

## 快速开始

### 安装

```bash
dotnet add package Novap.JsonRepair
```

### 基本用法

```csharp
using Novap.JsonRepair;

// 修复 LLM 输出中的 JSON
string? result = JsonRepairer.Repair(llmOutput);

// 安全版本（不抛异常）
if (JsonRepairer.TryRepair(llmOutput, out var json))
{
    var doc = JsonDocument.Parse(json);
    // 使用解析后的 JSON...
}
```

### API

#### `JsonRepairer.Repair(string? text) -> string?`

从原始文本中提取并修复 JSON，返回合法的 JSON 字符串。如果无法提取，返回 `null`。

#### `JsonRepairer.TryRepair(string? text, out string? result) -> bool`

`Repair` 的安全版本，不会抛出异常。成功返回 `true`，失败返回 `false`。

### 异常

当修复失败时，抛出 `JsonRepairException`，携带以下附加信息：

- `ExtractedJson` — 尝试修复的原始 JSON 文本
- `Position` — 解析失败的位置索引

## 特性

- **零外部依赖** — 完全基于 .NET BCL，无任何第三方 NuGet 包
- **AOT 兼容** — 使用 `Utf8JsonWriter` 序列化，避免反射，可在 Native AOT 场景中使用
- **针对 LLM 场景优化** — 专门处理 LLM 输出中常见的 JSON 格式问题
- **轻量级** — 源码精简，易于集成

## 技术栈

- 目标框架：.NET 10.0
- 语言：C#
- 测试：xunit

## 项目结构

```
Novap.JsonRepair/
├── JsonRepairer.cs                 # 公共 API 入口
├── JsonRepairException.cs          # 自定义异常
└── Parsing/
    ├── JsonParser.cs               # 递归下降解析器（主入口）
    ├── JsonParser.Object.cs        # 对象解析
    ├── JsonParser.Array.cs         # 数组解析
    ├── JsonParser.String.cs        # 字符串解析
    ├── JsonParser.Number.cs        # 数字解析
    ├── JsonParser.Comment.cs       # 注释解析
    └── ParseContext.cs             # 解析上下文状态栈
```

## 构建与测试

```bash
# 构建
dotnet build

# 运行测试
dotnet test
```

## 许可证

[MIT License](LICENSE) - Copyright (c) 2026 CY-Ye
