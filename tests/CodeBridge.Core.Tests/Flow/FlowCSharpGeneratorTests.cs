using System.Reflection;
using CodeBridge.Core.Abstractions;
using CodeBridge.ESP32;
using CodeBridge.Flow;
using CodeBridge.Flow.CodeGeneration;
using CodeBridge.Flow.Execution;
using CodeBridge.Flow.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeBridge.Core.Tests.Flow;

/// <summary>
/// "Export to C#" must produce code that compiles and behaves exactly like the flow runtime: the same commands reach
/// the board in the same order. Both are checked with the shipped example flows.
/// </summary>
public class FlowCSharpGeneratorTests
{
    private sealed class ScriptedTransport : ITransport
    {
        private readonly List<string> _sent = new();
        private bool _connected;

        public bool IsConnected => _connected;
        public IReadOnlyList<string> SentCommands => _sent;
        public event EventHandler<DataReceivedEventArgs>? DataReceived { add { } remove { } }

        public Task ConnectAsync(CancellationToken ct = default) { _connected = true; return Task.CompletedTask; }
        public Task DisconnectAsync(CancellationToken ct = default) { _connected = false; return Task.CompletedTask; }
        public Task SendRawAsync(byte[] data, CancellationToken ct = default) => Task.CompletedTask;
        public Task<byte[]> ReceiveRawAsync(int length, CancellationToken ct = default) => Task.FromResult(new byte[length]);
        public void Dispose() { }

        public Task<string> SendCommandAsync(string command, CancellationToken ct = default)
        {
            _sent.Add(command);
            var response = command switch
            {
                _ when command.StartsWith("PING") => "OK:PONG",
                _ when command.StartsWith("VER") => "OK:0.8.0",
                _ when command.StartsWith("AR:") => "OK:1500",
                _ when command.StartsWith("DR:") => "OK:1",
                _ => "OK"
            };
            return Task.FromResult(response);
        }
    }

    private static string ItemTemplates()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "CodeBridge.VisualStudio", "ItemTemplates");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("ItemTemplates not found.");
    }

    public static IEnumerable<object[]> Examples() =>
        Directory.EnumerateFiles(ItemTemplates(), "Example*.cbflow", SearchOption.AllDirectories)
            .OrderBy(path => path)
            .Select(path => new object[] { Path.GetFileName(path) });

    private static FlowDocument Load(string fileName)
    {
        var path = Directory.EnumerateFiles(ItemTemplates(), fileName, SearchOption.AllDirectories).Single();
        return FlowDocumentJson.Deserialize(File.ReadAllText(path));
    }

    private static Assembly Compile(string code, bool console = false)
    {
        // The whole runtime (not only what is loaded yet) plus the CodeBridge assemblies the generated code uses.
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).ToList();
        foreach (var assembly in new[] { typeof(IBoard).Assembly, typeof(ESP32Board).Assembly, typeof(CodeBridge.Transport.BoardDiscovery).Assembly, typeof(FlowDocument).Assembly })
            paths.Add(assembly.Location);

        var references = paths.Distinct().Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();

        var compilation = CSharpCompilation.Create(
            "Exported_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest)) },
            references,
            new CSharpCompilationOptions(console ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        var problems = result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).Select(d => d.ToString()).ToList();
        Assert.True(result.Success && problems.Count == 0, "Generated code does not compile cleanly:" + Environment.NewLine + string.Join(Environment.NewLine, problems) + Environment.NewLine + code);

        return Assembly.Load(stream.ToArray());
    }

    private static async Task<List<string>> RunRuntimeAsync(FlowDocument document)
    {
        var transport = new ScriptedTransport();
        var board = new ESP32Board(transport);
        await board.ConnectAsync();
        var before = transport.SentCommands.Count;

        var profile = BuiltInBoardProfiles.FindById(document.BoardId) ?? BuiltInBoardProfiles.Default;
        await new FlowRuntime(BuiltInBlockCatalog.Create(profile)).ExecuteAsync(document, board);

        return transport.SentCommands.Skip(before).ToList();
    }

    private static async Task<List<string>> RunGeneratedAsync(Assembly assembly, string className)
    {
        var transport = new ScriptedTransport();
        var board = new ESP32Board(transport);
        await board.ConnectAsync();
        var before = transport.SentCommands.Count;

        var type = assembly.GetTypes().Single(t => t.Name == className);
        var run = type.GetMethod("RunAsync")!;
        await (Task)run.Invoke(null, new object[] { board, CancellationToken.None })!;

        return transport.SentCommands.Skip(before).ToList();
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Exported_example_compiles_without_warnings(string fileName)
    {
        var result = FlowCSharpGenerator.Generate(Load(fileName));

        Compile(result.Code);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task Exported_example_sends_the_same_commands_as_the_flow_runtime(string fileName)
    {
        var document = Load(fileName);
        var generated = FlowCSharpGenerator.Generate(document);
        var assembly = Compile(generated.Code);

        var expected = await RunRuntimeAsync(document);
        var actual = await RunGeneratedAsync(assembly, generated.ClassName);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Examples))]
    public void Exported_example_compiles_as_a_console_app_and_in_loop_mode(string fileName)
    {
        var document = Load(fileName);

        Compile(FlowCSharpGenerator.Generate(document, new FlowCSharpOptions { Mode = FlowCSharpMode.ConsoleApp, Loop = true }).Code, console: true);
        Compile(FlowCSharpGenerator.Generate(document, new FlowCSharpOptions { Loop = true, LoopIntervalMs = 250 }).Code);
    }

    [Fact]
    public void The_class_name_and_namespace_can_be_chosen()
    {
        var result = FlowCSharpGenerator.Generate(Load("Example1_Blink.cbflow"), new FlowCSharpOptions { ClassName = "my night-lamp", Namespace = "Acme.Lamps" });

        Assert.Equal("MyNightLamp", result.ClassName);
        Assert.Contains("namespace Acme.Lamps;", result.Code);
        Assert.Contains("public static class MyNightLamp", result.Code);
    }

    [Fact]
    public void Blocks_without_an_export_become_todo_comments_with_a_warning_and_still_compile()
    {
        var document = new FlowDocument { Name = "Sampling" };
        document.Nodes.Add(new FlowNode { Id = "start", Type = BuiltInBlockCatalog.ManualTrigger });
        document.Nodes.Add(new FlowNode { Id = "sample", Type = BuiltInBlockCatalog.SampleChannel, Parameters = { ["pin"] = 34 } });
        document.Connections.Add(new FlowConnection { Id = "c", FromNodeId = "start", FromPort = "trigger", ToNodeId = "sample", ToPort = "trigger" });

        var result = FlowCSharpGenerator.Generate(document);

        Assert.Single(result.Warnings);
        Assert.Contains("// TODO", result.Code);
        Compile(result.Code);
    }

    [Fact]
    public void A_flow_with_errors_is_not_exported()
    {
        var document = new FlowDocument();
        document.Nodes.Add(new FlowNode { Id = "bad", Type = "does.not-exist" });

        var error = Assert.Throws<InvalidOperationException>(() => FlowCSharpGenerator.Generate(document));

        Assert.Contains("cannot be exported", error.Message);
    }

    [Fact]
    public void Arduino_flows_connect_through_the_protocol_board()
    {
        var document = Load("Example1_Blink.cbflow");
        document.BoardId = "arduino-uno";

        var result = FlowCSharpGenerator.Generate(document, new FlowCSharpOptions { Mode = FlowCSharpMode.ConsoleApp });

        Assert.Contains("CodeBridgeProtocolBoard", result.Code);
        Compile(result.Code, console: true);
    }
}
