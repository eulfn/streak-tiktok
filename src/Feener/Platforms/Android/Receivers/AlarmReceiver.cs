using Android.App;
using Android.Content;
using Android.OS;
using Feener.Platforms.Android.Services;

namespace Feener.Platforms.Android.Receivers;

[BroadcastReceiver(Name = AppConstants.PackageName + ".Receivers.AlarmReceiver", Enabled = true, Exported = false)]
public class AlarmReceiver : BroadcastReceiver
{
    public const string ActionStreakAlarm = AppConstants.ActionStreakAlarm;
    public const string ActionBurstWakeup = AppConstants.PackageName + ".action.BURST_WAKEUP";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        if (intent.Action == ActionStreakAlarm || intent.Action == ActionBurstWakeup)
        {
            // Start the foreground service
            var serviceIntent = new Intent(context, typeof(StreakService));
            
            if (intent.Action == ActionBurstWakeup)
            {
                serviceIntent.PutExtra("IsBurstMode", true);
            }
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }
        }
    }
}



