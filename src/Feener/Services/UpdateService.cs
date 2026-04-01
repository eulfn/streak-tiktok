using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Feener.Services;

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = [];
}

public class UpdateInfo
{
    public bool HasUpdate { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>APK direct download URL from GitHub Assets, or null if no APK asset found.</summary>
    public string? ApkDownloadUrl { get; set; }
}

public class UpdateService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string ApiUrl = "https://api.github.com/repos/eulfn/streak-tiktok/releases/latest";

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Feener/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Silently fetches the latest GitHub release and compares it with the current app version.
    /// Does not throw exceptions; returns null on failure.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(ApiUrl);
            if (release == null || string.IsNullOrEmpty(release.TagName))
                return null;

            // Strip the potential 'v' prefix from GitHub tag ("v1.4.5" -> "1.4.5")
            string remoteVersionStr = release.TagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? release.TagName.Substring(1)
                : release.TagName;

            if (Version.TryParse(remoteVersionStr, out var remoteVersion) &&
                Version.TryParse(AppInfo.Current.VersionString, out var localVersion))
            {
                // Prefer the explicitly named Feener APK; fall back to first .apk asset
                var apkAsset = release.Assets.FirstOrDefault(a =>
                    a.Name.StartsWith("Feener-", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    ?? release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));

                return new UpdateInfo
                {
                    HasUpdate = remoteVersion > localVersion,
                    LatestVersion = remoteVersionStr,
                    Changelog = release.Body,
                    ReleaseUrl = release.HtmlUrl,
                    ApkDownloadUrl = apkAsset?.BrowserDownloadUrl
                };
            }

            return null; // Version parse failed
        }
        catch
        {
            // Silently swallow network/timeout errors to prevent startup freezing
            return null;
        }
    }

    /// <summary>
    /// Downloads the APK from the given URL with progress reporting.
    /// Returns the full path to the downloaded file, or null on failure.
    /// </summary>
    public async Task<string?> DownloadApkAsync(string url, string version, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use version in filename to avoid caching conflicts across multiple updates
            string fileName = $"Feener-{version}.apk";
            string destPath = Path.Combine(FileSystem.CacheDirectory, fileName);

            // Use a separate client with longer timeout for downloads
            using var downloadClient = new HttpClient();
            downloadClient.DefaultRequestHeaders.Add("User-Agent", "Feener/1.0");
            downloadClient.Timeout = TimeSpan.FromMinutes(10);

            using var response = await downloadClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                    progress.Report((double)downloadedBytes / totalBytes);
            }

            return destPath;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches the release notes body for a specific version tag from GitHub.
    /// Returns null silently on any failure.
    /// </summary>
    public async Task<string?> GetChangelogForVersionAsync(string version)
    {
        try
        {
            string tag = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
            string url = $"https://api.github.com/repos/eulfn/streak-tiktok/releases/tags/{tag}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Feener/1.0");

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();
            return string.IsNullOrWhiteSpace(release?.Body) ? null : release.Body;
        }
        catch
        {
            return null;
        }
    }
}
