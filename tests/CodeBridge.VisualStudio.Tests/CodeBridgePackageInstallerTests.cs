using System.IO;
using System.Xml.Linq;
using CodeBridge.VisualStudio;

namespace CodeBridge.VisualStudio.Tests;

public sealed class CodeBridgePackageInstallerTests
{
    [Fact]
    public void GetInstalledPackageVersion_reads_attribute_version()
    {
        using var project = TemporaryProject.Create(
            new XElement(
                "Project",
                new XElement(
                    "ItemGroup",
                    new XElement(
                        "PackageReference",
                        new XAttribute("Include", "CodeBridge.Designer.WinForms"),
                        new XAttribute("Version", "0.1.5-preview.1")))));

        var version = CodeBridgePackageInstaller.GetInstalledPackageVersion(
            project.Path,
            "CodeBridge.Designer.WinForms");

        Assert.Equal("0.1.5-preview.1", version);
    }

    [Fact]
    public void FlowEditorControl_CanBeInstantiated()
    {
        Exception? ex = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var control = new CodeBridge.VisualStudio.Editor.FlowEditorControl();
                var doc = new CodeBridge.Flow.FlowDocument();
                var node1 = new CodeBridge.Flow.FlowNode
                {
                    Id = "node-1",
                    Type = "flow.manual-trigger",
                    Position = new CodeBridge.Flow.FlowPosition(100, 100)
                };
                var node2 = new CodeBridge.Flow.FlowNode
                {
                    Id = "node-2",
                    Type = "gpio.digital-write",
                    Position = new CodeBridge.Flow.FlowPosition(300, 100)
                };
                doc.Nodes.Add(node1);
                doc.Nodes.Add(node2);
                doc.Connections.Add(new CodeBridge.Flow.FlowConnection
                {
                    Id = "conn-1",
                    FromNodeId = "node-1",
                    FromPort = "trigger",
                    ToNodeId = "node-2",
                    ToPort = "trigger"
                });
                control.LoadDocument(doc);
            }
            catch (Exception e)
            {
                ex = e;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (ex != null)
        {
            throw new InvalidOperationException($"FlowEditorControl failed: {ex}");
        }
    }

    [Fact]
    public void FlowEditorControl_CanLoadBlinkFlowTemplate()
    {
        Exception? ex = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ItemTemplates", "CodeBridgeFlow", "BlinkFlow.cbflow");
                if (!File.Exists(templatePath))
                {
                    templatePath = Path.GetFullPath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "..", "..",
                        "src", "CodeBridge.VisualStudio", "ItemTemplates", "CodeBridgeFlow", "BlinkFlow.cbflow"));
                }
                var json = File.ReadAllText(templatePath);
                var doc = System.Text.Json.JsonSerializer.Deserialize<CodeBridge.Flow.FlowDocument>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Assert.NotNull(doc);
                Assert.Equal(5, doc!.Nodes.Count);
                Assert.Equal(4, doc.Connections.Count);

                var control = new CodeBridge.VisualStudio.Editor.FlowEditorControl();
                control.LoadDocument(doc);
            }
            catch (Exception e)
            {
                ex = e;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (ex != null)
        {
            throw new InvalidOperationException($"FlowEditorControl failed on BlinkFlow: {ex}");
        }
    }

    [Fact]
    public void GetInstalledPackageVersion_reads_child_version_element()
    {
        using var project = TemporaryProject.Create(
            new XElement(
                "Project",
                new XElement(
                    "ItemGroup",
                    new XElement(
                        "PackageReference",
                        new XAttribute("Include", "CodeBridge.Designer.WinForms"),
                        new XElement("Version", "0.1.4-preview.1")))));

        var version = CodeBridgePackageInstaller.GetInstalledPackageVersion(
            project.Path,
            "CodeBridge.Designer.WinForms");

        Assert.Equal("0.1.4-preview.1", version);
    }

    [Fact]
    public void GetInstalledPackageVersion_returns_null_when_package_is_missing()
    {
        using var project = TemporaryProject.Create(
            new XElement(
                "Project",
                new XElement(
                    "ItemGroup",
                    new XElement(
                        "PackageReference",
                        new XAttribute("Include", "Other.Package"),
                        new XAttribute("Version", "1.0.0")))));

        var version = CodeBridgePackageInstaller.GetInstalledPackageVersion(
            project.Path,
            "CodeBridge.Designer.WinForms");

        Assert.Null(version);
    }

    [Fact]
    public async Task RemovePackageAsync_returns_not_installed_without_calling_dotnet()
    {
        using var project = TemporaryProject.Create(new XElement("Project"));
        var package = new ToolboxPackageInfo
        {
            Id = "CodeBridge.Designer.WinForms",
            Version = "0.1.5-preview.1"
        };

        var result = await CodeBridgePackageInstaller.RemovePackageAsync(project.Path, package);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(PackageInstallAction.NotInstalled, result.Action);
        Assert.Null(result.PreviousVersion);
        Assert.Equal("0.1.5-preview.1", result.TargetVersion);
    }

    private sealed class TemporaryProject : IDisposable
    {
        private TemporaryProject(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryProject Create(XElement project)
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CodeBridge.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "TestProject.csproj");
            new XDocument(project).Save(path);
            return new TemporaryProject(path);
        }

        public void Dispose()
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
