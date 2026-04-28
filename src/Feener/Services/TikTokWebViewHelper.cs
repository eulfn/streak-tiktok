namespace Feener.Services;

/// <summary>
/// Helper class for TikTok WebView configuration and login detection
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class TikTokWebViewHelper
{
    public const string LoginUrl = "https://www.tiktok.com/login";
    public const string MessagesUrl = "https://www.tiktok.com/messages";

    /// <summary>
    /// Configure a WebView for TikTok with proper settings
    /// </summary>
    public static void ConfigureWebView(WebView webView, string? customUserAgent = null)
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var androidWebView = webView.Handler?.PlatformView as Android.Webkit.WebView;
            if (androidWebView != null)
            {
                ConfigureAndroidWebView(androidWebView, customUserAgent);
            }
        });
#endif
    }

#if ANDROID
    /// <summary>
    /// Configure an Android WebView directly
    /// </summary>
    public static void ConfigureAndroidWebView(Android.Webkit.WebView webView, string? customUserAgent = null)
    {
        webView.Settings.JavaScriptEnabled = true;
        webView.Settings.DomStorageEnabled = true;
        webView.Settings.DatabaseEnabled = true;
        webView.Settings.CacheMode = Android.Webkit.CacheModes.Normal;
        
        // Set user agent
        if (!string.IsNullOrEmpty(customUserAgent))
        {
            webView.Settings.UserAgentString = customUserAgent;
        }
        else
        {
            webView.Settings.UserAgentString = GetDefaultUserAgent();
        }
        
        // Enable cookies (critical for TikTok session)
        var cookieManager = Android.Webkit.CookieManager.Instance;
        cookieManager?.SetAcceptCookie(true);
        cookieManager?.SetAcceptThirdPartyCookies(webView, true);
    }

    /// <summary>
    /// Flush cookies to ensure they are persisted
    /// </summary>
    public static void FlushCookies()
    {
        Android.Webkit.CookieManager.Instance?.Flush();
    }

    /// <summary>
    /// Instantly check if a valid sessionid cookie exists for TikTok.
    /// Fast, synchronous, and uses zero network.
    /// </summary>
    public static bool HasValidSessionCookie()
    {
        try
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager == null) return false;

            // Check primary domains where TikTok might store the auth cookie
            string cookies1 = cookieManager.GetCookie("https://www.tiktok.com") ?? string.Empty;
            string cookies2 = cookieManager.GetCookie("https://tiktok.com") ?? string.Empty;

            // sessionid is the core authentication token for TikTok
            return cookies1.Contains("sessionid=") || cookies2.Contains("sessionid=");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking session cookie: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Physically destroy all WebView cookies to guarantee a clean logout.
    /// </summary>
    public static void ClearAllCookies()
    {
        try
        {
            var cookieManager = Android.Webkit.CookieManager.Instance;
            if (cookieManager != null)
            {
                cookieManager.RemoveAllCookies(null);
                cookieManager.Flush();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing cookies: {ex.Message}");
        }
    }
#else
    public static bool HasValidSessionCookie() => false;
    public static void ClearAllCookies() { }
#endif

    /// <summary>
    /// Get default user agent for TikTok
    /// </summary>
    public static string GetDefaultUserAgent()
    {
        return "Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
    }

    /// <summary>
    /// Update session service with login status and optionally flush cookies
    /// </summary>
    public static void UpdateSessionStatus(SessionService sessionService, bool isLoggedIn)
    {
        sessionService.SetSessionValid(isLoggedIn);
        
#if ANDROID
        if (isLoggedIn)
        {
            FlushCookies();
        }
#endif
    }
}
