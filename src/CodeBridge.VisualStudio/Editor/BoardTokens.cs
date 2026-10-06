#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace CodeBridge.VisualStudio.Editor
{
    /// <summary>
    /// Pairing tokens of the Wi-Fi boards this user has set up, keyed by IP address or host name.
    /// Stored encrypted with the Windows user's DPAPI key (%LOCALAPPDATA%\CodeBridge\board-tokens.json), so the file is
    /// useless on any other account or machine.
    /// </summary>
    internal static class BoardTokens
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CodeBridge.BoardTokens.v1");

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodeBridge",
            "board-tokens.json");

        public static string? Get(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return null;

            try
            {
                var map = Read();
                if (!map.TryGetValue(Normalize(host), out var protectedValue))
                    return null;

                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedValue), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                // Corrupt file or a token protected under another account: treat as "not paired".
                return null;
            }
        }

        public static void Set(string host, string token)
        {
            var map = Read();
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
            map[Normalize(host)] = Convert.ToBase64String(protectedBytes);
            Write(map);
        }

        public static void Remove(string host)
        {
            var map = Read();
            if (map.Remove(Normalize(host)))
                Write(map);
        }

        /// <summary>True for "COM3" or "/dev/ttyUSB0": a USB serial port rather than a Wi-Fi host.</summary>
        public static bool IsSerialPort(string target) =>
            target.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || target.StartsWith("/dev/", StringComparison.Ordinal);

        private static string Normalize(string host) => host.Trim().ToLowerInvariant();

        private static Dictionary<string, string> Read()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                if (File.Exists(FilePath) &&
                    new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath)) is Dictionary<string, object> raw)
                {
                    foreach (var pair in raw)
                    {
                        if (pair.Value is string value)
                            result[pair.Key] = value;
                    }
                }
            }
            catch (Exception)
            {
                // start over with an empty store
            }

            return result;
        }

        private static void Write(Dictionary<string, string> map)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(map));
        }
    }
}
