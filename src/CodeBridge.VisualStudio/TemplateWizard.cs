using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Shell;

namespace CodeBridge.VisualStudio
{
    public class TemplateWizard : IWizard
    {
        private DTE _dte = null!;
        private string _destinationDirectory = null!;
        private string _safeProjectName = null!;

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = (DTE)automationObject;
            _destinationDirectory = replacementsDictionary["$destinationdirectory$"];
            _safeProjectName = replacementsDictionary["$safeprojectname$"];
        }

        public void ProjectFinishedGenerating(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var projectFullName = project.FullName;
                var projectDir = Path.GetDirectoryName(projectFullName) ?? _destinationDirectory;

                // 1. Always ensure Program.cs exists — this is the #1 cause of CS5001.
                EnsureProgramCs(projectDir, _safeProjectName);

                // 2. Generate the starter form.
                var context = new VisualStudioProjectContext(project, projectFullName, _safeProjectName);
                CodeBridgeStarterFormService.AddStarterForm(context);

                // 3. Try to install the NuGet package (best-effort, non-blocking).
                try
                {
                    var manifest = ToolboxManifestLoader.LoadDefault();
                    var packageFeedPath = CodeBridgePackageInstaller.ResolvePackageFeedPath(manifest);

                    if (!string.IsNullOrEmpty(packageFeedPath))
                    {
                        // Run synchronously on the UI thread — we're already on it.
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await CodeBridgePackageInstaller.InstallPackageAsync(
                                projectFullName, manifest.Package, packageFeedPath!);
                        });
                    }
                }
                catch (Exception ex)
                {
                    ActivityLog.LogWarning("CodeBridge", $"Template Wizard NuGet installation warning: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("CodeBridge", $"Template Wizard generation error: {ex.Message}");
            }
        }

        public void RunFinished() { }
        public void ProjectItemFinishedGenerating(ProjectItem projectItem) { }
        public bool ShouldAddProjectItem(string filePath) => true;
        public void BeforeOpeningFile(ProjectItem projectItem) { }

        /// <summary>
        /// Writes Program.cs to disk if it does not already exist.
        /// This guarantees the project always has a valid entry point,
        /// regardless of whether the vstemplate file-copy succeeded.
        /// </summary>
        private static void EnsureProgramCs(string projectDir, string namespaceName)
        {
            var programPath = Path.Combine(projectDir, "Program.cs");
            if (File.Exists(programPath))
                return;

            var code = $@"using System;
using System.Windows.Forms;

namespace {namespaceName}
{{
    internal static class Program
    {{
        [STAThread]
        public static void Main(string[] args)
        {{
            ApplicationConfiguration.Initialize();
            Application.Run(new CodeBridgeStarterForm());
        }}
    }}
}}
";
            File.WriteAllText(programPath, code, Encoding.UTF8);
        }
    }
}
