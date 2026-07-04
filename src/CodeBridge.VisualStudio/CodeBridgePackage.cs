using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("CodeBridge", "Visual flows and ESP32 toolbox integration for WinForms.", "0.1.14-preview.1")]
[ProvideMenuResource("CodeBridgeCommands.CTMENU", 1)]
[Guid(PackageGuids.PackageGuidString)]
public sealed class CodeBridgePackage : AsyncPackage
{
    protected override async System.Threading.Tasks.Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        await OpenCodeBridgeSetupCommand.InitializeAsync(this);
        await InstallCodeBridgeWinFormsPackageCommand.InitializeAsync(this);
        await AddCodeBridgeStarterFormCommand.InitializeAsync(this);
    }
}
