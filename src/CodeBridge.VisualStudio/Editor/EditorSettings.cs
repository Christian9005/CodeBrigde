#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>Per-user editor preferences (last used port) stored in %LOCALAPPDATA%\CodeBridge\editor-settings.json.</summary>
    internal sealed class EditorSettings
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeBridge",
            "editor-settings.json");

        public string? LastPort { get; set; }

        public static EditorSettings Load()
        {
            try
            {
                if (File.Exists(FilePath) &&
                    new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath)) is Dictionary<string, object> map)
                {
                    return new EditorSettings { LastPort = map.TryGetValue("lastPort", out var port) ? port as string : null };
                }
            }
            catch (Exception)
            {
                // A corrupt settings file must never break the editor.
            }

            return new EditorSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(new Dictionary<string, object?> { ["lastPort"] = LastPort }));
            }
            catch (Exception)
            {
                // Read-only profile: preferences simply are not remembered.
            }
        }
    }
}
