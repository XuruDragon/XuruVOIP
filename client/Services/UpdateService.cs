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
                string latestClean = latestTag.TrimStart('v');
                string currentClean = currentVersionStr.TrimStart('v');

                if (Version.TryParse(latestClean, out var latestVer) && Version.TryParse(currentClean, out var currentVer))
                {
                    return (latestVer > currentVer, latestTag);
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to check for updates from GitHub Releases API", ex);
        }

        return (false, string.Empty);
    }
}
