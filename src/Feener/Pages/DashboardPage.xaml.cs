using Microsoft.Maui.Controls.Shapes;
using Feener.Models;
using Feener.Services;
using Feener.Views;

namespace Feener.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class DashboardPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly SessionService _sessionService;
    private readonly UpdateService _updateService;
    private bool _isCheckingForUpdates = false;
    private bool _isAppInForeground = false;
    private IDispatcherTimer? _statusTimer;
    private readonly SuccessRateDrawable _chartDrawable;

    public DashboardPage()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
        _sessionService = new SessionService();
        _updateService = new UpdateService();
        _chartDrawable = new SuccessRateDrawable();
        SuccessChartView.Drawable = _chartDrawable;
    }

    private Color GetThemeColor(string key, string fallbackHex = "#92979E")
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var resource) && resource is Color color)
            return color;
        return Color.FromArgb(fallbackHex);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isAppInForeground = true;

        // Fade-in animation for smooth tab transition
        this.Opacity = 0;
        await this.FadeTo(1, 250, Easing.CubicOut);

        // Update greeting
        GreetingLabel.Text = $"Hi, {_sessionService.GetDisplayName()}";

        // Update session indicator
        UpdateSessionIndicator();

        LoadSettings();
        LoadHistory();
        UpdateStatus();
        UpdateSuccessChart();

        await EvaluatePermissionsAsync();

        if (_statusTimer == null)
        {
            _statusTimer = Dispatcher.CreateTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += OnStatusTimerTick;
        }
        _statusTimer.Start();
        OnStatusTimerTick(null, EventArgs.Empty);

        _ = CheckStartupPopupAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isAppInForeground = false;
        _statusTimer?.Stop();
    }

    private void UpdateSessionIndicator()
    {
        bool valid = _sessionService.IsSessionValid();
        SessionDot.BackgroundColor = valid
            ? GetThemeColor("Success", "#22946E")
            : GetThemeColor("Gray400", "#8B8F96");
        SessionStatusLabel.Text = valid ? "Session active" : "Not logged in";
        SessionActionLabel.IsVisible = !valid;

        MasterRunButton.IsEnabled = valid;
        MasterRunButton.Opacity = valid ? 1.0 : 0.5;
    }

    private void OnStatusTimerTick(object? sender, EventArgs e)
    {
        bool isRunning = false;
#if ANDROID
        isRunning = Feener.Platforms.Android.Services.StreakService.IsRunning;
#endif
        RunButtonsContainer.IsVisible = !isRunning;
        StopServiceButton.IsVisible = isRunning;
        UpdateStatus();
        UpdateBurstPlanDisplay();
    }

    // ─── Update / Startup Popup Logic ───────────────────────────────────────────

    private static string NormalizeVersion(string raw)
        => raw.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? raw.Substring(1) : raw;

    private async Task CheckStartupPopupAsync()
    {
        if (_isCheckingForUpdates) return;
        _isCheckingForUpdates = true;
        try
        {
            if (!_isAppInForeground) return;
            if (Navigation.ModalStack.Any(p => p is AboutPopupPage)) return;

            string currentVersion = NormalizeVersion(AppInfo.Current.VersionString);

            bool updateJustInstalled = Preferences.Default.Get("UpdateJustInstalled", false);
            if (updateJustInstalled)
            {
                Preferences.Default.Remove("UpdateJustInstalled");
                Preferences.Default.Set("LastAppVersionSeen", currentVersion);
                _isCheckingForUpdates = false;
                await CheckUpdateOnlyAsync();
                return;
            }

            string lastAppSeen = NormalizeVersion(Preferences.Default.Get("LastAppVersionSeen", string.Empty));
            if (string.IsNullOrEmpty(lastAppSeen) || lastAppSeen != currentVersion)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Navigation.PushModalAsync(new AboutPopupPage(
                        "Welcome to Feener", currentVersion, string.Empty, false)));
                return;
            }

            _isCheckingForUpdates = false;
            await CheckUpdateOnlyAsync();
        }
        catch { }
        finally { _isCheckingForUpdates = false; }
    }

    private async Task CheckUpdateOnlyAsync()
    {
        if (_isCheckingForUpdates) return;
        _isCheckingForUpdates = true;
        try
        {
            if (!_isAppInForeground) return;
            if (Navigation.ModalStack.Any(p => p is AboutPopupPage)) return;

            string currentVersion = NormalizeVersion(AppInfo.Current.VersionString);
            string lastRemoteSeen = NormalizeVersion(Preferences.Default.Get("LastRemoteVersionSeen", string.Empty));

            var updateCheck = await _updateService.CheckForUpdatesAsync();
            if (updateCheck == null || !updateCheck.HasUpdate) return;

            string remoteVersion = NormalizeVersion(updateCheck.LatestVersion);
            if (remoteVersion == lastRemoteSeen || remoteVersion == currentVersion) return;
            if (Navigation.ModalStack.Any(p => p is AboutPopupPage)) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Navigation.PushModalAsync(new AboutPopupPage(
                    "Update Available!", remoteVersion, updateCheck.Changelog, true, updateCheck.ApkDownloadUrl)));
        }
        catch { }
        finally { _isCheckingForUpdates = false; }
    }

    // ─── Settings / Mode Switching ──────────────────────────────────────────────

    private void LoadSettings()
    {
        MessageEditor.Text = _settingsService.GetMessageText();
        var isBurstActive = _settingsService.IsBurstModeActive();
        if (isBurstActive) SetBurstModeUI();
        else SetNormalModeUI();
        LoadBurstMessages();
        BurstTargetUserEntry.Text = _settingsService.GetBurstTargetUsername();
        BurstDailyLimitEntry.Text = _settingsService.GetBurstDailyLimit().ToString();
        UpdateBurstPlanDisplay();
    }

    private void OnNormalModeTapped(object? sender, TappedEventArgs e)
    {
        _settingsService.SetBurstModeActive(false);
        SetNormalModeUI();
    }

    private void OnBurstModeTapped(object? sender, TappedEventArgs e)
    {
        _settingsService.SetBurstModeActive(true);
        SetBurstModeUI();
    }

    private async void SetNormalModeUI()
    {
        if (BurstModeContainer.IsVisible)
        {
            await BurstModeContainer.FadeTo(0, 150, Easing.CubicIn);
            BurstModeContainer.IsVisible = false;
        }
        NormalModeTabBorder.BackgroundColor = GetThemeColor("Primary", "#FE2C55");
        NormalModeTabLabel.TextColor = GetThemeColor("White", "#FFFFFF");
        BurstModeTabBorder.BackgroundColor = Colors.Transparent;
        BurstModeTabLabel.TextColor = GetThemeColor("Gray600", "#4B5563");
        NormalModeContainer.Opacity = 0;
        NormalModeContainer.IsVisible = true;
        await NormalModeContainer.FadeTo(1, 200, Easing.CubicOut);
        MasterRunButton.Text = "Run Normal";
        MasterRunButton.BackgroundColor = GetThemeColor("Primary", "#FE2C55");
    }

    private async void SetBurstModeUI()
    {
        if (NormalModeContainer.IsVisible)
        {
            await NormalModeContainer.FadeTo(0, 150, Easing.CubicIn);
            NormalModeContainer.IsVisible = false;
        }
        BurstModeTabBorder.BackgroundColor = GetThemeColor("BurstAccent", "#8B5CF6");
        BurstModeTabLabel.TextColor = Colors.White;
        NormalModeTabBorder.BackgroundColor = Colors.Transparent;
        NormalModeTabLabel.TextColor = GetThemeColor("Gray600", "#4B5563");
        BurstModeContainer.Opacity = 0;
        BurstModeContainer.IsVisible = true;
        await BurstModeContainer.FadeTo(1, 200, Easing.CubicOut);
        MasterRunButton.Text = "Run Burst";
        MasterRunButton.BackgroundColor = GetThemeColor("BurstAccent", "#8B5CF6");
    }

    // ─── Burst Plan / Messages ──────────────────────────────────────────────────

    private void UpdateBurstPlanDisplay()
    {
        var (sessions, minutes, totalSeconds, remaining, dailyLimit) = _settingsService.CalculateBurstPlan();
        var dailySent = dailyLimit - remaining;
        var hours = totalSeconds / 3600;
        var partMins = (totalSeconds % 3600) / 60;
        var partSecs = totalSeconds % 60;
        string timeStr = hours > 0 ? $"~{hours}h {partMins}m" : $"~{partMins}m {partSecs}s";
        BurstPlanLabel.Text = remaining > 0
            ? $"{remaining} messages left \u2022 ~{sessions} sessions"
            : "Daily cap reached! Come back tomorrow.";
        BurstTimeEstimateLabel.Text = remaining > 0 ? timeStr : "0m 0s";
        BurstDailyProgressLabel.Text = $"{dailySent}/{dailyLimit}";
    }

    private async void OnBurstLimitChanged(object? sender, EventArgs e)
    {
        if (int.TryParse(BurstDailyLimitEntry.Text, out int newLimit))
        {
            if (newLimit > SettingsService.BurstMaxDailyCeiling)
                await DisplayAlert("Limit Capped", $"The daily burst limit cannot exceed {SettingsService.BurstMaxDailyCeiling} messages for security and anti-spam reasons.", "OK");
            _settingsService.SetBurstDailyLimit(newLimit);
            BurstDailyLimitEntry.Text = _settingsService.GetBurstDailyLimit().ToString();
            UpdateBurstPlanDisplay();
        }
    }

    private void LoadBurstMessages()
    {
        BurstMessagesStack.Children.Clear();
        var msgs = _settingsService.GetBurstMessages();
        if (msgs.Count == 0) msgs.Add(SettingsService.DefaultMessage);
        foreach (var m in msgs) AddBurstMessageEditorUI(m);
        UpdateAddBurstMessageButtonVisibility();
    }

    private void AddBurstMessageEditorUI(string initialText)
    {
        var border = new Border
        {
            Stroke = GetThemeColor("BorderColorLight", "#E5E5E5"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Margin = new Thickness(0, 0, 0, 8)
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        var editor = new Editor { Text = initialText, Placeholder = "Enter burst message...", HeightRequest = 60, Margin = new Thickness(8) };
        editor.TextChanged += OnBurstSettingsChanged;
        var removeBtn = new Button
        {
            Text = "X", BackgroundColor = Colors.Transparent,
            TextColor = GetThemeColor("DeleteColor", "#EF4444"),
            FontAttributes = FontAttributes.Bold, WidthRequest = 40, VerticalOptions = LayoutOptions.Center
        };
        removeBtn.Clicked += (s, e) =>
        {
            if (BurstMessagesStack.Children.Count > 1) { BurstMessagesStack.Children.Remove(border); SaveBurstSettings(); UpdateAddBurstMessageButtonVisibility(); }
            else DisplayAlert("Limit Reached", "You must have at least one burst message.", "OK");
        };
        grid.Children.Add(editor); Grid.SetColumn(editor, 0);
        grid.Children.Add(removeBtn); Grid.SetColumn(removeBtn, 1);
        border.Content = grid;
        BurstMessagesStack.Children.Add(border);
    }

    private void OnAddBurstMessageClicked(object? sender, EventArgs e)
    {
        if (BurstMessagesStack.Children.Count < 5) { AddBurstMessageEditorUI(""); SaveBurstSettings(); UpdateAddBurstMessageButtonVisibility(); }
    }

    private void UpdateAddBurstMessageButtonVisibility() => AddBurstMessageButton.IsVisible = BurstMessagesStack.Children.Count < 5;

    private void OnBurstSettingsChanged(object? sender, TextChangedEventArgs e) => SaveBurstSettings();

    private void SaveBurstSettings()
    {
        _settingsService.SetBurstTargetUsername(BurstTargetUserEntry.Text?.Trim() ?? "");
        var messages = new List<string>();
        foreach (Border border in BurstMessagesStack.Children)
        {
            if (border.Content is Grid grid && grid.Children[0] is Editor editor)
            {
                var text = editor.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(text)) messages.Add(text);
            }
        }
        if (messages.Count == 0) messages.Add(SettingsService.DefaultMessage);
        _settingsService.SetBurstMessages(messages);
    }

    // ─── Status / History ───────────────────────────────────────────────────────

    private void UpdateStatus()
    {
        var isScheduled = _settingsService.IsScheduled();
        var lastRun = _settingsService.GetLastRunTime();
        if (lastRun.HasValue)
        {
            var timeSince = DateTime.Now - lastRun.Value;
            if (timeSince.TotalMinutes < 60) LastRunLabel.Text = $"{(int)timeSince.TotalMinutes} minutes ago";
            else if (timeSince.TotalHours < 24) LastRunLabel.Text = $"{(int)timeSince.TotalHours} hours ago";
            else LastRunLabel.Text = lastRun.Value.ToString("MMM dd, HH:mm");
        }
        else LastRunLabel.Text = "Never";

        if (isScheduled)
        {
            var nextRun = _settingsService.GetNextRunTime();
            var timeUntil = nextRun - DateTime.Now;
            if (timeUntil.TotalMinutes < 60) NextRunLabel.Text = $"In {(int)timeUntil.TotalMinutes} minutes";
            else if (timeUntil.TotalHours < 24) NextRunLabel.Text = $"In {(int)timeUntil.TotalHours} hours";
            else NextRunLabel.Text = nextRun.ToString("MMM dd, HH:mm");
        }
        else NextRunLabel.Text = "Not scheduled";
    }

    private void UpdateSuccessChart()
    {
        var history = _settingsService.GetRunHistory();
        int total = history.Count;
        int success = history.Count(r => r.Success);
        float rate = total > 0 ? (float)success / total : 0;

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        _chartDrawable.IsDarkTheme = isDark;
        _chartDrawable.SuccessRate = rate;
        _chartDrawable.RateText = total > 0 ? $"{(int)(rate * 100)}%" : "—";
        _chartDrawable.SubText = total > 0 ? "success" : "";
        SuccessChartView.Invalidate();

        if (total > 0)
        {
            SuccessRateLabel.Text = $"{success} of {total} runs successful";
            TotalRunsLabel.Text = $"Last {Math.Min(total, 50)} runs tracked";

            // Estimate avg duration from most recent run
            var lastRun = history.FirstOrDefault();
            if (lastRun != null && lastRun.FriendResults.Count > 0)
            {
                var lastTs = lastRun.FriendResults.Max(r => r.Timestamp);
                var dur = lastTs - lastRun.RunTime;
                AvgDurationLabel.Text = dur.TotalMinutes > 1
                    ? $"Last run: ~{(int)dur.TotalMinutes}m {dur.Seconds}s"
                    : $"Last run: ~{(int)dur.TotalSeconds}s";
            }
        }
        else
        {
            SuccessRateLabel.Text = "No data yet";
            TotalRunsLabel.Text = "";
            AvgDurationLabel.Text = "";
        }
    }

    private void LoadHistory()
    {
        var allHistory = _settingsService.GetRunHistory();
        var itemsToRemove = HistoryContainer.Children.Where(c => c != NoHistoryLabel).ToList();
        foreach (var item in itemsToRemove) HistoryContainer.Children.Remove(item);
        NoHistoryLabel.IsVisible = allHistory.Count == 0;
        SeeAllHistoryButton.IsVisible = allHistory.Count > 5;
        foreach (var run in allHistory.Take(5)) HistoryContainer.Children.Add(CreateHistoryView(run));
    }

    private async void OnSeeAllHistoryClicked(object? sender, EventArgs e)
    {
        var allHistory = _settingsService.GetRunHistory();
        var summary = string.Join("\n\n", allHistory.Take(20).Select(r =>
            $"{r.RunTime:MMM dd, HH:mm}: {(r.Success ? "Success" : "Failed")}\n" +
            $"{(r.FriendResults.Count > 0 ? $"{r.FriendResults.Count(f => f.Success)}/{r.FriendResults.Count} sent" : r.ErrorMessage)}"));
        await DisplayAlert("Run History", summary, "Done");
    }

    private View CreateHistoryView(StreakRunResult run)
    {
        var successCount = run.FriendResults.Count(r => r.Success);
        var totalCount = run.FriendResults.Count;
        var statusColor = run.Success ? GetThemeColor("Success", "#22946E") : GetThemeColor("Error", "#9C2121");
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12, Padding = new Thickness(0, 6)
        };
        var statusDot = new Border
        {
            WidthRequest = 8, HeightRequest = 8, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            BackgroundColor = statusColor, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(4, 0)
        };
        grid.Children.Add(statusDot);
        var infoStack = new VerticalStackLayout { Spacing = 3 };
        infoStack.Children.Add(new Label { Text = run.RunTime.ToString("MMM dd, HH:mm"), FontSize = 15, FontFamily = "InterMedium" });
        if (totalCount > 0)
        {
            var skippedCount = totalCount - successCount;
            var infoLabel = new Label
            {
                Text = skippedCount > 0 ? $"{successCount}/{totalCount} sent \u2022 {skippedCount} skipped" : $"{successCount}/{totalCount} messages sent",
                FontSize = 13
            };
            infoLabel.SetAppThemeColor(Label.TextColorProperty, GetThemeColor("Gray400"), GetThemeColor("Gray400"));
            infoStack.Children.Add(infoLabel);
        }
        else if (!string.IsNullOrEmpty(run.ErrorMessage))
        {
            infoStack.Children.Add(new Label { Text = run.ErrorMessage, FontSize = 12, TextColor = statusColor, LineBreakMode = LineBreakMode.TailTruncation });
        }
        Grid.SetColumn(infoStack, 1);
        grid.Children.Add(infoStack);
        return grid;
    }

    // ─── Actions ────────────────────────────────────────────────────────────────

    private void OnMessageChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.NewTextValue)) _settingsService.SetMessageText(e.NewTextValue);
    }

    private async void OnMasterRunClicked(object? sender, EventArgs e)
    {
        await MasterRunButton.ScaleTo(0.96, 80, Easing.CubicIn);
        await MasterRunButton.ScaleTo(1.0, 80, Easing.CubicOut);

        bool isBurstMode = _settingsService.IsBurstModeActive();
        if (isBurstMode)
        {
            var target = _settingsService.GetBurstTargetUsername();
            if (string.IsNullOrWhiteSpace(target)) { await DisplayAlert("No Target", "Please enter a target username for Burst Mode.", "OK"); return; }
            var plan = _settingsService.CalculateBurstPlan();
            if (plan.remaining == 0) { await DisplayAlert("Daily Cap Reached", $"You've already sent {plan.dailyLimit} burst messages today. Come back tomorrow!", "OK"); return; }
            var hours = plan.estimatedTotalSeconds / 3600; var mins = (plan.estimatedTotalSeconds % 3600) / 60; var secs = plan.estimatedTotalSeconds % 60;
            var timeStr = hours > 0 ? $"{hours}h {mins}m" : $"{mins}m {secs}s";
            var confirm = await DisplayAlert("Burst Mode",
                $"Target: @{target}\nMessages: {plan.remaining} remaining today\nSessions: ~{plan.sessionsNeeded} (with hibernation breaks)\nEstimated time: ~{timeStr}\n\nMessages will be chunked into batches of {SettingsService.BurstChunkSizeMin}-{SettingsService.BurstChunkSizeMax} with smart hibernation breaks to preserve battery.",
                "Start Bursting", "Cancel");
            if (!confirm) return;
#if ANDROID
            await RequestNotificationPermission();
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            bool started = Feener.Platforms.Android.StreakScheduler.RunNow(context, isBurstMode: true);
            if (started) { await DisplayAlert("Burst Started", $"Sending {plan.remaining} messages in ~{plan.sessionsNeeded} sessions with hibernation breaks. Tap Stop to cancel anytime.", "OK"); UpdateStatus(); }
            else await DisplayAlert("Service Locked", "An automation process is already active.", "OK");
#else
            await DisplayAlert("Info", "This feature is only available on Android", "OK");
#endif
        }
        else
        {
            var friends = _settingsService.GetEnabledFriends();
            if (friends.Count == 0) { await DisplayAlert("No Friends", "Please add at least one friend before running.", "OK"); return; }
            var confirm = await DisplayAlert("Run Now", $"This will send your streak message to {friends.Count} friend{(friends.Count != 1 ? "s" : "")}. Continue?", "Run", "Cancel");
            if (!confirm) return;
#if ANDROID
            await RequestNotificationPermission();
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            bool started = Feener.Platforms.Android.StreakScheduler.RunNow(context, isBurstMode: false);
            if (started) { await DisplayAlert("Started", "Normal streak run started. Check the notification for progress.", "OK"); UpdateStatus(); }
            else await DisplayAlert("Already Running", "A process is already running. Please wait for it to finish.", "OK");
#else
            await DisplayAlert("Info", "This feature is only available on Android", "OK");
#endif
        }
    }

    private void OnStopServiceClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        Feener.Platforms.Android.StreakScheduler.StopService(context);
#endif
    }

    private bool _isExportingLogs = false;
    private async void OnExportLogsClicked(object? sender, EventArgs e)
    {
        if (_isExportingLogs) return;
        _isExportingLogs = true;
        try
        {
#if ANDROID
            var logs = Feener.Platforms.Android.Services.StreakService.GetLogs();
#else
            var logs = new List<string>();
#endif
            if (logs == null || logs.Count == 0) { await DisplayAlert("Export Logs", "No logs to export", "OK"); return; }
            var textContent = string.Join(Environment.NewLine, logs);
            var fileName = $"streak_logs_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            var filePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
            await System.IO.File.WriteAllTextAsync(filePath, textContent);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Export System Logs", File = new ShareFile(filePath, "text/plain") });
        }
        catch (Exception ex) { await DisplayAlert("Export Failed", $"Could not export logs: {ex.Message}", "OK"); }
        finally { _isExportingLogs = false; }
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        var history = _settingsService.GetRunHistory();
        if (history.Count == 0) return;
        bool confirm = await DisplayAlert("Clear History", "Are you sure you want to clear your run history?", "Clear", "Cancel");
        if (confirm) { _settingsService.ClearRunHistory(); LoadHistory(); UpdateSuccessChart(); }
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        GreetingLabel.Text = $"Hi, {_sessionService.GetDisplayName()}";
        UpdateSessionIndicator();
        LoadSettings();
        LoadHistory();
        UpdateStatus();
        UpdateSuccessChart();
        await EvaluatePermissionsAsync();
        await CheckUpdateOnlyAsync();
        MainRefreshView.IsRefreshing = false;
    }

    // ─── Permissions ────────────────────────────────────────────────────────────

    private async Task EvaluatePermissionsAsync()
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        bool exactAlarmGranted = Feener.Platforms.Android.StreakScheduler.CanScheduleExactAlarms(context);
        bool batteryOptGranted = Feener.Platforms.Android.StreakScheduler.IsIgnoringBatteryOptimizations(context);
        bool notificationGranted = true;
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            notificationGranted = (status == PermissionStatus.Granted);
        }
        BtnExactAlarm.IsVisible = !exactAlarmGranted;
        BtnBatteryOpt.IsVisible = !batteryOptGranted;
        BtnNotification.IsVisible = !notificationGranted;
        PermissionsPanel.IsVisible = !exactAlarmGranted || !batteryOptGranted || !notificationGranted;
#else
        PermissionsPanel.IsVisible = false;
#endif
    }

    private void OnRequestExactAlarmClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        Feener.Platforms.Android.StreakScheduler.RequestExactAlarmPermission(context);
#endif
    }

    private void OnRequestBatteryOptimizationClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        Feener.Platforms.Android.StreakScheduler.RequestBatteryOptimizationExemption(context);
#endif
    }

    private async void OnRequestNotificationClicked(object? sender, EventArgs e)
    {
        await RequestNotificationPermission();
        await EvaluatePermissionsAsync();
    }

    private async Task RequestNotificationPermission()
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                await DisplayAlert("Permission Required", "Notification permission is required to show status while sending streaks.", "OK");
        }
#endif
    }
}
