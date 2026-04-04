using Feener.Services;
using Microsoft.Maui.ApplicationModel;

namespace Feener;

public partial class AboutPopupPage : ContentPage
{
    private readonly string _assignedVersion;
    private readonly bool _isUpdate;
    private readonly string? _apkDownloadUrl;
    private bool _isClosing = false;
    private bool _isDownloading = false;
    private CancellationTokenSource? _downloadCts;
    private bool _isFallbackMode = false;

    public AboutPopupPage(string title, string version, string changelog, bool isUpdate, string? apkDownloadUrl = null)
    {
        InitializeComponent();

        _assignedVersion = version;
        _isUpdate = isUpdate;
        _apkDownloadUrl = apkDownloadUrl;

        HeaderTitle.Text = title;
        VersionLabel.Text = $"v{version}";

        if (isUpdate)
        {
            // Update popup: show changelog, hide the tagline subtitle
            SubHeaderLabel.IsVisible = false;
            if (string.IsNullOrWhiteSpace(changelog))
                ChangelogBorder.IsVisible = false;
            else
                LoadChangelogHtml(changelog);

            CloseButton.IsVisible = false;
            UpdateButtonGrid.IsVisible = true;
        }
        else
        {
            // Welcome popup: hide changelog entirely, show tagline subtitle
            ChangelogBorder.IsVisible = false;
            SubHeaderLabel.IsVisible = true;

            CloseButton.IsVisible = true;
            UpdateButtonGrid.IsVisible = false;
        }
    }

    private void LoadChangelogHtml(string markdown)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        string bg = isDark ? "#1C1C1E" : "#E5E5EA";
        string fg = isDark ? "#E5E5EA" : "#1C1C1E";
        string fgMuted = isDark ? "#AEAEB2" : "#636366";
        string hrColor = isDark ? "#3A3A3C" : "#C7C7CC";

        string bodyHtml = ConvertMarkdownToHtml(markdown);

        string html = $@"<!DOCTYPE html>
<html><head><meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>
<style>
* {{ margin:0; padding:0; box-sizing:border-box; }}
body {{ background:{bg}; color:{fg}; font-family:-apple-system,Roboto,sans-serif; font-size:14px; line-height:1.55; padding:4px 0; }}
h1 {{ font-size:18px; font-weight:700; margin:12px 0 6px 0; }}
h2 {{ font-size:16px; font-weight:700; margin:10px 0 4px 0; }}
h3 {{ font-size:14px; font-weight:600; margin:8px 0 4px 0; }}
p {{ margin:4px 0; }}
strong {{ font-weight:600; }}
ul, ol {{ padding-left:20px; margin:4px 0; }}
li {{ margin:3px 0; }}
hr {{ border:none; border-top:1px solid {hrColor}; margin:12px 0; }}
blockquote {{ border-left:3px solid {hrColor}; padding-left:10px; color:{fgMuted}; margin:6px 0; }}
code {{ background:{hrColor}; padding:1px 4px; border-radius:3px; font-size:13px; }}
</style></head><body>{bodyHtml}</body></html>";

        ChangelogWebView.Source = new HtmlWebViewSource { Html = html };

        // Auto-size WebView height after content loads
        ChangelogWebView.Navigated += (s, e) =>
        {
            ChangelogWebView.EvaluateJavaScriptAsync("document.body.scrollHeight")
                .ContinueWith(t =>
                {
                    if (t.Result is string result && double.TryParse(result, out double height) && height > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ChangelogWebView.HeightRequest = height + 16;
                        });
                    }
                });
        };
    }

    /// <summary>
    /// Lightweight Markdown-to-HTML converter. Handles headings, bold, italic,
    /// inline code, bullet lists, numbered lists, horizontal rules, and blockquotes.
    /// </summary>
    private static string ConvertMarkdownToHtml(string markdown)
    {
        var lines = markdown.Split('\n');
        var sb = new System.Text.StringBuilder();
        bool inUl = false;
        bool inOl = false;

        foreach (var rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');

            // Horizontal rules
            if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^-{3,}$|^\*{3,}$|^_{3,}$"))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine("<hr/>");
                continue;
            }

            // Headings
            if (line.StartsWith("### "))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine($"<h3>{FormatInline(line[4..])}</h3>");
                continue;
            }
            if (line.StartsWith("## "))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine($"<h2>{FormatInline(line[3..])}</h2>");
                continue;
            }
            if (line.StartsWith("# "))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine($"<h1>{FormatInline(line[2..])}</h1>");
                continue;
            }

            // Blockquote
            if (line.StartsWith("> "))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine($"<blockquote>{FormatInline(line[2..])}</blockquote>");
                continue;
            }

            // Unordered list items (- or *)
            var ulMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*[-*]\s+(.+)");
            if (ulMatch.Success)
            {
                if (inOl) { sb.AppendLine("</ol>"); inOl = false; }
                if (!inUl) { sb.AppendLine("<ul>"); inUl = true; }
                sb.AppendLine($"<li>{FormatInline(ulMatch.Groups[1].Value)}</li>");
                continue;
            }

            // Ordered list items
            var olMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\.\s+(.+)");
            if (olMatch.Success)
            {
                if (inUl) { sb.AppendLine("</ul>"); inUl = false; }
                if (!inOl) { sb.AppendLine("<ol>"); inOl = true; }
                sb.AppendLine($"<li>{FormatInline(olMatch.Groups[1].Value)}</li>");
                continue;
            }

            // Empty line
            if (string.IsNullOrWhiteSpace(line))
            {
                CloseList(sb, ref inUl, ref inOl);
                continue;
            }

            // Plain paragraph
            CloseList(sb, ref inUl, ref inOl);
            sb.AppendLine($"<p>{FormatInline(line)}</p>");
        }

        CloseList(sb, ref inUl, ref inOl);
        return sb.ToString();
    }

    private static void CloseList(System.Text.StringBuilder sb, ref bool inUl, ref bool inOl)
    {
        if (inUl) { sb.AppendLine("</ul>"); inUl = false; }
        if (inOl) { sb.AppendLine("</ol>"); inOl = false; }
    }

    /// <summary>Formats inline Markdown: bold, italic, inline code.</summary>
    private static string FormatInline(string text)
    {
        // Inline code: `code`
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]+)`", "<code>$1</code>");
        // Bold: **text** or __text__
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"__(.+?)__", "<strong>$1</strong>");
        // Italic: *text* or _text_
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\w)_(.+?)_(?!\w)", "<em>$1</em>");
        return text;
    }

    // ── Onboarding / Welcome: Continue button ──────────────────────────────
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;

        Preferences.Default.Set("LastAppVersionSeen", _assignedVersion);

        try { await Navigation.PopModalAsync(); } catch { }
    }

    // ── Update: Later button ───────────────────────────────────────────────
    private async void OnLaterClicked(object sender, EventArgs e)
    {
        if (_isClosing || _isDownloading) return;
        _isClosing = true;

        Preferences.Default.Set("LastRemoteVersionSeen", _assignedVersion);

        try { await Navigation.PopModalAsync(); } catch { }
    }

    // ── Update: Install button ─────────────────────────────────────────────
    private async void OnInstallClicked(object? sender, EventArgs e)
    {
        if (_isDownloading) return;

        // If no APK asset found, fall back to opening GitHub releases page
        if (string.IsNullOrEmpty(_apkDownloadUrl))
        {
            await FallbackToGitHubAsync();
            return;
        }

        _isDownloading = true;
        InstallButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        InstallButton.Text = "Downloading...";

        _downloadCts = new CancellationTokenSource();

        var progress = new Progress<double>(percent =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                int pct = (int)Math.Round(percent * 100);
                ProgressLabel.IsVisible = true;
                ProgressLabel.Text = $"Downloading update... {pct}%";
            });
        });

        var updateService = new UpdateService();
        string? apkPath = await updateService.DownloadApkAsync(
            _apkDownloadUrl,
            _assignedVersion,
            progress,
            _downloadCts.Token);

        if (apkPath == null || !File.Exists(apkPath))
        {
            ShowDownloadError();
            return;
        }

        // Pre-mark version state BEFORE handing off to installer.
        // When Android restarts the app after install, CheckStartupPopupAsync will
        // detect UpdateJustInstalled=true and skip straight past the Welcome popup.
        Preferences.Default.Set("UpdateJustInstalled", true);
        Preferences.Default.Set("LastRemoteVersionSeen", _assignedVersion);
        Preferences.Default.Set("LastAppVersionSeen", _assignedVersion);

        ProgressLabel.Text = "Starting installer...";

        TriggerApkInstall(apkPath);

        // Close popup after handing off to the system installer
        try { await Navigation.PopModalAsync(); } catch { }
    }

    private void ShowDownloadError()
    {
        _isDownloading = false;
        InstallButton.IsEnabled = true;
        LaterButton.IsEnabled = true;
        InstallButton.Text = "Install";
        ProgressLabel.IsVisible = true;
        ProgressLabel.Text = "Download failed. Please try again.";
        ProgressLabel.TextColor = Color.FromArgb("#F44336");

        SwitchToFallbackMode();
    }

    private void SwitchToFallbackMode()
    {
        if (_isFallbackMode) return;
        _isFallbackMode = true;

        InstallButton.Text = "Open GitHub Page";
        InstallButton.Clicked -= OnInstallClicked;
        InstallButton.Clicked += OnFallbackClicked;
    }

    private async void OnFallbackClicked(object? sender, EventArgs e)
    {
        await FallbackToGitHubAsync();
    }

    private async Task FallbackToGitHubAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync("https://github.com/eulfn/streak-tiktok/releases/latest");
        }
        catch { }
    }

    private void TriggerApkInstall(string apkPath)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var file = new Java.IO.File(apkPath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                $"{context.PackageName}.fileProvider",
                file);

            // Intent.ActionView with the APK MIME type is the correct cross-version approach.
            // ActionInstallPackage is deprecated on API 29+ and triggers CA1422.
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Feener] APK install intent failed: {ex.Message}");

            // Show user-friendly explanation for the most common failure cause.
            // INSTALL_FAILED_UPDATE_INCOMPATIBLE means the update APK was signed with
            // a different keystore than the installed version — Android blocks this.
            // The user must uninstall the existing app first, then reinstall from GitHub.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProgressLabel.IsVisible = true;
                ProgressLabel.Text = "Install failed. If you see a 'conflicting package' error, " +
                                     "uninstall this app first, then reinstall from GitHub.";
                ProgressLabel.TextColor = Color.FromArgb("#F44336");
                _isDownloading = false;
                InstallButton.IsEnabled = true;
                LaterButton.IsEnabled = true;
                SwitchToFallbackMode();
            });
        }
#endif
    }

    /// <summary>Intercept Android hardware back button to behave consistently with Later.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (_isDownloading)
        {
            _downloadCts?.Cancel();
        }
        if (_isUpdate)
            OnLaterClicked(this, EventArgs.Empty);
        else
            OnCloseClicked(this, EventArgs.Empty);
        return true;
    }
}
