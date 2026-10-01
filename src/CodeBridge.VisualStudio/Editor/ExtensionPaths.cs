#nullable enable
using System;
using System.IO;
using System.Reflection;

namespace CodeBridge.VisualStudio.Editor
{
    internal static class ExtensionPaths
    {
        /// <summary>
        /// Folder the extension assembly was installed to. Uses the original location (CodeBase) so it keeps working
        /// when the assembly is shadow-copied, for example by a test runner.
        /// </summary>
        public static string Directory
        {
            get
            {
                var assembly = typeof(ExtensionPaths).Assembly;
                try
                {
                    var codeBase = assembly.CodeBase;
                    if (!string.IsNullOrEmpty(codeBase) && Uri.TryCreate(codeBase, UriKind.Absolute, out var uri) && uri.IsFile)
                        return Path.GetDirectoryName(uri.LocalPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                }
                catch (Exception)
                {
                    // fall through
                }

                return Path.GetDirectoryName(assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }
}
