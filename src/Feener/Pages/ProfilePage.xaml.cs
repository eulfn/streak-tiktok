using Feener.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Feener.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class ProfilePage : ContentPage
{
    private readonly SessionService _sessionService;
    private readonly SettingsService _settingsService;
    private bool _isCheckingSession = false;
    private bool _sessionCheckCompleted = false;
    private int _navigationCount = 0;
#if ANDROID
    private IDispatcherTimer? _sessionCheckTimeout;
#endif

    public ProfilePage()
    {
        InitializeComponent();
        _sessionService = new SessionService();
        _settingsService = new SettingsService();
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

        LoadProfilePhoto();

        // Load display name
        DisplayNameEntry.Text = _sessionService.GetDisplayName();

        // Load settings
        ScheduleSwitch.IsToggled = _settingsService.IsScheduled();
        SkipUnreachableSwitch.IsToggled = _settingsService.GetSkipUnreachableUsers();

        // Version
        VersionLabel.Text = $"v{AppInfo.Current.VersionString}";

        // Check session
        CheckSessionStatus();
    }

    // ─── Profile Photo ──────────────────────────────────────────────────────────

    private void LoadProfilePhoto()
    {
        var photoPath = _sessionService.GetProfileImagePath();
        if (!string.IsNullOrEmpty(photoPath) && System.IO.File.Exists(photoPath))
        {
            ProfilePhoto.Source = ImageSource.FromFile(photoPath);
            ProfilePhoto.IsVisible = true;
            ProfileEmoji.IsVisible = false;
            // Clip the image to the circle
            ProfilePhoto.Clip = new EllipseGeometry
            {
                Center = new Point(28, 28),
                RadiusX = 28,
                RadiusY = 28
            };
        }
        else
        {
            ProfilePhoto.IsVisible = false;
            ProfileEmoji.IsVisible = true;
        }
    }

    private async void OnProfilePhotoTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Please pick a photo"
            });

            if (result != null)
            {
                var newFile = System.IO.Path.Combine(FileSystem.AppDataDirectory, result.FileName);
                using (var stream = await result.OpenReadAsync())
                using (var newStream = System.IO.File.OpenWrite(newFile))
                    await stream.CopyToAsync(newStream);

                _sessionService.SetProfileImagePath(newFile);
                LoadProfilePhoto();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", $"Could not pick photo: {ex.Message}", "OK");
        }
    }

    // ─── Display Name ───────────────────────────────────────────────────────────

    private void OnDisplayNameChanged(object? sender, EventArgs e)
    {
        _sessionService.SetDisplayName(DisplayNameEntry.Text ?? "User");
    }

    // ─── Session Check (moved from MainPage) ────────────────────────────────────

    private void CheckSessionStatus()
    {
        if (_sessionCheckCompleted)
        {
            UpdateLoginButtonState(_sessionService.IsSessionValid());
            return;
        }

        var lastCheck = _sessionService.GetLastCheckTime();
        if (lastCheck == null)
        {
            _sessionCheckCompleted = true;
            UpdateLoginButtonState(false);
            return;
        }

#if ANDROID
        if (Feener.Platforms.Android.Services.StreakService.IsRunning)
        {
            UpdateLoginButtonState(_sessionService.IsSessionValid());
            return;
        }
#endif

        _isCheckingSession = true;
        _navigationCount = 0;
        UpdateLoginButtonState(false, isChecking: true);

#if ANDROID
        TikTokWebViewHelper.ConfigureWebView(SessionCheckWebView);
        SessionCheckWebView.Source = TikTokWebViewHelper.MessagesUrl;

        if (_sessionCheckTimeout != null)
        {
            _sessionCheckTimeout.Stop();
            _sessionCheckTimeout.Tick -= OnSessionCheckTimeout;
        }
        _sessionCheckTimeout = Dispatcher.CreateTimer();
        _sessionCheckTimeout.Interval = TimeSpan.FromSeconds(10);
        _sessionCheckTimeout.Tick += OnSessionCheckTimeout;
        _sessionCheckTimeout.Start();
#else
        _sessionCheckCompleted = true;
        UpdateLoginButtonState(_sessionService.IsSessionValid());
#endif
    }

#if ANDROID
    private void OnSessionCheckTimeout(object? sender, EventArgs e)
    {
        _sessionCheckTimeout?.Stop();
        if (_isCheckingSession)
        {
            if (Feener.Platforms.Android.Services.StreakService.IsRunning)
            {
                _isCheckingSession = false;
                _sessionCheckCompleted = true;
                MainThread.BeginInvokeOnMainThread(() => UpdateLoginButtonState(_sessionService.IsSessionValid()));
                return;
            }
            _isCheckingSession = false;
            _sessionCheckCompleted = true;
            _sessionService.SetSessionValid(false);
            MainThread.BeginInvokeOnMainThread(() => UpdateLoginButtonState(false));
        }
    }
#endif

    private void OnSessionCheckNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (!_isCheckingSession) return;
        _navigationCount++;
        var result = TikTokWebViewHelper.CheckLoginStatus(e.Url);

        if (result.IsValidUrl && e.Url?.ToLower().Contains("/login") == true)
        {
#if ANDROID
            _sessionCheckTimeout?.Stop();
#endif
            _isCheckingSession = false;
            _sessionCheckCompleted = true;
            TikTokWebViewHelper.UpdateSessionStatus(_sessionService, false);
            MainThread.BeginInvokeOnMainThread(() => UpdateLoginButtonState(false));
            return;
        }

        if (result.IsLoggedIn && _navigationCount >= 1)
        {
            Task.Delay(2000).ContinueWith(_ =>
            {
                if (_isCheckingSession)
                {
#if ANDROID
                    _sessionCheckTimeout?.Stop();
#endif
                    _isCheckingSession = false;
                    _sessionCheckCompleted = true;
                    TikTokWebViewHelper.UpdateSessionStatus(_sessionService, true);
                    MainThread.BeginInvokeOnMainThread(() => UpdateLoginButtonState(true));
                }
            });
        }
    }

    private async void UpdateLoginButtonState(bool isSessionValid, bool isChecking = false)
    {
        await LoginButton.FadeTo(0.5, 100);

        if (isChecking)
        {
            LoginButton.Text = string.Empty;
            LoginButton.BackgroundColor = GetThemeColor("Gray400");
            LoginButton.IsEnabled = false;
            SessionCheckingIndicator.IsVisible = true;
            SessionDot.BackgroundColor = GetThemeColor("Gray400");
            SessionStatusLabel.Text = "Validating...";
            SessionLastCheckLabel.Text = "";
        }
        else if (isSessionValid)
        {
            LoginButton.Text = "Session OK";
            LoginButton.BackgroundColor = GetThemeColor("Success", "#22946E");
            LoginButton.IsEnabled = false;
            SessionCheckingIndicator.IsVisible = false;
            SessionDot.BackgroundColor = GetThemeColor("Success", "#22946E");
            SessionStatusLabel.Text = "Session active";
            var lastCheck = _sessionService.GetLastCheckTime();
            SessionLastCheckLabel.Text = lastCheck.HasValue ? $"Verified {lastCheck.Value:MMM dd, HH:mm}" : "";
        }
        else
        {
            LoginButton.Text = "Login to TikTok";
            LoginButton.BackgroundColor = GetThemeColor("Primary", "#FE2C55");
            LoginButton.IsEnabled = true;
            SessionCheckingIndicator.IsVisible = false;
            SessionDot.BackgroundColor = GetThemeColor("Error", "#9C2121");
            SessionStatusLabel.Text = "Not logged in";
            SessionLastCheckLabel.Text = "Tap below to login";
        }

        await LoginButton.FadeTo(1.0, 200);
    }

    // ─── Actions ────────────────────────────────────────────────────────────────

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        _sessionCheckCompleted = false;
        await Navigation.PushAsync(new LoginPage());
    }

    private void OnScheduleToggled(object? sender, ToggledEventArgs e)
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (e.Value)
            Feener.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        else
            Feener.Platforms.Android.StreakScheduler.CancelSchedule(context);
#endif
    }

    private void OnSkipUnreachableToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.SetSkipUnreachableUsers(e.Value);
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        string currentVersion = AppInfo.Current.VersionString;
        await Navigation.PushModalAsync(new AboutPopupPage(
            "About Feener", currentVersion, string.Empty, false));
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "This will clear your TikTok session. You'll need to login again before running automations.", "Logout", "Cancel");
        if (confirm)
        {
            _sessionService.ClearSession();
            _sessionCheckCompleted = false;
            CheckSessionStatus();
        }
    }
}
