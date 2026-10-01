using CodeBridge.Flow;
using CodeBridge.Flow.Serialization;
using CodeBridge.Flow.Validation;

namespace CodeBridge.Core.Tests.Flow;

/// <summary>The example flows shipped as Visual Studio item templates must always be valid for the runtime.</summary>
public class ExampleFlowTemplateTests
{
    private static string TemplatesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "CodeBridge.VisualStudio", "ItemTemplates");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("ItemTemplates folder not found.");
    }

    public static IEnumerable<object[]> ExampleFiles() =>
        Directory.EnumerateFiles(TemplatesDirectory(), "*.cbflow", SearchOption.AllDirectories)
            .Select(path => new object[] { Path.GetRelativePath(TemplatesDirectory(), path) });

    [Fact]
    public void There_are_at_least_five_numbered_examples()
    {
        var examples = Directory.GetDirectories(TemplatesDirectory(), "CodeBridgeExample*");

        Assert.True(examples.Length >= 5, $"Found {examples.Length} example templates.");
    }

    [Theory]
    [MemberData(nameof(ExampleFiles))]
    public void Example_flow_is_valid_for_the_esp32_catalog(string relativePath)
    {
        var json = File.ReadAllText(Path.Combine(TemplatesDirectory(), relativePath));
        var document = FlowDocumentJson.Deserialize(json);
        var profile = BuiltInBoardProfiles.FindById(document.BoardId) ?? BuiltInBoardProfiles.Default;

        var result = new FlowValidator().Validate(document, BuiltInBlockCatalog.Create(profile));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Empty(result.Warnings);
        Assert.NotEmpty(document.Nodes);
    }

    [Theory]
    [MemberData(nameof(ExampleFiles))]
    public void Every_template_folder_points_at_an_existing_flow_file(string relativePath)
    {
        var folder = Path.GetDirectoryName(Path.Combine(TemplatesDirectory(), relativePath))!;
        var template = Directory.GetFiles(folder, "*.vstemplate").Single();

        Assert.Contains(Path.GetFileName(relativePath), File.ReadAllText(template));
    }
}
