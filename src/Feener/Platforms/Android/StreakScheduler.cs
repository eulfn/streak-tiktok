using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Feener.Platforms.Android.Receivers;
using Feener.Services;

namespace Feener.Platforms.Android;

/// <summary>
/// Manages alarm scheduling for the streak service
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class StreakScheduler
{
    private const int AlarmRequestCode = 1001;

    /// <summary>
    /// Schedule the next streak run based on settings
    /// </summary>
    public static void ScheduleNextRun(Context context)
    {
        var settingsService = new SettingsService();
        var intervalHours = settingsService.GetIntervalHours();
        var lastRun = settingsService.GetLastRunTime();

        DateTime nextRunTime;
        if (lastRun.HasValue)
        {
            nextRunTime = lastRun.Value.AddHours(intervalHours);
            // If the calculated time is in the past, schedule for now + small delay
            if (nextRunTime < DateTime.Now)
            {
                nextRunTime = DateTime.Now.AddMinutes(1);
            }
        }
        else
        {
            // First run - schedule for interval from now
            nextRunTime = DateTime.Now.AddHours(intervalHours);
        }

        ScheduleAt(context, nextRunTime);
        settingsService.SetScheduled(true);
    }

    /// <summary>
    /// Schedule a run at a specific time
    /// </summary>
    public static void ScheduleAt(Context context, DateTime triggerTime)
    {
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null) return;

        var intent = new Intent(context, typeof(AlarmReceiver));
        intent.SetAction(AlarmReceiver.ActionStreakAlarm);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            AlarmRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        if (pendingIntent == null) return;

        // Convert to milliseconds since epoch
        var triggerAtMillis = new DateTimeOffset(triggerTime).ToUnixTimeMilliseconds();

        // Use exact alarm for precise timing
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            // Android 12+ requires checking for exact alarm permission
            if (alarmManager.CanScheduleExactAlarms())
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
            else
            {
                // Fall back to inexact alarm, but request permission
                alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        {
            alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
        }


    }

    /// <summary>
    /// Cancel any scheduled alarm
    /// </summary>
    public static void CancelSchedule(Context context)
    {
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null) return;

        var intent = new Intent(context, typeof(AlarmReceiver));
        intent.SetAction(AlarmReceiver.ActionStreakAlarm);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            AlarmRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        if (pendingIntent != null)
        {
            alarmManager.Cancel(pendingIntent);
        }

        var settingsService = new SettingsService();
        settingsService.SetScheduled(false);


    }

    /// <summary>
    /// Run the service immediately (for manual trigger)
    /// </summary>
    public static void RunNow(Context context)
    {
        // Cancel any pending alarm so it cannot fire during or after this manual run
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager != null)
        {
            var alarmIntent = new Intent(context, typeof(AlarmReceiver));
            alarmIntent.SetAction(AlarmReceiver.ActionStreakAlarm);
            var pendingIntent = PendingIntent.GetBroadcast(
                context, AlarmRequestCode, alarmIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            if (pendingIntent != null)
                alarmManager.Cancel(pendingIntent);
        }

        var serviceIntent = new Intent(context, typeof(Services.StreakService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(serviceIntent);
        else
            context.StartService(serviceIntent);
    }

    /// <summary>
    /// Check if exact alarms can be scheduled (Android 12+)
    /// </summary>
    public static bool CanScheduleExactAlarms(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.S)
        {
            return true;
        }

        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        return alarmManager?.CanScheduleExactAlarms() ?? false;
    }

    /// <summary>
    /// Open settings to allow exact alarms (Android 12+)
    /// </summary>
    public static void RequestExactAlarmPermission(Context context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var intent = new Intent(Settings.ActionRequestScheduleExactAlarm);
            var packageName = context.PackageName;
            if (!string.IsNullOrEmpty(packageName))
            {
                intent.SetData(global::Android.Net.Uri.Parse($"package:{packageName}"));
            }
            intent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
    }

    /// <summary>
    /// Request to ignore battery optimizations
    /// </summary>
    public static void RequestBatteryOptimizationExemption(Context context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
            var packageName = context.PackageName;

            if (powerManager != null && packageName != null && !powerManager.IsIgnoringBatteryOptimizations(packageName))
            {
                var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(global::Android.Net.Uri.Parse($"package:{packageName}"));
                intent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
            }
        }
    }

    /// <summary>
    /// Check if app is exempted from battery optimization
    /// </summary>
    public static bool IsIgnoringBatteryOptimizations(Context context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
            var packageName = context.PackageName;

            return powerManager?.IsIgnoringBatteryOptimizations(packageName) ?? false;
        }
        return true;
    }
}









