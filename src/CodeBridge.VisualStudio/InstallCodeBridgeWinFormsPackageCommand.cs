using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeBridge.VisualStudio;

internal sealed class InstallCodeBridgeWinFormsPackageCommand
{
    private readonly AsyncPackage _package;

    private InstallCodeBridgeWinFormsPackageCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;

        var commandId = new CommandID(PackageGuids.CommandSet, PackageIds.InstallWinFormsPackageCommandId);
        var command = new MenuCommand(Execute, commandId);
        commandService.AddCommand(command);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService is not null)
            _ = new InstallCodeBridgeWinFormsPackageCommand(package, commandService);
    }

    private void Execute(object sender, EventArgs e)
    {
        _package.JoinableTaskFactory.Run(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        try
        {
            var manifest = ToolboxManifestLoader.LoadDefault();
            var context = await VisualStudioProjectLocator.GetActiveCSharpProjectAsync(_package);
            if (context is null)
            {
                ShowMessage(
                    "Select a WinForms C# project or open one of its files before installing the CodeBridge package.",
                    OLEMSGICON.OLEMSGICON_WARNING);
                return;
            }

            var packageFeedPath = CodeBridgePackageInstaller.ResolvePackageFeedPath(manifest);
            if (string.IsNullOrWhiteSpace(packageFeedPath))
            {
                ShowMessage(
                    "CodeBridge could not find the packaged local NuGet feed.\n\n" +
                    "Please verify your CodeBridge installation or set the CODEBRIDGE_NUGET_FEED environment variable to a directory containing the CodeBridge packages.",
                    OLEMSGICON.OLEMSGICON_WARNING);
                return;
            }

            var result = await CodeBridgePackageInstaller.InstallPackageAsync(context.ProjectPath, manifest.Package, packageFeedPath!);
            var message = CreateResultMessage(context.ProjectPath, packageFeedPath!, result);
            ShowMessage(message, result.ExitCode == 0 ? OLEMSGICON.OLEMSGICON_INFO : OLEMSGICON.OLEMSGICON_CRITICAL);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }
    }

    private static string CreateResultMessage(string projectPath, string packageFeedPath, CommandResult result)
    {
        if (result.Action == PackageInstallAction.AlreadyCurrent)
        {
            return $"CodeBridge.Designer.WinForms {result.TargetVersion} is already installed in this project, so nothing changed." +
                   Environment.NewLine + Environment.NewLine +
                   "If the Toolbox items are missing, rebuild the project and reopen the designer." +
                   Environment.NewLine + Environment.NewLine + $"Project: {projectPath}";
        }

        var builder = new StringBuilder();
        if (result.ExitCode == 0)
        {
            builder.AppendLine(CreatePackageResultTitle(result));
            builder.AppendLine();
            builder.AppendLine("Next steps:");
            builder.AppendLine("1. Rebuild the WinForms project.");
            builder.AppendLine("2. Close and reopen the designer surface.");
            builder.AppendLine("3. Check the Toolbox category for CodeBridge Flow and CodeBridge Board.");
        }
        else
        {
            builder.AppendLine(CreatePackageResultTitle(result));
        }

        builder.AppendLine();
        builder.AppendLine($"Project: {projectPath}");
        builder.AppendLine($"Installed version: {result.PreviousVersion ?? "(none)"}");
        builder.AppendLine($"VSIX package version: {result.TargetVersion ?? "(unknown)"}");
        builder.AppendLine($"Package feed: {packageFeedPath}");
        builder.AppendLine($"dotnet exit code: {result.ExitCode}");

        var details = string.Join(
            Environment.NewLine,
            new[] { result.Output.Trim(), result.Error.Trim() }.Where(value => value.Length > 0));
        if (details.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine(details.Length > 2500 ? details.Substring(0, 2500) : details);
        }

        return builder.ToString();
    }

    private static string CreatePackageResultTitle(CommandResult result) =>
        result.Action switch
        {
            PackageInstallAction.Installed => "CodeBridge.Designer.WinForms was installed in the project.",
            PackageInstallAction.Updated => "CodeBridge.Designer.WinForms was updated in the project.",
            PackageInstallAction.AlreadyCurrent => "CodeBridge.Designer.WinForms is already current.",
            PackageInstallAction.NewerInstalled => "The project already has a newer CodeBridge.Designer.WinForms package.",
            PackageInstallAction.UpdateFailed => "CodeBridge could not update the WinForms package.",
            PackageInstallAction.InstallFailed => "CodeBridge could not install the WinForms package.",
            _ => result.ExitCode == 0
                ? "CodeBridge.Designer.WinForms package operation completed."
                : "CodeBridge package operation failed."
        };

    private void ShowMessage(string message, OLEMSGICON icon)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        VsShellUtilities.ShowMessageBox(
            _package,
            message,
            "CodeBridge",
            icon,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

}
