namespace Feener.Services;

/// <summary>
/// Service for managing TikTok session state
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class SessionService
{
    private const string SessionValidKey = "session_valid";
    private const string SessionLastCheckKey = "session_last_check";
    private const string DisplayNameKey = "session_display_name";

    /// <summary>
    /// Get whether the session was valid on last check
    /// </summary>
    public bool IsSessionValid()
    {
        return Preferences.Get(SessionValidKey, false);
    }

    /// <summary>
    /// Set whether the session is valid
    /// </summary>
    public void SetSessionValid(bool valid)
    {
        Preferences.Set(SessionValidKey, valid);
        Preferences.Set(SessionLastCheckKey, DateTime.Now.Ticks);
    }

    /// <summary>
    /// Get when the session was last checked
    /// </summary>
    public DateTime? GetLastCheckTime()
    {
        var ticks = Preferences.Get(SessionLastCheckKey, 0L);
        return ticks > 0 ? new DateTime(ticks) : null;
    }

    /// <summary>
    /// Get the user's display name
    /// </summary>
    public string GetDisplayName()
    {
        return Preferences.Get(DisplayNameKey, "User");
    }

    /// <summary>
    /// Set the user's display name
    /// </summary>
    public void SetDisplayName(string name)
    {
        Preferences.Set(DisplayNameKey, string.IsNullOrWhiteSpace(name) ? "User" : name.Trim());
    }

    /// <summary>
    /// Clear session data (logout)
    /// </summary>
    public void ClearSession()
    {
        Preferences.Set(SessionValidKey, false);
        Preferences.Remove(SessionLastCheckKey);
        
        // Physically destroy cookies to guarantee a clean logout
        TikTokWebViewHelper.ClearAllCookies();
    }

    /// <summary>
    /// Get the path to the user's local profile photo
    /// </summary>
    public string GetProfileImagePath()
    {
        return Preferences.Get("session_profile_photo", string.Empty);
    }

    /// <summary>
    /// Set the path to the user's local profile photo
    /// </summary>
    public void SetProfileImagePath(string path)
    {
        Preferences.Set("session_profile_photo", path ?? string.Empty);
    }
}
