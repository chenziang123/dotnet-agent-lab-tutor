using DotNetLabTutor.Rag;
using Microsoft.Extensions.Logging.Abstractions;

var service = new RagService(NullLogger<RagService>.Instance);
service.DocumentsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "resource"));
await service.InitializeAsync();

var queries = new[] {
  "什么是 ReAct？",
  "请列出课程主题",
  "SemanticKernelAgentFramework是什么？",
  "ToolCalling在Agent里有什么作用？",
  "Semantic Kernel Agent Framework基础（01-sk-agent-framework.md）相关内容"
};

foreach (var q in queries) {
  var r = await service.SearchAsync(q, 3);
  Console.WriteLine($"Query: {q}");
  Console.WriteLine($"  Hits: {r.Count}, Top score: {(r.Count > 0 ? r[0].Score.ToString("F4") : "N/A")}, File: {(r.Count > 0 ? r[0].SourceFile : "-")}");
}
