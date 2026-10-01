#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>
    /// Writes to a "CodeBridge" pane of the Visual Studio Output window, so board, upload and run messages live in the
    /// same place as build and debug output. Returns false outside Visual Studio (unit tests), where the editor falls
    /// back to its own output panel.
    /// </summary>
    internal static class VsOutput
    {
        private static readonly Guid PaneId = new Guid("5c3f7d2e-8a41-4b6e-9d0a-2f6e1b7c9a10");
        private static IVsOutputWindowPane? _pane;
        private static bool _unavailable;

        public static bool TryWrite(string text)
        {
            return Run(() => WriteCore(text, show: false));
        }

        /// <summary>Selects the CodeBridge pane and shows the Output tool window.</summary>
        public static bool TryShow()
        {
            return Run(() => WriteCore(string.Empty, show: true));
        }

        private static bool Run(Func<bool> action)
        {
            if (_unavailable)
                return false;

            try
            {
                return action();
            }
            catch (Exception ex) when (ex is IOException || ex is TypeLoadException || ex is InvalidOperationException || ex is COMException)
            {
                _unavailable = true;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool WriteCore(string text, bool show)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_pane == null)
            {
                if (!(Package.GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow window))
                    return false;

                var id = PaneId;
                window.CreatePane(ref id, "CodeBridge", 1, 0);
                window.GetPane(ref id, out _pane);
            }

            if (_pane == null)
                return false;

            if (text.Length > 0)
                _pane.OutputStringThreadSafe(text);

            if (show)
            {
                _pane.Activate();
                if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell shell)
                {
                    var output = VSConstants.StandardToolWindows.Output;
                    if (ErrorHandler.Succeeded(shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref output, out var frame)))
                        frame?.Show();
                }
            }

            return true;
        }
    }
}
