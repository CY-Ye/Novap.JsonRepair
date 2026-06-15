# Novap.JsonRepair

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
- **LLM-optimized** — specifically designed to handle common JSON formatting issues in LLM output
- **Lightweight** — minimal source code, easy to integrate

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
