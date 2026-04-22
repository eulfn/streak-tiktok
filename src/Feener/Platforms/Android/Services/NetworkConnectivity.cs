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
}
