using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeBridge.VisualStudio;

internal sealed class VisualStudioProjectContext
{
    public VisualStudioProjectContext(Project project, string projectPath, string defaultNamespace)
    {
        Project = project;
        ProjectPath = projectPath;
        ProjectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        DefaultNamespace = defaultNamespace;
    }

    public Project Project { get; }

    public string ProjectPath { get; }

    public string ProjectDirectory { get; }

    public string DefaultNamespace { get; }
}

internal static class VisualStudioProjectLocator
{
    public static async Task<VisualStudioProjectContext?> GetActiveCSharpProjectAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await package.GetServiceAsync(typeof(SDTE)) as DTE2;
        if (dte is null)
            return null;

        var selectedProject = TryGetSelectedProject(dte);
        var activeProject = dte.ActiveDocument?.ProjectItem?.ContainingProject;
        var project = new[] { selectedProject, activeProject }
            .FirstOrDefault(item => GetProjectPath(item) is not null);

        var projectPath = GetProjectPath(project);
        if (project is null || projectPath is null)
            return null;

        return new VisualStudioProjectContext(
            project,
            projectPath,
            GetDefaultNamespace(project, projectPath));
    }

    private static Project? TryGetSelectedProject(DTE2 dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (SelectedItem selectedItem in dte.SelectedItems)
        {
            if (selectedItem.Project is not null)
                return selectedItem.Project;

            if (selectedItem.ProjectItem?.ContainingProject is not null)
                return selectedItem.ProjectItem.ContainingProject;
        }

        return null;
    }

    private static string? GetProjectPath(Project? project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var path = project?.FullName;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path!.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? path!
            : null;
    }

    private static string GetDefaultNamespace(Project project, string projectPath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (var propertyName in new[] { "DefaultNamespace", "RootNamespace" })
        {
            try
            {
                var value = project.Properties?.Item(propertyName)?.Value as string;
                if (!string.IsNullOrWhiteSpace(value))
                    return SanitizeNamespace(value!);
            }
            catch
            {
                // Some project systems do not expose all DTE properties.
            }
        }

        return SanitizeNamespace(Path.GetFileNameWithoutExtension(projectPath));
    }

    private static string SanitizeNamespace(string value)
    {
        var parts = value
            .Split(new[] { '.', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeIdentifier)
            .Where(part => part.Length > 0);

        var namespaceName = string.Join(".", parts);
        return namespaceName.Length == 0 ? "CodeBridgeStarter" : namespaceName;
    }

    private static string SanitizeIdentifier(string value)
    {
        var chars = value
            .Select((ch, index) => char.IsLetter(ch) || ch == '_' || (index > 0 && char.IsDigit(ch)) ? ch : '_')
            .ToArray();

        return new string(chars).Trim('_');
    }
}
