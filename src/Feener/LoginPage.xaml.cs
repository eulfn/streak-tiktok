using AsyncAwaitBestPractices;
using Feener.Services;

namespace Feener;
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class LoginPage : ContentPage
{
    private readonly SessionService _sessionService;
    private bool _isLoggedIn = false;

    public LoginPage()
    {
        InitializeComponent();
        _sessionService = new SessionService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTikTok();
    }

    private void LoadTikTok()
    {
        LoadingOverlay.IsVisible = true;
        
#if ANDROID
        var desktopUa = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
        _sessionService.SetLoginUserAgent(desktopUa);

        // The native WebView handler must be attached BEFORE we set the UA.
        // If we set Source first, the initial HTTP request goes out with the
        // default Android WebView UA, which TikTok rejects with HTTP 500.
        if (TikTokWebView.Handler != null)
        {
            TikTokWebViewHelper.ConfigureWebView(TikTokWebView, desktopUa);
            TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
        }
        else
        {
            void onHandlerReady(object? s, EventArgs e)
            {
                if (TikTokWebView.Handler != null)
                {
                    TikTokWebViewHelper.ConfigureWebView(TikTokWebView, desktopUa);
                    TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
                    TikTokWebView.HandlerChanged -= onHandlerReady;
                }
            }
            TikTokWebView.HandlerChanged += onHandlerReady;
        }
#else
        TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
#endif
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (_isLoggedIn)
        {
            return;
        }
        LoadingOverlay.IsVisible = false;

        // Use direct cookie check instead of URL matching for true confirmation
        bool hasSession = TikTokWebViewHelper.HasValidSessionCookie();

        if (hasSession)
        {
            _isLoggedIn = true;
            Done().SafeFireAndForget();
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (TikTokWebView.CanGoBack)
        {
            TikTokWebView.GoBack();
        }
        else
        {
            await Navigation.PopAsync();
        }
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        TikTokWebView.Reload();
    }

    private async Task Done()
    {
        // Update session status using helper
        TikTokWebViewHelper.UpdateSessionStatus(_sessionService, _isLoggedIn);
        
        if (_isLoggedIn)
        {
            await DisplayAlert("Logged In", 
                "You're logged in to TikTok! The app will use this session for background messaging.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Not Logged In", 
                "Please login to TikTok first before continuing.", "OK");
        }
    }
}



