using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace XuruVoipClient.Services;

public class UpdateService
{
    private static readonly HttpClient HttpClient = new();

    static UpdateService()
    {
        // GitHub API requires a User-Agent header to prevent 403 Forbidden responses
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("XuruVoipClient");
    }

    /// <summary>
    /// Checks if a newer release version exists on GitHub compared to the current version.
    /// </summary>
    /// <param name="currentVersionStr">Current client version string (e.g. "1.0.0")</param>
    /// <returns>A tuple indicating if an update is available and the tag name of the latest version</returns>
    public static async Task<(bool UpdateAvailable, string LatestVersion)> CheckForUpdatesAsync(string currentVersionStr)
    {
        try
        {
            var response = await HttpClient.GetStringAsync("https://api.github.com/repos/XuruDragon/XuruVOIP/releases/latest");
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("tag_name", out var tagProp))
            {
                string latestTag = tagProp.GetString() ?? "";
                bool updateAvailable = IsUpdateAvailable(latestTag, currentVersionStr);
                return (updateAvailable, latestTag);
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to check for updates from GitHub Releases API", ex);
        }

        return (false, string.Empty);
    }

    internal static bool IsUpdateAvailable(string latestTag, string currentVersionStr)
    {
        if (string.IsNullOrEmpty(latestTag) || string.IsNullOrEmpty(currentVersionStr))
            return false;

        string latestClean = latestTag.TrimStart('v').Trim();
        string currentClean = currentVersionStr.TrimStart('v').Trim();

        if (Version.TryParse(latestClean, out var latestVer) && Version.TryParse(currentClean, out var currentVer))
        {
            return latestVer > currentVer;
        }
        return false;
    }

    /// <summary>
    /// Checks if the application is running from the default Program Files directories,
    /// indicating it was installed via the MSI installer.
    /// </summary>
    public static bool IsInstalledVersion()
    {
        try
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            return exePath.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                   exePath.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to check if app is installed version", ex);
            return false;
        }
    }

    /// <summary>
    /// Checks if the target version is live on the WinGet catalog.
    /// </summary>
    public static async Task<bool> IsVersionAvailableOnWinGetAsync(string versionStr)
    {
        try
        {
            string cleanVersion = versionStr.TrimStart('v').Trim();
            
            // Try with the clean version string first
            if (await RunWinGetShowCheckAsync(cleanVersion))
                return true;

            // If it parses as a Version, try formatting it to 4 parts (e.g. 0.2.0 -> 0.2.0.0)
            if (Version.TryParse(cleanVersion, out var parsedVersion))
            {
                string fourDigitVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}.{Math.Max(0, parsedVersion.Revision)}";
                if (fourDigitVersion != cleanVersion && await RunWinGetShowCheckAsync(fourDigitVersion))
                    return true;
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Error checking winget version availability", ex);
        }
        return false;
    }

    private static Task<bool> RunWinGetShowCheckAsync(string version)
    {
        var tcs = new TaskCompletionSource<bool>();
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"show XuruDragon.XuruVOIPClient --version {version}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode == 0);
                process.Dispose();
            };

            if (!process.Start())
            {
                tcs.SetResult(false);
            }
        }
        catch (Exception)
        {
            tcs.SetResult(false);
        }
        return tcs.Task;
    }
}
