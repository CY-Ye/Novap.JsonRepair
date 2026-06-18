#:package Novap.JsonRepair@1.0.0
#:package Microsoft.Extensions.AI.OpenAI@10.7.0
#:package Ollama@1.15.1

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Novap.JsonRepair;

Console.OutputEncoding = Encoding.UTF8;

// ── 命令行参数解析 ──────────────────────────────────────────────────────────────

string? apiKey = null;
string provider = "ollama";
string? model = null;
string? endpoint = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--openai" when i + 1 < args.Length:
            provider = "openai";
            apiKey = args[++i];
            break;
        case "--deepseek" when i + 1 < args.Length:
            provider = "deepseek";
            apiKey = args[++i];
            break;
        case "--minimax" when i + 1 < args.Length:
            provider = "minimax";
            apiKey = args[++i];
            break;
        case "--endpoint" when i + 1 < args.Length:
            endpoint = args[++i];
            break;
        case "--model" when i + 1 < args.Length:
            model = args[++i];
            break;
    }
}

// ── IChatClient 工厂（每次调用创建新客户端，避免 Ollama 连接变 stale）──────────

IChatClient CreateChatClient() => provider switch
{
    "openai" => new OpenAI.OpenAIClient(
            new ApiKeyCredential(apiKey!),
            endpoint is not null ? new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) } : null)
        .GetChatClient(model ?? "gpt-4o-mini").AsIChatClient(),
    "deepseek" => new OpenAI.OpenAIClient(
            new ApiKeyCredential(apiKey!),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint ?? "https://api.deepseek.com") })
        .GetChatClient(model ?? "deepseek-chat").AsIChatClient(),
    "minimax" => new OpenAI.OpenAIClient(
            new ApiKeyCredential(apiKey!),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint ?? "https://api.minimaxi.com/v1") })
        .GetChatClient(model ?? "MiniMax-M3").AsIChatClient(),
    _ => CreateOllamaClient(),
};

// Ollama 客户端工厂：每次创建新 HttpClient + OllamaClient，避免连接 stale
Ollama.OllamaClient CreateOllamaClient() => new(
    new HttpClient { Timeout = TimeSpan.FromMinutes(3) },
    new Uri(endpoint ?? "http://localhost:11434"));

string providerLabel = provider switch
{
    "openai" => $"OpenAI ({model ?? "gpt-4o-mini"}){(endpoint != null ? $" @ {endpoint}" : "")}",
    "deepseek" => $"DeepSeek ({model ?? "deepseek-chat"}) @ {endpoint ?? "https://api.deepseek.com"}",
    "minimax" => $"MiniMax ({model ?? "MiniMax-M3"}) @ {endpoint ?? "https://api.minimaxi.com/v1"}",
    _ => $"Ollama ({model ?? "qwen3.5:9b"}) @ {endpoint ?? "http://localhost:11434"}",
};

// ── JSON 选项（AOT 兼容）────────────────────────────────────────────────────────

var jsonContext = new AppJsonSerializerContext(
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    });

// ── System Prompt ─────────────────────────────────────────────────────────────

const string SystemPrompt = """
你是专业的写作风格提炼器。从用户提供的文本中提取可执行的写作风格规则，输出结构化的写作风格内容。

## 核心原则

- **捕捉 HOW，不复述 WHAT**：提炼写法、结构、节奏、语言习惯和创作规则，不复述情节内容。
- **拒绝通用建议**：一个规则必须在文本中反复出现，并能指导面对新场景时怎么写。把通用写作建议包装成独门技法是严重的质量失败。
- **生成导向**：每条提取结果都应能直接指导新文本的生成，而非仅仅是观察报告。

## 输入类型识别

1. **他人作品片段** — 从文本中提炼可复现的叙事技艺和语言指纹
2. **风格需求描述** — 将用户的自然语言描述解析为结构化的可执行风格条目

## 分析维度

从以下六个维度提取风格特征，每个维度可提取一条或多条：

### 1. 叙事模型
- 结构手法：视角选择、时空编排、信息铺设、多线编排
- 辨识标准：这种结构选择如何影响阅读体验

### 2. 语言指纹
- 句式节奏：平均句长、长短句切换、段落呼吸、叙述密度
- 词汇光谱：高频词类、偏好的意象词汇、禁用表达
- 对话风格：台词长短、对话标记、潜台词密度、人物间语言差异
- 感官偏好：视觉/听觉/触觉/嗅觉/味觉的侧重和叙事功能

### 3. 母题与意象
- 反复出现的主题、隐喻、象征如何在文本中变形
- 自然、器物、身体、空间、颜色等意象如何服务叙事

### 4. 人物塑造
- 角色原型、人物弧线模式
- 关系推进方式（冲突、亲密、误解、背叛、传承）
- 对话如何区分人物（口癖、用词、句式差异）
- 命名/称谓规律（如有明显规律）

### 5. 情节技法
- 开篇：如何建立张力和读者注意力
- 转场：场景如何切换
- 节奏：如何铺垫、加速、停顿、释放
- 悬念：信息如何隐藏与揭示
- 冲突：主要冲突如何升级
- 收束：结尾如何回收或留白

### 6. 不可写（风格边界）
- 该风格明确避免的写法和表达
- 与该风格冲突的元素（如风格偏冷峻，则避免过度煽情）
- 在 Content 字段中用"不可写："前缀标注具体禁令

## 技法验证标准

一个技法进入提取结果前，必须同时满足前三项，最好满足全部五项：

1. **复现性**：在同一文本中出现不止一次
2. **生成力**：能指导面对新场景时怎么写
3. **排他性**：不是所有小说都会使用的普通建议
4. **可迁移性**：规则不绑定于原文的具体人物、情节或世界观。
   - ✅ "用沉默代替对话推进情节" → 可迁移
   - ❌ "李明说话总带口头禅'嗯哼'" → 绑定特定角色
   - ✅ "为每个角色设计独特口癖" → 可迁移
   判断方法：把规则描述中的专有名词替换为泛称，如果规则仍然成立则可迁移。
5. **重要性合理性**：
   - MustFollow：必须在全文中反复出现（≥3次），且决定风格基调
   - StronglyRecommended：出现 ≥2 次，有生成力但可灵活调整
   - Optional：偶尔出现或边缘特征

不满足前三项的内容不要写入。

## 输出格式
严格输出 JSON 数组，每个元素包含：
- Name: 风格技法名称（≤20字，简洁准确）
- Description: 一句话说明此技法如何运转（≤100字）
- Content: 详细分析（包含：具体写法描述 + 原文依据 + 可操作的写作指导。必要时包含"不可写"边界。可多段落）
- Importance: 重要程度（"MustFollow" | "StronglyRecommended" | "Optional"）

## Importance 推断规则
- **MustFollow**: 贯穿始终的核心特征，反复出现（≥3次）且决定风格基调，或用户明确强调。必须满足复现性、生成力和可迁移性。
- **StronglyRecommended**: 多次出现（≥2次）但非核心的特征，有生成力但可灵活调整。
- **Optional**: 偶尔出现或边缘特征，增强风格但非必需。

## 示例 1：他人作品片段

### 输入：
"你来了。"她头也不抬，手指仍在那本泛黄的账册上划动。
我没做声，径直走到窗边。雨打在玻璃上，模糊了外面的路灯。屋里只有翻页的沙沙声。
"东西带来了？"
我摸了摸口袋里的信封，还是没开口。她终于抬起头，眼神里没有期待，更像是在确认一个已知的事实。
我点了点头。

### 输出：
[
  {
    "Name": "沉默驱动叙事",
    "Description": "用动作和沉默替代直接对话推进情节，大量留白让读者自行填补信息",
    "Content": "人物之间不通过言语交换信息，而是通过动作暗示（摸口袋、点头、走到窗边）和沉默来推进。每句话不超过十个字（"你来了""东西带来了？"），对话仅起触发作用，真正的信息传递靠肢体语言完成。\n\n写作指导：写对话时，让人物用最少的话推动场景，把信息藏在动作和停顿里。一个"摸口袋"比一段心理描写更有张力。\n\n不可写：避免大段独白式对话或过度解释人物内心的想法。",
    "Importance": "MustFollow"
  },
  {
    "Name": "环境映射心理",
    "Description": "外部环境（雨、灯光、声音）直接映衬人物内心状态，环境即情绪的外化",
    "Content": "环境不是背景板。"雨打在玻璃上，模糊了路灯"对应关系的模糊不明朗；"翻页的沙沙声"在沉默中成为唯一声响，强化压抑感。每个环境细节都有心理对应。\n\n写作指导：选择环境细节时，先问"人物此刻的情绪是什么"，然后只保留能映射这种情绪的感官细节，剔除无关描写。",
    "Importance": "MustFollow"
  },
  {
    "Name": "短句冷峻节奏",
    "Description": "以短句和短段落营造冷峻干练的阅读节奏",
    "Content": "整段以短句为主（"我没做声，径直走到窗边""我点了点头"），段落保持1-3句。句式不修饰，少用从句和形容词堆砌。这种节奏带来克制、冷峻的阅读感受。\n\n写作指导：控制平均句长在15字以内，需要强调的地方更短。段落之间留出呼吸空间。用句号制造停顿感。\n\n不可写：避免长复合句、过度修饰的形容词堆砌、抒情式段落。",
    "Importance": "StronglyRecommended"
  }
]

## 示例 2：风格需求描述

### 输入：
我希望小说的对话自然一些，不要太书面化。角色说话时应该有口癖和停顿，
就像真实生活中一样。另外打斗场景一定要快，短句为主，让读者喘不过气来。
还有就是我比较喜欢留白，不要把所有事情都解释清楚，给读者想象空间。

### 输出：
[
  {
    "Name": "对话口语化",
    "Description": "人物对话贴近真实口语，包含口癖、停顿和不完整句式",
    "Content": "对话应避免书面化的完整句子，加入真实口语特征：语气词（嗯、啊、吧）、重复、打断、迟疑（破折号或省略号表示停顿）、口癖（"那个""就是说"等）。每个人物应有各自的语言习惯，读者能从说话方式分辨角色。\n\n写作指导：写对话时先大声念出来，念着别扭的地方就是不够口语的地方。每个人的台词要有"声音"。\n\n不可写：避免书面化的完整长句、文绉绉的措辞、所有角色说话语气一致。",
    "Importance": "MustFollow"
  },
  {
    "Name": "动作场景节奏快",
    "Description": "打斗等动作场景使用极短句和快速视角切换，营造紧张节奏",
    "Content": "动作场景中，句子控制在10字以内。连续使用动作动词（劈、刺、闪、撞），减少修饰语和心理描写。段落频繁切换，每段1-2句即可换行。利用句号制造停顿感，让读者的呼吸节奏与打斗节奏同步。\n\n写作指导：把动作分解为最小单元，每个单元一句话。删除所有不推动动作的词。\n\n不可写：避免动作场景中的长段心理描写、过多形容词、连续超过3句的段落。",
    "Importance": "MustFollow"
  },
  {
    "Name": "叙事留白",
    "Description": "刻意省略部分信息，让读者自行填补和想象",
    "Content": "不过度解释人物动机、事件因果和世界观设定。关键信息通过暗示和侧面描写传递，而非直接说明。结局或重要转折点留给读者解读空间。\n\n写作指导：写完一段后问自己"这句话能不能删"，如果删掉后读者仍能理解上下文，就删掉。相信读者的理解能力。\n\n不可写：避免旁白式解释、"也就是说""换句话说"之类的总结句、把潜台词说破。",
    "Importance": "StronglyRecommended"
  }
]

## 规则
1. 必须输出合法的 JSON 数组，不要包含任何额外文字
2. 如果无法从文本中识别出风格特征，输出空数组 []
3. 每条内容独立完整，不相互引用
4. 避免输出过于宽泛的分析（如"语言流畅""文笔优美"），应具体有区分度和生成力
5. Content 字段应包含具体的可操作写作指导和原文依据，而非抽象评价
6. 不要把通用写作建议（如"注意人物一致性""情节要有起伏"）当作独特风格
7. 条目数量以用户提示中给出的建议范围为准。信息不足以支撑分析时，宁可输出更少条目也不编造

## 输出格式：
- 禁止包含 **```json**
""";

// ── 函数定义 ─────────────────────────────────────────────────────────────────

var ThinkTagRegex = new System.Text.RegularExpressions.Regex(@"<think>[\s\S]*?</think>", System.Text.RegularExpressions.RegexOptions.Compiled);

string BuildPrompt(string inputText)
{
    int charCount = inputText.Length;
    return $"""
        请分析以下文本（共 {charCount} 字），提取写作风格内容。
        建议提取 3~10 条。如果文本信息不足以支撑 3 条，可减少条目，但不要编造。

        ```
        {inputText}
        ```
        """;
}

var ParagraphSeparator = new Regex(@"(?:\r?\n|\r)(?:[ \t　]*(?:\r?\n|\r))+", RegexOptions.Compiled);

List<string> SplitIntoChunks(string text)
{
    const int smallTextThreshold = 5000;
    const int mediumChunkSize = 3000;
    const int largeChunkSize = 5000;
    const int largeTextThreshold = 50000;
    const int veryLargeTextThreshold = 200000;
    const int maxSampleChunks = 50;
    const int MinExtractLength = 200;

    if (text.Length < smallTextThreshold) return [text];

    int chunkSize = text.Length <= largeTextThreshold ? mediumChunkSize : largeChunkSize;
    var paragraphs = ParagraphSeparator.Split(text);
    var chunks = new List<string>();
    var currentChunk = new StringBuilder();

    foreach (var para in paragraphs)
    {
        if (string.IsNullOrWhiteSpace(para)) continue;
        var trimmed = para.Trim();
        if (trimmed.Length == 0) continue;
        if (currentChunk.Length > 0 && currentChunk.Length + 2 + trimmed.Length > chunkSize)
        {
            chunks.Add(currentChunk.ToString());
            currentChunk.Clear();
        }
        if (currentChunk.Length > 0) currentChunk.Append("\n\n");
        currentChunk.Append(trimmed);
    }

    if (currentChunk.Length > 0) chunks.Add(currentChunk.ToString());

    var validChunks = chunks.Where(c => c.Length >= MinExtractLength).ToList();
    if (validChunks.Count == 0) return [text];
    if (text.Length > veryLargeTextThreshold && validChunks.Count > maxSampleChunks)
        validChunks = SampleChunks(validChunks, maxSampleChunks);
    return validChunks;
}

List<string> SampleChunks(List<string> chunks, int maxCount)
{
    if (chunks.Count <= maxCount) return chunks;
    var sampled = new List<string> { chunks[0] };
    double step = (double)(chunks.Count - 1) / (maxCount - 1);
    for (int i = 1; i < maxCount - 1; i++)
    {
        int index = (int)Math.Round(i * step);
        sampled.Add(chunks[index]);
    }
    sampled.Add(chunks[^1]);
    return sampled;
}

var ImportanceOrder = new Dictionary<string, int>
{
    ["MustFollow"] = 0, ["StronglyRecommended"] = 1, ["Optional"] = 2,
};

List<ExtractedWritingStyleContent> MergeResults(List<ExtractedWritingStyleContent[]> allChunks)
{
    if (allChunks.Count == 0) return [];
    if (allChunks.Count == 1) return allChunks[0].Where(x => !string.IsNullOrEmpty(x.Name)).ToList();

    var allItems = new List<(ExtractedWritingStyleContent Item, int ChunkIndex)>();
    for (int i = 0; i < allChunks.Count; i++)
        foreach (var item in allChunks[i])
            if (!string.IsNullOrEmpty(item.Name))
                allItems.Add((item, i));

    var seen = new Dictionary<string, (ExtractedWritingStyleContent Item, int Index)>();
    foreach (var entry in allItems)
    {
        var name = entry.Item.Name ?? "";
        if (seen.TryGetValue(name, out var existing))
        {
            if (GetImportanceRank(entry.Item.Importance) < GetImportanceRank(existing.Item.Importance))
                seen[name] = (entry.Item, entry.ChunkIndex);
        }
        else seen[name] = (entry.Item, entry.ChunkIndex);
    }
    var exactDeduped = seen.Values.OrderBy(x => x.Index).ToList();

    var result = new List<(ExtractedWritingStyleContent Item, int Index)>();
    foreach (var entry in exactDeduped)
    {
        bool isDuplicate = false;
        for (int i = 0; i < result.Count; i++)
        {
            double sim = LevenshteinSimilarity(entry.Item.Name, result[i].Item.Name);
            if (sim >= 0.65)
            {
                if (GetImportanceRank(entry.Item.Importance) < GetImportanceRank(result[i].Item.Importance))
                    result[i] = entry;
                isDuplicate = true;
                break;
            }
        }
        if (!isDuplicate) result.Add(entry);
    }
    return result.Select(x => x.Item).ToList();
}

int GetImportanceRank(string? importance)
    => importance is not null && ImportanceOrder.TryGetValue(importance, out var rank) ? rank : 99;

double LevenshteinSimilarity(string? s1, string? s2)
{
    if (s1 == s2) return 1.0;
    if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;
    int distance = LevenshteinDistance(s1, s2);
    return 1.0 - (double)distance / Math.Max(s1.Length, s2.Length);
}

int LevenshteinDistance(string s1, string s2)
{
    var d = new int[s1.Length + 1, s2.Length + 1];
    for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
    for (int j = 0; j <= s2.Length; j++) d[0, j] = j;
    for (int i = 1; i <= s1.Length; i++)
        for (int j = 1; j <= s2.Length; j++)
        {
            int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }
    return d[s1.Length, s2.Length];
}

async Task<(string RawOutput, bool BeforeOk, ExtractedWritingStyleContent[]? BeforeResult, string? BeforeError,
             bool AfterOk, ExtractedWritingStyleContent[]? AfterResult, string? RepairedJson, double RepairMs)>
    ProcessChunkAsync(string chunkText)
{
    const int maxRetries = 3;
    const int timeoutMinutes = 10;

    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, SystemPrompt),
        new(ChatRole.User, BuildPrompt(chunkText)),
    };

    var chatOptions = new ChatOptions
    {
        MaxOutputTokens = 40960,
        ModelId = provider == "ollama" ? (model ?? "qwen3.5:9b") : null,
        AdditionalProperties = provider == "minimax"
            ? new AdditionalPropertiesDictionary { ["reasoning_split"] = true }
            : null,
    };

    // 带重试的 LLM 调用（共享 CancellationToken，超时时真正取消 HTTP 请求）
    string rawOutput = "";
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (attempt > 1)
            {
                Console.WriteLine($"│ │ 🔄 重试 {attempt}/{maxRetries}...");
                Console.Out.Flush();
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
            using var client = CreateChatClient();
            var response = await client.GetResponseAsync(messages, chatOptions, cancellationToken: cts.Token);
            rawOutput = response.Text;
            break;
        }
        catch (OperationCanceledException) when (attempt < maxRetries)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"│ │ ⏰ 超时（{timeoutMinutes}分钟），正在重试...");
            Console.ResetColor();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    // 剥离 <think>...</think> 标签（MiniMax-M3 等推理模型可能将思考内容混入 content）
    rawOutput = ThinkTagRegex.Replace(rawOutput, "").Trim();

    ExtractedWritingStyleContent[]? beforeResult = null;
    bool beforeOk = false;
    string? beforeError = null;
    try
    {
        beforeResult = JsonSerializer.Deserialize(rawOutput, jsonContext.ExtractedWritingStyleContentArray);
        beforeOk = beforeResult is not null;
    }
    catch (Exception ex) { beforeError = ex.Message; }

    ExtractedWritingStyleContent[]? afterResult = null;
    bool afterOk = false;
    string? repairedJson = null;
    double repairMs = 0;
    try
    {
        var sw = Stopwatch.StartNew();
        repairedJson = JsonRepairer.Repair(rawOutput);
        sw.Stop();
        repairMs = sw.Elapsed.TotalMilliseconds;
        if (repairedJson is not null)
        {
            afterResult = JsonSerializer.Deserialize(repairedJson, jsonContext.ExtractedWritingStyleContentArray);
            afterOk = afterResult is not null;
        }
    }
    catch { }

    return (rawOutput, beforeOk, beforeResult, beforeError, afterOk, afterResult, repairedJson, repairMs);
}

// ── 主处理流程 ───────────────────────────────────────────────────────────────

var novelDir = Path.GetFullPath("examples/data/novel");
var reportDir = Path.GetFullPath("examples/data/report");
Directory.CreateDirectory(reportDir);

if (!Directory.Exists(novelDir))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"错误：数据目录不存在: {novelDir}");
    Console.ResetColor();
    return 1;
}

var txtFiles = Directory.GetFiles(novelDir, "*.txt").OrderBy(f => f).ToArray();
if (txtFiles.Length == 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"提示：数据目录为空: {novelDir}");
    Console.ResetColor();
    return 1;
}

var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
var reportPath = Path.Combine(reportDir, $"report-{timestamp}.json");
var logWriter = new StreamWriter(reportPath, false, Encoding.UTF8);
var logEntries = new List<FileLog>();

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  Agent 写作风格提取效果报告");
Console.WriteLine($"  Provider: {providerLabel}");
Console.WriteLine($"  数据目录: examples/data/novel/");
Console.WriteLine($"  日志文件: {reportPath}");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine();

int totalFiles = 0, totalChunks = 0;
int beforeSuccess = 0, afterSuccess = 0;
int emptyOutputCount = 0, llmErrorCount = 0;
double totalRepairMs = 0, totalLlmMs = 0;
int totalMergedItems = 0;
bool hasErrors = false;

for (int fileIdx = 0; fileIdx < txtFiles.Length; fileIdx++)
{
    var filePath = txtFiles[fileIdx];
    var fileName = Path.GetFileName(filePath);
    var fileContent = File.ReadAllText(filePath, Encoding.UTF8);
    var chunks = SplitIntoChunks(fileContent);
    totalFiles++;
    totalChunks += chunks.Count;

    Console.WriteLine($"┌─ 文件 {fileIdx + 1}/{txtFiles.Length}: {fileName} ({fileContent.Length:N0} 字, {chunks.Count} 块) ─");

    var chunkResults = new List<ExtractedWritingStyleContent[]>();
    var logChunks = new List<ChunkLog>();

    for (int chunkIdx = 0; chunkIdx < chunks.Count; chunkIdx++)
    {
        var chunk = chunks[chunkIdx];
        Console.WriteLine($"│");
        Console.WriteLine($"│ ┌─ 块 {chunkIdx + 1}/{chunks.Count} ({chunk.Length:N0} 字) ────────────────");
        Console.WriteLine($"│ │");
        Console.WriteLine($"│ │ ⏳ 正在调用 LLM... [{DateTime.Now:HH:mm:ss}]");
        Console.Out.Flush();

        try
        {
            var llmSw = Stopwatch.StartNew();
            var (rawOutput, beforeOk, beforeResult, beforeError, afterOk, afterResult, repairedJson, repairMs) =
                await ProcessChunkAsync(chunk);
            llmSw.Stop();
            var llmMs = llmSw.Elapsed.TotalMilliseconds;
            totalLlmMs += llmMs;

            logChunks.Add(new ChunkLog
            {
                ChunkIndex = chunkIdx,
                ChunkChars = chunk.Length,
                SystemPrompt = SystemPrompt,
                UserPrompt = BuildPrompt(chunk),
                LlmOutput = rawOutput,
                LlmDurationMs = Math.Round(llmMs, 1),
                Before = new ChunkBeforeLog { Success = beforeOk, Error = beforeError, Result = beforeResult },
                After = new ChunkAfterLog { Success = afterOk, RepairedJson = repairedJson, RepairDurationMs = Math.Round(repairMs, 3), Result = afterResult },
            });

            Console.WriteLine($"│ │ [LLM 原始输出]");

            // 检测空输出
            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                emptyOutputCount++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"│ │   (空输出，LLM 未返回任何内容)");
                Console.ResetColor();
                Console.WriteLine($"│ │");
                Console.WriteLine($"│ │ [Before - 直接解析] ⏭️ 跳过（空输出）");
                Console.WriteLine($"│ │ [After  - 修复后解析] ⏭️ 跳过（空输出）");
            }
            else
            {
                var displayOutput = rawOutput.Length > 500 ? rawOutput[..500] + "..." : rawOutput;
                foreach (var line in displayOutput.Split('\n'))
                    Console.WriteLine($"│ │   {line}");
                Console.WriteLine($"│ │");

                if (beforeOk)
                {
                    beforeSuccess++;
                    Console.WriteLine($"│ │ [Before - 直接解析] ✅ {beforeResult!.Length} 条提取成功");
                }
                else
                {
                    var errMsg = beforeError ?? "解析结果为空";
                    if (errMsg.Length > 80) errMsg = errMsg[..80] + "...";
                    Console.WriteLine($"│ │ [Before - 直接解析] ❌ {errMsg}");
                }

                if (afterOk)
                {
                    afterSuccess++;
                    totalRepairMs += repairMs;
                    Console.WriteLine($"│ │ [After  - 修复后解析] ✅ {afterResult!.Length} 条提取成功");
                    Console.WriteLine($"│ │   ├─ 耗时: {repairMs * 1000:F3}us");
                    Console.WriteLine($"│ │   └─ 结果:");
                    for (int i = 0; i < afterResult.Length; i++)
                        Console.WriteLine($"│ │      {i + 1}. {afterResult[i].Name ?? "(null)"} [{afterResult[i].Importance ?? "(null)"}]");
                    chunkResults.Add(afterResult);
                }
                else
                {
                    Console.WriteLine($"│ │ [After  - 修复后解析] ❌ 修复后仍无法解析");
                }
            }
        }
        catch (OperationCanceledException)
        {
            hasErrors = true;
            llmErrorCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"│ │ [超时] LLM 调用 3 次均超时（每次 3 分钟），已跳过");
            Console.ResetColor();
            logChunks.Add(new ChunkLog { ChunkIndex = chunkIdx, ChunkChars = chunk.Length, Error = "超时（3次重试均超过3分钟）" });
        }
        catch (Exception ex)
        {
            hasErrors = true;
            llmErrorCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"│ │ [错误] LLM 调用失败: {ex.Message}");
            Console.ResetColor();
            logChunks.Add(new ChunkLog { ChunkIndex = chunkIdx, ChunkChars = chunk.Length, Error = ex.Message });
        }

        Console.WriteLine($"│ └──────────────────────────────────────────────────────");
    }

    List<ExtractedWritingStyleContent> merged = [];
    if (chunkResults.Count > 0)
    {
        var rawItemCount = chunkResults.Sum(r => r.Length);
        merged = MergeResults(chunkResults);
        totalMergedItems += merged.Count;
        Console.WriteLine($"│");
        Console.WriteLine($"│ [合并结果] {merged.Count} 条（去重前 {rawItemCount} 条）");
        for (int i = 0; i < merged.Count; i++)
            Console.WriteLine($"│   {i + 1}. {merged[i].Name ?? "(null)"} [{merged[i].Importance ?? "(null)"}]");
    }

    Console.WriteLine($"└─────────────────────────────────────────────────────────");
    Console.WriteLine();
    logEntries.Add(new FileLog { FileName = fileName, CharCount = fileContent.Length, Chunks = logChunks, MergedResult = merged });
}

var report = new ReportLog
{
    Timestamp = DateTime.UtcNow.ToString("o"),
    Provider = providerLabel,
    Files = logEntries,
    Summary = new SummaryLog
    {
        TotalFiles = totalFiles,
        TotalChunks = totalChunks,
        EmptyOutputCount = emptyOutputCount,
        LlmErrorCount = llmErrorCount,
        BeforeSuccessRate = $"{beforeSuccess}/{totalChunks - emptyOutputCount - llmErrorCount} ({((totalChunks - emptyOutputCount - llmErrorCount) > 0 ? 100.0 * beforeSuccess / (totalChunks - emptyOutputCount - llmErrorCount) : 0):F0}%)",
        AfterSuccessRate = $"{afterSuccess}/{totalChunks - emptyOutputCount - llmErrorCount} ({((totalChunks - emptyOutputCount - llmErrorCount) > 0 ? 100.0 * afterSuccess / (totalChunks - emptyOutputCount - llmErrorCount) : 0):F0}%)",
        AvgRepairMs = afterSuccess > 0 ? Math.Round(totalRepairMs / afterSuccess, 3) : 0,
        AvgLlmDurationMs = totalChunks > 0 ? Math.Round(totalLlmMs / totalChunks / 1000, 1) : 0,
    },
};

await logWriter.WriteAsync(JsonSerializer.Serialize(report, jsonContext.ReportLog));
await logWriter.DisposeAsync();

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  汇总统计");
Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine($"  文件总数:          {totalFiles}");
Console.WriteLine($"  分块总数:          {totalChunks}");
Console.WriteLine($"  空输出:            {emptyOutputCount} 块（不计入成功率）");
Console.WriteLine($"  LLM 运行错误:      {llmErrorCount} 块（不计入成功率）");
var validChunks = totalChunks - emptyOutputCount - llmErrorCount;
Console.WriteLine($"  直接解析成功率:     {beforeSuccess}/{validChunks} ({(validChunks > 0 ? 100.0 * beforeSuccess / validChunks : 0):F0}%)");
Console.WriteLine($"  修复后成功率:       {afterSuccess}/{validChunks} ({(validChunks > 0 ? 100.0 * afterSuccess / validChunks : 0):F0}%)");
Console.WriteLine($"  平均修复耗时:       {(afterSuccess > 0 ? totalRepairMs / afterSuccess * 1000 : 0):F3}us");
Console.WriteLine($"  平均 LLM 响应时间:  {(validChunks > 0 ? totalLlmMs / validChunks / 1000 : 0):F1}s");
Console.WriteLine($"  合并后平均条目数:   {(totalFiles > 0 ? (double)totalMergedItems / totalFiles : 0):F1} 条/文件");
Console.WriteLine($"  日志已保存:         {reportPath}");
Console.WriteLine("═══════════════════════════════════════════════════════════");

return hasErrors ? 1 : 0;

// ── 数据模型（named type，必须在所有 top-level statements 之后）──────────────

public record ExtractedWritingStyleContent(
    string Name, string Description, string Content, string Importance);

public record ChunkBeforeLog
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ExtractedWritingStyleContent[]? Result { get; init; }
}

public record ChunkAfterLog
{
    public bool Success { get; init; }
    public string? RepairedJson { get; init; }
    public double RepairDurationMs { get; init; }
    public ExtractedWritingStyleContent[]? Result { get; init; }
}

public record ChunkLog
{
    public int ChunkIndex { get; init; }
    public int ChunkChars { get; init; }
    public string? SystemPrompt { get; init; }
    public string? UserPrompt { get; init; }
    public string? LlmOutput { get; init; }
    public double LlmDurationMs { get; init; }
    public ChunkBeforeLog? Before { get; init; }
    public ChunkAfterLog? After { get; init; }
    public string? Error { get; init; }
}

public record FileLog
{
    public string FileName { get; init; } = "";
    public int CharCount { get; init; }
    public List<ChunkLog> Chunks { get; init; } = [];
    public List<ExtractedWritingStyleContent> MergedResult { get; init; } = [];
}

public record SummaryLog
{
    public int TotalFiles { get; init; }
    public int TotalChunks { get; init; }
    public int EmptyOutputCount { get; init; }
    public int LlmErrorCount { get; init; }
    public string BeforeSuccessRate { get; init; } = "";
    public string AfterSuccessRate { get; init; } = "";
    public double AvgRepairMs { get; init; }
    public double AvgLlmDurationMs { get; init; }
}

public record ReportLog
{
    public string Timestamp { get; init; } = "";
    public string Provider { get; init; } = "";
    public List<FileLog> Files { get; init; } = [];
    public SummaryLog Summary { get; init; } = new();
}

[JsonSerializable(typeof(ReportLog))]
[JsonSerializable(typeof(ExtractedWritingStyleContent[]))]
[JsonSerializable(typeof(ChunkLog))]
[JsonSerializable(typeof(FileLog))]
[JsonSerializable(typeof(ChunkBeforeLog))]
[JsonSerializable(typeof(ChunkAfterLog))]
[JsonSerializable(typeof(SummaryLog))]
[JsonSerializable(typeof(List<ExtractedWritingStyleContent>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
