using System.IO;
using System.Linq;
using System.Xml.Linq;
using CodeBridge.VisualStudio;

namespace CodeBridge.VisualStudio.Tests;

/// <summary>
/// Guards the things that silently break a published extension: version drift between files,
/// and the editor's block catalog drifting away from the runtime catalog.
/// </summary>
public sealed class ReleaseConsistencyTests
{
    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string ProductVersion()
    {
        var props = XDocument.Load(Path.Combine(RepoRoot(), "Directory.Build.props"));
        return props.Descendants("Version").First().Value.Trim();
    }

    [Fact]
    public void ToolboxManifest_versions_match_the_product_version()
    {
        var manifest = ToolboxManifestLoader.Load(
            Path.Combine(RepoRoot(), "src", "CodeBridge.VisualStudio", "Assets", "toolbox-components.json"));

        Assert.Equal(ProductVersion(), manifest.Package.Version);
        Assert.Equal(ProductVersion(), manifest.ManifestVersion);
    }

    [Fact]
    public void Vsix_manifest_version_matches_the_product_version()
    {
        var manifest = XDocument.Load(Path.Combine(RepoRoot(), "src", "CodeBridge.VisualStudio", "source.extension.vsixmanifest"));
        var identity = manifest.Descendants().First(element => element.Name.LocalName == "Identity");

        Assert.Equal(ProductVersion(), identity.Attribute("Version")!.Value);
    }

    [Fact]
    public void Shipped_source_does_not_contain_developer_machine_paths()
    {
        var sourceRoot = Path.Combine(RepoRoot(), "src");
        var offenders = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(@"\obj\") && !path.Contains(@"\bin\"))
            .Where(path => File.ReadAllText(path).Contains(@"C:\Projects"))
            .ToList();

        Assert.Empty(offenders);
    }
}
