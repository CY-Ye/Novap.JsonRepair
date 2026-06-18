# Novap.JsonRepair

[![NuGet](https://img.shields.io/nuget/v/Novap.JsonRepair)](https://www.nuget.org/packages/Novap.JsonRepair)

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
| Python 字面量 | `None` → `null`、`True` → `true`、`False` → `false` |

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
    // 使用修复后的 JSON 字符串
    Console.WriteLine(json);
}
```

### 使用 System.Text.Json 反序列化

```csharp
using System.Text.Json;
using Novap.JsonRepair;

string? json = JsonRepairer.Repair(llmOutput);
if (json is not null)
{
    var user = JsonSerializer.Deserialize<User>(json);
}

public record User(string Name, int Age, string[] Hobbies);
```

### 使用 Newtonsoft.Json 反序列化

```bash
dotnet add package Newtonsoft.Json
```

```csharp
using Newtonsoft.Json;
using Novap.JsonRepair;

string? json = JsonRepairer.Repair(llmOutput);
if (json is not null)
{
    var user = JsonConvert.DeserializeObject<User>(json);
}

public record User(string Name, int Age, string[] Hobbies);
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
- **AOT 兼容** — 零反射代码，可在 Native AOT 场景中使用
- **性能优化** — 基于 `Span`/`ReadOnlySpan` 解析、`ArrayPool` 复用，最小化 GC 分配
- **针对 LLM 场景优化** — 专门处理 LLM 输出中常见的 JSON 格式问题
- **轻量级** — 源码精简，易于集成

## 性能

通过 `Span<T>` / `ReadOnlySpan<char>` / `ArrayPool<T>` 优化，最大程度降低 GC 压力。核心优化手段：

- **基于 Span 的字符串解析** — `ParseString()` 对无转义字符串直接使用 `ReadOnlySpan<char>` 切片，完全消除 `StringBuilder` 分配
- **基于 Span 的数字解析** — `ParseNumber()` 直接在 `ReadOnlySpan<char>` 上调用 `long.TryParse` / `double.TryParse`，避免中间字符串分配
- **ArrayPool 验证** — `IsValidJson()` 从 `ArrayPool<byte>` 租用字节缓冲区，而非每次分配新数组
- **零拷贝序列化** — `SerializeToString()` 使用 `MemoryStream.GetBuffer()` 替代 `ToArray()`
- **Switch 字符查找** — 热路径字符分类使用模式匹配 `is` 表达式替代 `HashSet<char>`

基准测试结果（.NET 10.0, Release, win-x64）：

| 场景 | 平均耗时 (μs) | 内存分配 (KB) |
|:-----|------------:|------------:|
| 有效 JSON（快速路径） | 1.0 | 0.01 |
| 有效 JSON（大负载） | 4.2 | 0.01 |
| 单引号 + 尾逗号 | 11.0 | 2.4 |
| 无引号键名 | 9.8 | 2.4 |
| 缺少闭合括号 | 11.4 | 2.8 |
| Markdown 代码块包裹 | 0.5 | 0.1 |
| Python 常量 | 13.1 | 2.9 |
| 带注释的 JSON | 15.6 | 3.1 |
| 多种问题混合 | 13.1 | 3.1 |
| 大型畸形 JSON（10 个对象） | 67.0 | 18.6 |

> 有效 JSON 快速路径通过 `Utf8JsonReader` 验证后直接返回，约 1μs 完成且几乎零分配。
> 完整基准测试脚本见 [`examples/BenchRepair.cs`](examples/BenchRepair.cs)。

## Agent 集成实测

使用 LLM 写作风格提取任务进行真实评测（3 部小说，共 37 个分块，每块约 5000 字）：

| LLM 模型 | 直接解析 | 使用 JsonRepair | 平均修复耗时 |
|----------|---------|----------------|------------|
| MiniMax-M2.7 | 28/35 (80%) | **35/35 (100%)** | 0.3ms |
| MiniMax-M3 | 7/27 (26%) | **27/27 (100%)** | 0.4ms |

> LLM 经常在 JSON 外包裹 Markdown 围栏、添加解释文字或输出格式错误的 JSON。
> `JsonRepairer.Repair()` 可在亚毫秒时间内恢复所有这些情况。

<details>
<summary>测试报告文件</summary>

- [`MiniMax-M2.7`](examples/data/report/report-20260618-135027.json)
- [`MiniMax-M3`](examples/data/report/report-20260618-102158.json)

</details>

## 使用案例

使用 **Novap.JsonRepair** 的项目：

- [Novap](https://apps.microsoft.com/store/detail/9P6GJ1M3MQ8F?cid=DevShareMCLPCS) — 基于 LLM 的小说写作助手，已上架 Microsoft Store。

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
