using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;

namespace CodeBridge.VisualStudio;

internal static class ToolboxManifestLoader
{
    private const string ManifestRelativePath = "Assets\\toolbox-components.json";

    public static ToolboxManifest LoadDefault()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppDomain.CurrentDomain.BaseDirectory;
        var manifestPath = Path.Combine(assemblyDirectory, ManifestRelativePath);
        return Load(manifestPath);
    }

    public static ToolboxManifest Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            throw new ToolboxManifestException($"Toolbox manifest was not found: {manifestPath}");

        var json = File.ReadAllText(manifestPath);
        var serializer = new JavaScriptSerializer();
        var manifest = serializer.Deserialize<ToolboxManifest>(json)
            ?? throw new ToolboxManifestException("Toolbox manifest is empty or invalid.");

        Validate(manifest);
        return manifest;
    }

    private static void Validate(ToolboxManifest manifest)
    {
        Require(manifest.SchemaVersion, "schemaVersion");
        Require(manifest.ManifestVersion, "manifestVersion");
        Require(manifest.ExtensionId, "extensionId");
        Require(manifest.Package.Id, "package.id");
        Require(manifest.Package.Version, "package.version");
        Require(manifest.Package.AssemblyName, "package.assemblyName");
        Require(manifest.Package.TargetFramework, "package.targetFramework");
        Require(manifest.Toolbox.Category, "toolbox.category");
        Require(manifest.Toolbox.Designer, "toolbox.designer");
        Require(manifest.Toolbox.RegistrationMode, "toolbox.registrationMode");

        if (manifest.Components.Count == 0)
            throw new ToolboxManifestException("Toolbox manifest must define at least one component.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in manifest.Components)
        {
            Require(component.Id, "components[].id");
            Require(component.Kind, $"components[{component.Id}].kind");
            Require(component.TypeName, $"components[{component.Id}].typeName");
            Require(component.AssemblyName, $"components[{component.Id}].assemblyName");
            Require(component.DisplayName, $"components[{component.Id}].displayName");
            Require(component.Category, $"components[{component.Id}].category");

            if (!ids.Add(component.Id))
                throw new ToolboxManifestException($"Duplicate toolbox component id '{component.Id}'.");

            if (!string.Equals(component.Kind, "Control", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(component.Kind, "ComponentTray", StringComparison.OrdinalIgnoreCase))
            {
                throw new ToolboxManifestException(
                    $"Component '{component.Id}' has unsupported kind '{component.Kind}'.");
            }
        }

        var duplicateTypes = manifest.Components
            .GroupBy(component => component.TypeName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTypes is not null)
            throw new ToolboxManifestException($"Duplicate toolbox type '{duplicateTypes.Key}'.");
    }

    private static void Require(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ToolboxManifestException($"Toolbox manifest field '{field}' is required.");
    }
}

internal sealed class ToolboxManifestException : Exception
{
    public ToolboxManifestException(string message)
        : base(message)
    {
    }
}
