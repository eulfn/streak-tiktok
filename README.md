# Feener

Feener is a fork of [TiktokStreakSaver](https://github.com/Jon2G/TiktokStreakSaver) by Jon2G, rebranded and extended with additional features, stability fixes, and a redesigned UI.
It runs as a background service on Android, sending messages to TikTok friends on a 23-hour cycle to maintain streaks.

## Changes from the original

### Rebranding
- Renamed from TiktokStreakSaver to Feener
- Changed package ID to `com.fen.loid`
- Default message changed from `Hey! Keeping our streak alive!` to `Streak`
- Custom app icon with adaptive background and dark mode variant
- Custom notification icon (PNG, replacing the original XML drawable)
- Custom splash screen with theme-aware styling

### New features
- **In-app updater** with APK download, install intent, and GitHub fallback
- **Skip Unreachable Users toggle** that continues the run when a user is not found, instead of aborting
- **Auto-disable missing users** when skip is enabled, preventing repeated failures
- **Formatted release notes** in the update dialog using a built-in Markdown-to-HTML renderer
- **Pull-to-refresh** for schedule status and friend list
- **Friend management tools**: edit display names, clear friend lists, wipe activity history
- **Import/Export** of friend configuration

### UI
- Full dark and light mode with semantic theme tokens
- Card-based layout with standardized spacing and typography
- Inter font family across all text elements
- Dynamic Android status bar color integration
- Live notification progress per friend during automation runs
- Sticky automation controls anchored to the bottom of the viewport

### Stability fixes
- Changed `StartCommandResult.Sticky` to `NotSticky` to prevent idle battery drain from automatic service restarts
- Added alarm cancellation in `RunNow` to prevent duplicate scheduled runs after a manual execution
- Guarded `ScheduleNextRun` in `CompleteService` to only re-arm when the scheduling toggle is ON
- Fixed automation chain firing multiple times on a single page load
- Fixed duplicate update popup on pull-to-refresh
- Fixed race condition in silent update check after welcome screen dismissal
- Fixed timer and handler leaks in session validation and download failure paths
- Fixed `NullReferenceException` in background service on slow networks
- Removed duplicate event handler registrations across page navigations

### Build and CI
- Automated release pipeline triggered by Git tags (`v*`)
- Signed APK output with keystore validation
- APK identity report step (package name, version code, signing fingerprint)
- Version derived from Git tag, not hardcoded in csproj
- Auto-generated release notes from commit history

## Requirements

- Android 7.0 (API 24) or higher
- TikTok account
- Internet connection

## Installation

### Download APK

1. Download the APK from the [latest release](https://github.com/eulfn/streak-tiktok/releases/latest)
2. Enable "Install from unknown sources" on your Android device
3. Install the APK

### Build from source

```bash
git clone https://github.com/eulfn/streak-tiktok.git
cd streak-tiktok/src/Feener
dotnet workload install maui-android
dotnet restore
dotnet build -f net9.0-android -c Release
```

## Usage

1. Open the app and tap "Login to TikTok"
2. Sign in to your TikTok account in the WebView
3. Add friends by tapping "Add" and entering their TikTok username
4. Set your message in the "Message to Send" field
5. Toggle scheduling ON
6. Grant permissions when prompted:
   - Battery optimization exemption
   - Exact alarm permission
   - Notification permission

## How it works

```
[App Start] -> Schedule 23hr alarm
      |
[AlarmReceiver] -> Start foreground service
      |
[StreakService] -> Load TikTok in WebView
      |
[For each friend] -> Find chat -> Send message
      |
[Complete] -> Schedule next run (if toggle ON) -> Stop service
```

## Configuration

### Interval

Default is 23 hours. Change `DefaultIntervalHours` in `Services/SettingsService.cs`:

```csharp
public const int DefaultIntervalHours = 23;
```

### Message

Configurable in the app UI. Default: `Streak`

## Project structure

```
src/Feener/
  Models/                          Data models
  Services/
    SettingsService.cs             Preferences and friend persistence
    SessionService.cs              TikTok session validation
    UpdateService.cs               In-app update check and APK download
    TikTokWebViewHelper.cs         WebView cookie and session management
  Platforms/Android/
    Services/StreakService.cs       Foreground service and automation logic
    StreakScheduler.cs              AlarmManager scheduling and manual run
    Receivers/AlarmReceiver.cs      Alarm broadcast receiver
    Receivers/BootReceiver.cs       Boot-completed receiver
  Resources/Raw/
    tiktok_automation.js           JS injected into TikTok WebView
  AboutPopupPage.xaml              Update dialog and welcome screen
  MainPage.xaml                    Main UI
  LoginPage.xaml                   TikTok login WebView
.github/workflows/
  android-release.yml              CI/CD pipeline
```

## Credits

Original project: [TiktokStreakSaver](https://github.com/Jon2G/TiktokStreakSaver) by [Jon2G](https://github.com/Jon2G)

Modified and maintained by [@eulfen](https://github.com/eulfn)

## Disclaimer

This application is for educational purposes only. Use responsibly and in accordance with TikTok's Terms of Service. The developers are not responsible for any account restrictions or bans that may result from using this application.

## License

MIT License. See [LICENSE](LICENSE).
