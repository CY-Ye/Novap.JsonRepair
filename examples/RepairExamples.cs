#:package Novap.JsonRepair@1.0.0
#:package Newtonsoft.Json@13.0.3

using Novap.JsonRepair;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 辅助方法：修复 + 双库反序列化
void Demo(string title, string input)
{
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine($"输入: {input}");

    var repaired = JsonRepairer.Repair(input);
    Console.WriteLine($"修复: {repaired}");

    if (repaired is not null)
    {
        var stj = System.Text.Json.JsonSerializer.Deserialize(repaired, PersonJsonContext.Default.Person);
        Console.WriteLine($"STJ: {stj}");

        var ns = JsonConvert.DeserializeObject<Person>(repaired);
        Console.WriteLine($"Newtonsoft: {ns}");
    }

    Console.WriteLine();
}

// ========== 第一部分：LLM 响应提取 ==========

Demo("Markdown 代码块", """
    Sure! Here is the JSON:
    ```json
    {"name":"Alice","age":30}
    ```
    """);

Demo("前置解释文字", """
    Based on your request, here is the result:
    {"name":"Bob","age":25}
    """);

Demo("无语言标记的代码块", """
    Here you go:
    ```
    {"name":"Charlie","age":28}
    ```
    """);

Demo("代码块 + 前后多余文字", """
    I found the user information:
    ```json
    {"name":"Diana","age":35}
    ```
    Let me know if you need more details!
    """);

// ========== 第二部分：常见畸形 JSON ==========

Demo("单引号字符串", """
    {'name': 'Eve', 'age': 30}
    """);

Demo("无引号 key", """
    {name: "Frank", age: 40}
    """);

Demo("尾部逗号", """
    {"name":"Grace","age":22,}
    """);

Demo("缺失闭合括号", """
    {"name":"Hank","age":35
    """);

Demo("注释", """
    {
      // 用户名
      "name": "Ivy",
      # 年龄
      "age": 28,
      /* 备注 */
      "active": true
    }
    """);

Demo("多种问题组合（嵌套对象 + 多种错误叠加）", """
    // 用户配置
    {
      name: 'Jack',
      age: 30,
      active: true,
      address: {
        city: 'Beijing',
        zip: '100000',
        # 详细地址暂缺
      },
      hobbies: ['reading', 'coding', /* 'gaming', */],
      scores: {
        math: 95,
        english: 88,
        science: 92,
      }
    }
    """);

// 反序列化目标类型（使用源生成器实现 AOT 兼容）
public record Person(string Name, int Age);

[JsonSerializable(typeof(Person))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class PersonJsonContext : JsonSerializerContext;
