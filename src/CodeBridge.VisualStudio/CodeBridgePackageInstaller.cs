using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CodeBridge.VisualStudio;

internal static class CodeBridgePackageInstaller
{
    public static string? ResolvePackageFeedPath(ToolboxManifest manifest)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        var vsixFeed = Path.Combine(
            assemblyDirectory,
            string.IsNullOrWhiteSpace(manifest.Package.VsixPackageFeed)
                ? "Packages"
                : manifest.Package.VsixPackageFeed);
        if (HasPackage(vsixFeed, manifest.Package.Id))
            return vsixFeed;

        var environmentFeed = Environment.GetEnvironmentVariable("CODEBRIDGE_NUGET_FEED");
        if (!string.IsNullOrWhiteSpace(environmentFeed) && HasPackage(environmentFeed, manifest.Package.Id))
            return environmentFeed;

        var localFeed = Path.Combine(@"C:\Projects\CodeBridge", manifest.Package.LocalFeedHint);
        if (HasPackage(localFeed, manifest.Package.Id))
            return localFeed;

        return null;
    }

    public static async Task<CommandResult> InstallPackageAsync(
        string projectPath,
        ToolboxPackageInfo package,
        string packageFeedPath)
    {
        var currentVersion = GetInstalledPackageVersion(projectPath, package.Id);
        if (currentVersion is { Length: > 0 })
        {
            var comparison = ComparePackageVersions(currentVersion, package.Version);
            if (comparison == 0)
            {
                return new CommandResult(
                    0,
                    $"{package.Id} is already installed at {package.Version}.",
                    string.Empty,
                    PackageInstallAction.AlreadyCurrent,
                    currentVersion,
                    package.Version);
            }

            if (comparison > 0)
            {
                return new CommandResult(
                    0,
                    $"{package.Id} {currentVersion} is newer than the VSIX packaged version {package.Version}.",
                    string.Empty,
                    PackageInstallAction.NewerInstalled,
                    currentVersion,
                    package.Version);
            }

            var removeBeforeUpdate = await RunDotnetAsync(
                projectPath,
                $"remove {Quote(projectPath)} package {package.Id}");
            if (removeBeforeUpdate.ExitCode != 0)
            {
                return new CommandResult(
                    removeBeforeUpdate.ExitCode,
                    removeBeforeUpdate.Output,
                    removeBeforeUpdate.Error,
                    PackageInstallAction.UpdateFailed,
                    currentVersion,
                    package.Version);
            }

            var update = await AddPackageAsync(projectPath, package, packageFeedPath);
            return new CommandResult(
                update.ExitCode,
                CombineOutput(removeBeforeUpdate.Output, update.Output),
                CombineOutput(removeBeforeUpdate.Error, update.Error),
                update.ExitCode == 0 ? PackageInstallAction.Updated : PackageInstallAction.UpdateFailed,
                currentVersion,
                package.Version);
        }

        var install = await AddPackageAsync(projectPath, package, packageFeedPath);
        return new CommandResult(
            install.ExitCode,
            install.Output,
            install.Error,
            install.ExitCode == 0 ? PackageInstallAction.Installed : PackageInstallAction.InstallFailed,
            currentVersion,
            package.Version);
    }

    public static Task<CommandResult> RemovePackageAsync(string projectPath, ToolboxPackageInfo package)
    {
        var currentVersion = GetInstalledPackageVersion(projectPath, package.Id);
        if (currentVersion is not { Length: > 0 })
        {
            return Task.FromResult(new CommandResult(
                0,
                $"{package.Id} is not installed in this project.",
                string.Empty,
                PackageInstallAction.NotInstalled,
                null,
                package.Version));
        }

        return RemoveInstalledPackageAsync(projectPath, package, currentVersion);
    }

    public static string? GetInstalledPackageVersion(string projectPath, string packageId)
    {
        if (!File.Exists(projectPath))
            return null;

        try
        {
            var document = XDocument.Load(projectPath);
            var packageReference = document
                .Descendants()
                .FirstOrDefault(element =>
                    string.Equals(element.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value,
                        packageId,
                        StringComparison.OrdinalIgnoreCase));

            if (packageReference is null)
                return null;

            var attributeVersion = packageReference.Attribute("Version")?.Value;
            if (attributeVersion?.Trim() is { Length: > 0 } trimmedAttributeVersion)
                return trimmedAttributeVersion;

            var elementVersion = packageReference
                .Elements()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim();

            return elementVersion is { Length: > 0 } ? elementVersion : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool HasPackage(string directory, string packageId)
    {
        return Directory.Exists(directory) &&
            Directory.EnumerateFiles(directory, $"{packageId}.*.nupkg", SearchOption.TopDirectoryOnly).Any();
    }

    private static Task<CommandResult> AddPackageAsync(
        string projectPath,
        ToolboxPackageInfo package,
        string packageFeedPath)
    {
        return RunDotnetAsync(
            projectPath,
            $"add {Quote(projectPath)} package {package.Id} --version {package.Version} --source {Quote(packageFeedPath)}");
    }

    private static async Task<CommandResult> RemoveInstalledPackageAsync(
        string projectPath,
        ToolboxPackageInfo package,
        string currentVersion)
    {
        var result = await RunDotnetAsync(
            projectPath,
            $"remove {Quote(projectPath)} package {package.Id}");

        return new CommandResult(
            result.ExitCode,
            result.Output,
            result.Error,
            result.ExitCode == 0 ? PackageInstallAction.Removed : PackageInstallAction.RemoveFailed,
            currentVersion,
            package.Version);
    }

    private static async Task<CommandResult> RunDotnetAsync(string projectPath, string arguments)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.Run(() => process.WaitForExit());

        return new CommandResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static int ComparePackageVersions(string currentVersion, string targetVersion)
    {
        var current = ParsePackageVersion(currentVersion);
        var target = ParsePackageVersion(targetVersion);

        for (var i = 0; i < Math.Max(current.Numbers.Length, target.Numbers.Length); i++)
        {
            var left = i < current.Numbers.Length ? current.Numbers[i] : 0;
            var right = i < target.Numbers.Length ? target.Numbers[i] : 0;
            if (left != right)
                return left.CompareTo(right);
        }

        if (string.IsNullOrWhiteSpace(current.Prerelease) && !string.IsNullOrWhiteSpace(target.Prerelease))
            return 1;
        if (!string.IsNullOrWhiteSpace(current.Prerelease) && string.IsNullOrWhiteSpace(target.Prerelease))
            return -1;

        return string.Compare(current.Prerelease, target.Prerelease, StringComparison.OrdinalIgnoreCase);
    }

    private static ParsedPackageVersion ParsePackageVersion(string version)
    {
        var dashIndex = version.IndexOf('-');
        var numericPart = dashIndex >= 0 ? version.Substring(0, dashIndex) : version;
        var prerelease = dashIndex >= 0 ? version.Substring(dashIndex + 1).Trim() : string.Empty;
        var numbers = numericPart
            .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part.Trim(), out var value) ? value : 0)
            .ToArray();

        return new ParsedPackageVersion(numbers, prerelease);
    }

    private static string CombineOutput(string first, string second) =>
        string.Join(
            Environment.NewLine,
            new[] { first.Trim(), second.Trim() }.Where(value => value.Length > 0));

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private readonly struct ParsedPackageVersion
    {
        public ParsedPackageVersion(int[] numbers, string prerelease)
        {
            Numbers = numbers;
            Prerelease = prerelease;
        }

        public int[] Numbers { get; }

        public string Prerelease { get; }
    }
}

internal enum PackageInstallAction
{
    Unknown,
    Installed,
    Updated,
    AlreadyCurrent,
    NewerInstalled,
    Removed,
    NotInstalled,
    InstallFailed,
    UpdateFailed,
    RemoveFailed
}

internal sealed class CommandResult
{
    public CommandResult(
        int exitCode,
        string output,
        string error,
        PackageInstallAction action = PackageInstallAction.Unknown,
        string? previousVersion = null,
        string? targetVersion = null)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
        Action = action;
        PreviousVersion = previousVersion;
        TargetVersion = targetVersion;
    }

    public int ExitCode { get; }

    public string Output { get; }

    public string Error { get; }

    public PackageInstallAction Action { get; }

    public string? PreviousVersion { get; }

    public string? TargetVersion { get; }
}
