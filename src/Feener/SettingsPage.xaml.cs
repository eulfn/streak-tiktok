using Feener.Services;

namespace Feener;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;

    public SettingsPage()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ScheduleSwitch.IsToggled = _settingsService.IsScheduled();
        SkipUnreachableSwitch.IsToggled = _settingsService.GetSkipUnreachableUsers();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnScheduleToggled(object sender, ToggledEventArgs e)
    {
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (e.Value)
            Feener.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        else
            Feener.Platforms.Android.StreakScheduler.CancelSchedule(context);
#endif
    }

    private void OnSkipUnreachableToggled(object sender, ToggledEventArgs e)
    {
        _settingsService.SetSkipUnreachableUsers(e.Value);
    }
}
