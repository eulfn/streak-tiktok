using Android.App;
using Android.Runtime;

namespace Feener
{
    [Application]
    [Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // 1. Register network recovery listener (re-runs streak if offline -> online)
            Feener.Platforms.Android.Services.NetworkChangeMonitor.Register(this);

            // 2. Register low battery anticipator
            try
            {
                var receiver = new Feener.Platforms.Android.Receivers.BatteryLowReceiver();
                var filter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryLow);

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
                {
                    // Android 13+ requires an explicit export flag for runtime-registered receivers.
                    RegisterReceiver(receiver, filter, Android.Content.ReceiverFlags.NotExported);
                }
                else
                {
                    RegisterReceiver(receiver, filter);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register BatteryLowReceiver: {ex.Message}");
            }
        }
    }
}
