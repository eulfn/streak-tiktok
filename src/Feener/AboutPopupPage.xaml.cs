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

    public AboutPopupPage(string title, string version, string changelog, bool isUpdate, string? apkDownloadUrl = null)
    {
        InitializeComponent();

        _assignedVersion = version;
        _isUpdate = isUpdate;
        _apkDownloadUrl = apkDownloadUrl;

        HeaderTitle.Text = title;
        VersionLabel.Text = $"v{version}";

        if (string.IsNullOrWhiteSpace(changelog))
        {
            ChangelogBorder.IsVisible = false;
        }
        else
        {
            ChangelogLabel.Text = changelog;
        }

        // Show appropriate buttons based on popup type
        if (isUpdate)
        {
            CloseButton.IsVisible = false;
            UpdateButtonGrid.IsVisible = true;
        }
        else
        {
            CloseButton.IsVisible = true;
            UpdateButtonGrid.IsVisible = false;
        }
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

        // Show the GitHub fallback option inside progress label temporarily
        Dispatcher.CreateTimer().Start();
        _ = ShowFallbackButtonAsync();
    }

    private async Task ShowFallbackButtonAsync()
    {
        await Task.Delay(400);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Repurpose the Install button as a fallback GitHub link
            InstallButton.Text = "Open GitHub Page";
            InstallButton.Clicked -= OnInstallClicked;
            InstallButton.Clicked += async (s, e) => await FallbackToGitHubAsync();
        });
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
                InstallButton.Text = "Open GitHub Page";
                InstallButton.Clicked -= OnInstallClicked;
                InstallButton.Clicked += async (s, e) => await FallbackToGitHubAsync();
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
