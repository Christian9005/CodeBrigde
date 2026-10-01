#nullable enable
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CodeBridge.VisualStudio.Tour
{
    /// <summary>The CodeBridge Tour, opened as a document tab (Tools > CodeBridge Tour).</summary>
    [Guid("0d5b8a3e-6f2c-4c1e-8f43-3c5a7a1e9b52")]
    public sealed class TourToolWindow : ToolWindowPane
    {
        private readonly TourControl _control;

        public TourToolWindow() : base(null)
        {
            Caption = "CodeBridge Tour";
            _control = new TourControl();
            Content = _control;
        }

        protected override void Initialize()
        {
            base.Initialize();
            _control.OpenExampleRequested += chapter =>
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(() => OpenExampleAsync(chapter));
            _control.OpenCodeRequested += (name, code) =>
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(() => OpenCodeAsync(name, code));
        }

        /// <summary>Copies the example flow into the active project (or Documents\CodeBridge Examples) and opens it.</summary>
        private async Task OpenExampleAsync(TourChapter chapter)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var source = TourContent.ExamplePath(chapter);
            if (source == null || !File.Exists(source))
            {
                ShowMessage("This example is not available in this installation. Reinstall CodeBridge Visual Studio Tools.");
                return;
            }

            try
            {
                var asyncPackage = (AsyncPackage)Package;
                var context = await VisualStudioProjectLocator.GetActiveCSharpProjectAsync(asyncPackage);
                var directory = context?.ProjectDirectory
                                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CodeBridge Examples");
                Directory.CreateDirectory(directory);

                var target = TourContent.UniquePath(directory, Path.GetFileNameWithoutExtension(source), ".cbflow");
                File.Copy(source, target);

                if (context != null)
                {
                    try
                    {
                        // Old-style projects need the file added; SDK-style projects already include it by globbing.
                        context.Project.ProjectItems.AddFromFile(target);
                    }
                    catch (Exception)
                    {
                    }
                }

                OpenFile(target);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ShowMessage("Could not create the example file: " + ex.Message);
            }
        }

        /// <summary>Writes the sample to a temp file and opens it so it is read with Visual Studio's C# colouring.</summary>
        private async Task OpenCodeAsync(string fileName, string code)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var directory = Path.Combine(Path.GetTempPath(), "CodeBridge", "Tour");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, fileName);
                File.WriteAllText(path, code);
                OpenFile(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ShowMessage("Could not open the sample: " + ex.Message);
            }
        }

        private void OpenFile(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = GetService(typeof(SDTE)) as DTE;
            dte?.ItemOperations.OpenFile(path);
        }

        private void ShowMessage(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VsShellUtilities.ShowMessageBox(this, text, "CodeBridge Tour", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    internal sealed class OpenTourCommand
    {
        private readonly AsyncPackage _package;

        private OpenTourCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            commandService.AddCommand(new MenuCommand(Execute, new CommandID(PackageGuids.CommandSet, PackageIds.OpenTourCommandId)));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
                _ = new OpenTourCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await _package.ShowToolWindowAsync(typeof(TourToolWindow), 0, true, _package.DisposalToken);
            });
        }
    }

    /// <summary>Lets code that has no reference to the package (the flow editor) open the tour.</summary>
    internal static class TourLauncher
    {
        public static bool TryOpen()
        {
            try
            {
                return OpenCore();
            }
            catch (Exception ex) when (ex is IOException || ex is TypeLoadException || ex is InvalidOperationException || ex is COMException)
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool OpenCore()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell shell))
                return false;

            var group = PackageGuids.CommandSet;
            object? argument = null;
            return ErrorHandler.Succeeded(shell.PostExecCommand(ref group, (uint)PackageIds.OpenTourCommandId, 0, ref argument));
        }
    }
}
