using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CodeBridge.Flow;
using CodeBridge.VisualStudio.Editor;

namespace CodeBridge.VisualStudio.Tests;

public sealed class ExportTests
{
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                // What the Visual Studio UI thread has: continuations of async UI code resume on this thread.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
                action();
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException(failure.ToString());
    }

    /// <summary>Lets continuations of an async UI method run on this thread while waiting for it.</summary>
    private static void Await(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
            Thread.Sleep(10);
        }

        task.GetAwaiter().GetResult();
    }

    private static FlowDocument Blink()
    {
        var document = new FlowDocument { Name = "Blink", BoardId = "esp32-devkit" };
        document.Nodes.Add(new FlowNode { Id = "start", Type = "flow.manual-trigger" });
        document.Nodes.Add(new FlowNode { Id = "blink", Type = "gpio.blink-led", Parameters = { ["pin"] = 2, ["durationMs"] = 500 } });
        document.Connections.Add(new FlowConnection { Id = "c", FromNodeId = "start", FromPort = "trigger", ToNodeId = "blink", ToPort = "trigger" });
        return document;
    }

    [Fact]
    public void Export_as_class_raises_the_generated_code_named_after_the_flow_file()
    {
        RunSta(() =>
        {
            var control = new FlowEditorControl { FlowFilePath = @"C:\work\Lamp.cbflow", NamespaceProvider = () => "Acme.Desk" };
            control.LoadDocument(Blink());
            ExportedCode? exported = null;
            control.CodeExported += code => exported = code;

            Await(control.ExportAsync(ExportAction.AddClassToProject));

            Assert.NotNull(exported);
            Assert.Equal("LampFlow.cs", exported!.FileName);
            Assert.False(exported.IsConsoleProgram);
            Assert.Contains("namespace Acme.Desk;", exported.Code);
            Assert.Contains("public static class LampFlow", exported.Code);
            Assert.Contains("PinMode.Output", exported.Code);
        });
    }

    [Fact]
    public void Export_as_console_app_produces_a_program_file()
    {
        RunSta(() =>
        {
            var control = new FlowEditorControl();
            control.LoadDocument(Blink());
            ExportedCode? exported = null;
            control.CodeExported += code => exported = code;

            Await(control.ExportAsync(ExportAction.AddConsoleProgram));

            Assert.NotNull(exported);
            Assert.Equal("Program.cs", exported!.FileName);
            Assert.True(exported.IsConsoleProgram);
            Assert.Contains("BoardDiscovery.DiscoverPorts()", exported.Code);
        });
    }

    [Fact]
    public void A_flow_with_errors_is_not_exported()
    {
        RunSta(() =>
        {
            var document = new FlowDocument();
            document.Nodes.Add(new FlowNode { Id = "bad", Type = "does.not-exist" });
            var control = new FlowEditorControl();
            control.LoadDocument(document);
            var raised = false;
            control.CodeExported += _ => raised = true;

            Await(control.ExportAsync(ExportAction.AddClassToProject));

            Assert.False(raised);
        });
    }
}
