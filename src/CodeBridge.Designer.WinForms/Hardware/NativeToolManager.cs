using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CodeBridge.Designer.WinForms.Hardware;

internal static class NativeToolManager
{
    private static readonly string ToolsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeBridge", "tools");
    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<string> DownloadAndInstallToolAsync(
        string toolName,
        string version,
        string downloadUrl,
        string expectedSha256,
        string executableName,
        IProgress<string>? progress = null)
    {
        var toolDir = Path.Combine(ToolsDirectory, toolName, version);
        var executablePath = Path.Combine(toolDir, executableName);

        if (File.Exists(executablePath))
        {
            progress?.Report($"{toolName} {version} is already installed.");
            return executablePath;
        }

        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Security error: Download URL for {toolName} must use secure HTTPS.");
        }

        Directory.CreateDirectory(toolDir);
        var tempFile = Path.Combine(toolDir, $"{toolName}_{version}_download.tmp");

        try
        {
            progress?.Report($"Downloading {toolName} {version}...");
            
            HttpResponseMessage? response = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    break;
                }
                catch when (attempt < 3)
                {
                    response?.Dispose();
                    response = null;
                    await Task.Delay(1000 * attempt);
                    progress?.Report($"Retrying download of {toolName} (attempt {attempt + 1}/3)...");
                }
            }

            if (response == null)
            {
                throw new HttpRequestException($"Failed to download {toolName} after 3 attempts.");
            }

            using (response)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(tempFile);
                await stream.CopyToAsync(fileStream);
            }

            if (!string.IsNullOrEmpty(expectedSha256))
            {
                progress?.Report("Verifying checksum...");
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(tempFile);
                var hash = sha256.ComputeHash(stream);
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                if (!string.Equals(hashString, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Checksum verification failed for {toolName}. Expected {expectedSha256}, got {hashString}.");
                }
            }

            progress?.Report("Extracting archive...");
            
            if (downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(tempFile, toolDir, overwriteFiles: true);
            }
            else
            {
                // Standalone binary or other format, just rename
                File.Copy(tempFile, executablePath, overwrite: true);
            }
            
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            
            // Search for the executable in the extracted folder in case it was in a subfolder
            if (!File.Exists(executablePath))
            {
                var files = Directory.GetFiles(toolDir, executableName, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    executablePath = files[0];
                }
            }

            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException($"Failed to find {executableName} after installing {toolName}.");
            }

            progress?.Report($"{toolName} installed successfully.");
            return executablePath;
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }
}
