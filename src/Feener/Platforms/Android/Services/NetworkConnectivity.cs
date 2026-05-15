using Android.Content;
using Android.Net;

namespace Feener.Platforms.Android.Services;

/// <summary>
/// Wi‑Fi / cellular reachability check before starting WebView work.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
internal static class NetworkConnectivity
{
    /// <summary>
    /// True when the active network is Wi‑Fi or cellular and reports Internet capability.
    /// </summary>
    public static bool HasWifiOrCellularInternet(Context context)
    {
        var cm = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService);
        if (cm == null) return false;

        var network = cm.ActiveNetwork;
        if (network == null) return false;

        var caps = cm.GetNetworkCapabilities(network);
        if (caps == null) return false;

        if (!caps.HasCapability(NetCapability.Internet)) return false;

        return caps.HasTransport(TransportType.Wifi) || caps.HasTransport(TransportType.Cellular);
    }

    /// <summary>
    /// Stricter variant that also demands <see cref="NetCapability.Validated"/>, meaning Android
    /// has successfully probed a captive portal or DNS and confirmed packets can actually route.
    /// </summary>
    public static bool HasWifiOrCellularValidatedInternet(Context context)
    {
        var cm = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService);
        if (cm == null) return false;

        var network = cm.ActiveNetwork;
        if (network == null) return false;

        var caps = cm.GetNetworkCapabilities(network);
        if (caps == null) return false;

        if (!caps.HasCapability(NetCapability.Internet)) return false;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !caps.HasCapability(NetCapability.Validated)) return false;

        return caps.HasTransport(TransportType.Wifi) || caps.HasTransport(TransportType.Cellular);
    }
}
