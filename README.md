# Novap.JsonRepair

[![NuGet](https://img.shields.io/nuget/v/Novap.JsonRepair)](https://www.nuget.org/packages/Novap.JsonRepair)

**English** | [简体中文](README.zh-CN.md)

A lightweight .NET library for extracting and repairing malformed JSON from LLM (Large Language Model) raw response text.

## Introduction

When LLMs output JSON, the responses often contain non-standard formatting such as markdown code blocks, single-quoted strings, unquoted keys, trailing commas, missing closing brackets, and comments. Standard `System.Text.Json` parsers cannot handle these. **Novap.JsonRepair** uses a custom recursive-descent parser to repair these "broken" JSON strings into valid JSON.

## Repair Capabilities

| Issue Type | Example |
|-----------|---------|
| Markdown code block wrapping | `` ```json {...} ``` `` |
| Single-quoted strings | `{'name': 'Alice'}` |
| Curly/smart quotes | `"..."` |
| Unquoted keys | `{name: "Alice"}` |
| Unquoted string values | `[hello world]` |
| Trailing commas | `{"a":1,}` or `[1,2,3,]` |
| Missing closing brackets | `{"a":1,"b":2` |
| Comments | `# comment`, `// comment`, `/* comment */` |
| Surrounding text | `Sure! Here is JSON:\n{...}\nLet me know.` |
| Missing values | `{key:}` → empty string |
| Python literals | `None` → `null`, `True` → `true`, `False` → `false` |

## Quick Start

### Installation

```bash
dotnet add package Novap.JsonRepair
```

### Basic Usage

```csharp
using Novap.JsonRepair;

// Repair JSON from LLM output
string? result = JsonRepairer.Repair(llmOutput);

// Safe version (no exceptions thrown)
if (JsonRepairer.TryRepair(llmOutput, out var json))
{
    // Use the repaired JSON string
    Console.WriteLine(json);
}
```

### Deserialize with System.Text.Json

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

### Deserialize with Newtonsoft.Json

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

Extracts and repairs JSON from raw text, returning a valid JSON string. Returns `null` if extraction fails.

#### `JsonRepairer.TryRepair(string? text, out string? result) -> bool`

Safe version of `Repair` that does not throw exceptions. Returns `true` on success, `false` on failure.

### Exceptions

On repair failure, a `JsonRepairException` is thrown with the following additional information:

- `ExtractedJson` — the raw JSON text that was attempted to repair
- `Position` — the index position where parsing failed

## Features

- **Zero external dependencies** — built entirely on .NET BCL, no third-party NuGet packages
- **AOT compatible** — zero reflection code, works in Native AOT scenarios
- **Performance optimized** — `Span`/`ReadOnlySpan`-based parsing, `ArrayPool` reuse, minimal GC allocations
- **LLM-optimized** — specifically designed to handle common JSON formatting issues in LLM output
- **Lightweight** — minimal source code, easy to integrate

## Performance

Optimized with `Span<T>` / `ReadOnlySpan<char>` / `ArrayPool<T>` for minimal GC pressure. Key techniques:

- **Span-based string parsing** — `ParseString()` uses direct `ReadOnlySpan<char>` slicing for strings without escape sequences, eliminating `StringBuilder` allocation entirely
- **Span-based number parsing** — `ParseNumber()` calls `long.TryParse` / `double.TryParse` directly on `ReadOnlySpan<char>`, avoiding intermediate string allocation
- **ArrayPool for validation** — `IsValidJson()` rents byte buffers from `ArrayPool<byte>` instead of allocating new arrays
- **Zero-copy serialization** — `SerializeToString()` uses `MemoryStream.GetBuffer()` instead of `ToArray()`
- **Switch-based char lookup** — hot-path character classification uses pattern-matching `is` expressions instead of `HashSet<char>`

Benchmark results (.NET 10.0, Release, win-x64):

| Scenario | Avg (μs) | Alloc (KB) |
|:---------|--------:|-----------:|
| Valid JSON (fast path) | 1.0 | 0.01 |
| Valid JSON (large payload) | 4.2 | 0.01 |
| Single quotes + trailing comma | 11.0 | 2.4 |
| Unquoted keys | 9.8 | 2.4 |
| Missing brackets | 11.4 | 2.8 |
| Markdown fenced | 0.5 | 0.1 |
| Python constants | 13.1 | 2.9 |
| JSON with comments | 15.6 | 3.1 |
| Mixed problems | 13.1 | 3.1 |
| Large malformed (10 objects) | 67.0 | 18.6 |

> Valid JSON fast path skips the repair parser entirely via `Utf8JsonReader` validation, completing in ~1μs with near-zero allocation.
> Full benchmark script available at [`examples/BenchRepair.cs`](examples/BenchRepair.cs).

## Agent Integration Benchmark

Real-world evaluation with LLM-based writing style extraction (3 novels, 37 chunks total, ~5000 chars each):

| LLM Model | Direct Parse | With JsonRepair | Avg Repair Time |
|-----------|-------------|-----------------|-----------------|
| MiniMax-M2.7 | 28/35 (80%) | **35/35 (100%)** | 0.3ms |
| MiniMax-M3 | 7/27 (26%) | **27/27 (100%)** | 0.4ms |

> LLMs frequently wrap JSON in markdown fences, prepend explanation text, or output malformed JSON.
> `JsonRepairer.Repair()` recovers all of these cases in sub-millisecond time.

<details>
<summary>Report Files</summary>

- [`MiniMax-M2.7`](examples/data/report/report-20260618-135027.json)
- [`MiniMax-M3`](examples/data/report/report-20260618-102158.json)


</details>

## Showcase

Projects using **Novap.JsonRepair**:

- [Novap](https://apps.microsoft.com/store/detail/9P6GJ1M3MQ8F?cid=DevShareMCLPCS) — A novel writing assistant powered by LLM, available on Microsoft Store.

## Tech Stack

- Target framework: .NET 10.0
- Language: C#
- Testing: xunit

## Project Structure

```
Novap.JsonRepair/
├── JsonRepairer.cs                 # Public API entry point
├── JsonRepairException.cs          # Custom exception
└── Parsing/
    ├── JsonParser.cs               # Recursive-descent parser (main entry)
    ├── JsonParser.Object.cs        # Object parsing
    ├── JsonParser.Array.cs         # Array parsing
    ├── JsonParser.String.cs        # String parsing
    ├── JsonParser.Number.cs        # Number parsing
    ├── JsonParser.Comment.cs       # Comment parsing
    └── ParseContext.cs             # Parser context state stack
```

## Build & Test

```bash
# Build
dotnet build

# Run tests
dotnet test
```

## License

[MIT License](LICENSE) - Copyright (c) 2026 CY-Ye
