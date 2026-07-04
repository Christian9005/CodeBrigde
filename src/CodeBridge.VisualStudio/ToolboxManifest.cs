using System.Collections.Generic;

namespace CodeBridge.VisualStudio;

internal sealed class ToolboxManifest
{
    public string SchemaVersion { get; set; } = string.Empty;

    public string ManifestVersion { get; set; } = string.Empty;

    public string ExtensionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ToolboxPackageInfo Package { get; set; } = new();

    public ToolboxRegistrationInfo Toolbox { get; set; } = new();

    public List<ToolboxComponentInfo> Components { get; set; } = new();

    public List<ToolboxSampleInfo> Samples { get; set; } = new();
}

internal sealed class ToolboxPackageInfo
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string AssemblyName { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public string LocalFeedHint { get; set; } = string.Empty;

    public string VsixPackageFeed { get; set; } = string.Empty;

    public string PreferredInstallMode { get; set; } = string.Empty;

    public List<string> RequiredPackages { get; set; } = new();
}

internal sealed class ToolboxRegistrationInfo
{
    public string Category { get; set; } = string.Empty;

    public string Designer { get; set; } = string.Empty;

    public string MinimumVisualStudioVersion { get; set; } = string.Empty;

    public bool AutoRegister { get; set; }

    public string RegistrationMode { get; set; } = string.Empty;

    public string FallbackInstruction { get; set; } = string.Empty;
}

internal sealed class ToolboxComponentInfo
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public string AssemblyName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ToolboxItemName { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool IsVisible { get; set; }

    public ToolboxDefaultSize? DefaultSize { get; set; }

    public string DefaultProperty { get; set; } = string.Empty;

    public string DefaultEvent { get; set; } = string.Empty;

    public string DesignerTypeName { get; set; } = string.Empty;

    public List<string> SmartTags { get; set; } = new();

    public List<ToolboxRecommendedProperty> RecommendedProperties { get; set; } = new();

    public List<string> Events { get; set; } = new();

    public List<string> RuntimeApi { get; set; } = new();
}

internal sealed class ToolboxDefaultSize
{
    public int Width { get; set; }

    public int Height { get; set; }
}

internal sealed class ToolboxRecommendedProperty
{
    public string Name { get; set; } = string.Empty;

    public object? Value { get; set; }

    public string DisplayValue => Value switch
    {
        null => "<null>",
        bool boolean => boolean ? "true" : "false",
        _ => Value.ToString() ?? string.Empty
    };
}

internal sealed class ToolboxSampleInfo
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
