using Microsoft.Maui.Controls.Shapes;
using Feener.Models;
using Feener.Services;
using Feener.Views;

namespace Feener.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class HistoryPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly SuccessRateDrawable _chartDrawable;
    private bool _isExportingLogs = false;

    public HistoryPage()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
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
        this.Opacity = 0;
        this.TranslationY = 12;
        await Task.WhenAll(
            this.FadeTo(1, 280, Easing.SinInOut),
            this.TranslateTo(0, 0, 280, Easing.SinInOut));

        UpdateSuccessChart();
        LoadHistory();
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
        foreach (var run in allHistory) HistoryContainer.Children.Add(CreateHistoryView(run));
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
                Text = skippedCount > 0 ? $"{successCount}/{totalCount} sent • {skippedCount} skipped" : $"{successCount}/{totalCount} messages sent",
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

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        var history = _settingsService.GetRunHistory();
        if (history.Count == 0) return;
        bool confirm = await DisplayAlert("Clear History", "Are you sure you want to clear your run history?", "Clear", "Cancel");
        if (confirm) { _settingsService.ClearRunHistory(); LoadHistory(); UpdateSuccessChart(); }
    }

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
}
