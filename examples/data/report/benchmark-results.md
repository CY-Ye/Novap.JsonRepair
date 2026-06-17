# Novap.JsonRepair Benchmark Results

- **Date**: 2026-06-17 22:16:43
- **Runtime**: win-x64
- **Framework**: .NET 10.0.8
- **Warmup**: 100  |  **Iterations**: 5000

| Scenario | Avg (us) | Min (us) | P50 (us) | P95 (us) | P99 (us) | Alloc (KB) |
|:---------|--------:|--------:|--------:|--------:|--------:|-----------:|
| ValidJson (fast path) | 1.02 | 0.80 | 0.90 | 1.30 | 3.60 | 0.14 |
| ValidJson (large payload) | 4.32 | 4.00 | 4.20 | 6.00 | 7.60 | 0.58 |
| SingleQuotes + TrailingComma | 11.01 | 9.80 | 10.60 | 13.60 | 16.00 | 4.01 |
| UnquotedKeys | 10.42 | 9.40 | 10.10 | 13.30 | 15.10 | 3.59 |
| MissingBrackets | 12.58 | 11.30 | 12.20 | 15.30 | 18.40 | 4.42 |
| MarkdownFenced | 0.49 | 0.40 | 0.50 | 0.50 | 0.90 | 0.27 |
| PythonConstants | 12.65 | 11.80 | 12.60 | 13.00 | 17.80 | 4.91 |
| JsonWithComments | 16.41 | 15.30 | 16.30 | 16.80 | 21.90 | 5.32 |
| UnquotedStrings (array) | 11.89 | 10.80 | 11.70 | 12.40 | 18.10 | 4.04 |
| MixedProblems | 14.26 | 12.90 | 13.70 | 17.10 | 20.10 | 4.02 |
| LargeMalformed (10 users) | 79.75 | 73.20 | 77.20 | 90.70 | 115.20 | 32.50 |


---

# Novap.JsonRepair Benchmark Results

- **Date**: 2026-06-17 22:17:08
- **Runtime**: win-x64
- **Framework**: .NET 10.0.8
- **Warmup**: 100  |  **Iterations**: 5000

| Scenario | Avg (us) | Min (us) | P50 (us) | P95 (us) | P99 (us) | Alloc (KB) |
|:---------|--------:|--------:|--------:|--------:|--------:|-----------:|
| ValidJson (fast path) | 0.99 | 0.90 | 1.00 | 1.10 | 1.10 | 0.01 |
| ValidJson (large payload) | 4.25 | 4.10 | 4.20 | 4.30 | 4.60 | 0.01 |
| SingleQuotes + TrailingComma | 11.24 | 9.20 | 10.10 | 14.40 | 18.00 | 2.40 |
| UnquotedKeys | 10.18 | 8.90 | 9.50 | 13.30 | 16.60 | 2.41 |
| MissingBrackets | 11.12 | 10.40 | 10.90 | 11.60 | 15.00 | 2.84 |
| MarkdownFenced | 0.53 | 0.40 | 0.50 | 0.60 | 0.90 | 0.11 |
| PythonConstants | 11.48 | 10.70 | 11.20 | 12.00 | 17.90 | 2.91 |
| JsonWithComments | 15.64 | 14.30 | 15.20 | 16.10 | 23.90 | 3.09 |
| UnquotedStrings (array) | 10.96 | 10.00 | 10.80 | 11.60 | 15.80 | 2.52 |
| MixedProblems | 12.89 | 12.00 | 12.60 | 13.60 | 21.30 | 3.05 |
| LargeMalformed (10 users) | 68.52 | 53.60 | 66.90 | 78.90 | 109.60 | 18.60 |

