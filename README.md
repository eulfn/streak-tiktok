# Feener

**Automatically send TikTok messages to keep your streaks alive.**

Feener is an open-source Android application that runs in the background and automatically sends messages to your TikTok friends every 23 hours, ensuring you never lose your streaks.

## Features

- **Dynamic Theming** - Full support for Light and Dark modes with native Android status bar integration.
- **Modern UI** - Clean, professional card-based interface built with standardized semantic tokens.
- **Automatic Scheduling** - Sends messages every 23 hours automatically.
- **Multiple Friends** - Configure and manage multiple friends to maintain streaks with.
- **Management Tools** - Easily edit friend display names, clear friend lists, or wipe activity history.
- **Pull-to-Refresh** - Instantly update schedule status and friend lists with a simple gesture.
- **Background Service** - Works reliably even when the application is closed.
- **Smart Notifications** - Shows progress only while sending, then disappears.
- **Boot Persistence** - Automatically reschedules after device restart.
- **Session Management** - Secure TikTok session handling with visual validation status.
- **Battery Optimized** - Integrated battery optimization management for maximum reliability.

## Requirements

- Android 7.0 (API 24) or higher
- TikTok account
- Internet connection

## Installation

### Option 1: Download APK (Recommended)

1. Go to the [Releases](../../releases) page
2. Download the latest `Feener-vX.X.X.apk`
3. Enable "Install from unknown sources" on your Android device
4. Install the APK

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/eulfn/streak-tiktok.git
cd streak-tiktok

# Build for Android
cd src/Feener
dotnet build -f net9.0-android -c Release
```

## Getting Started

1. **Open the app** and tap "Login to TikTok"
2. **Sign in** to your TikTok account in the secure WebView
3. **Add friends** by tapping "Add" and entering their TikTok username
4. **Set your message** in the "Message to Send" field
5. **Enable scheduling** by toggling the "Schedule Status" switch
6. **Grant permissions** by tapping "Permissions" and allowing:
   - Battery optimization exemption
   - Exact alarm permission
   - Notification permission

## How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                           Feener                            │
│                       Logic Flow                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [App Start] -> Schedule 23hr Alarm                         │
│        |                                                    │
│  [Every 23hrs] -> AlarmReceiver triggers                     │
│        |                                                    │
│  [StreakService] -> Start Foreground Service                 │
│        |                                                    │
│  [WebView] -> Load TikTok Messages                           │
│        |                                                    │
│  [For each friend] -> Find chat -> Send message               │
│        |                                                    │
│  [Complete] -> Schedule next run -> Stop service              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

- **.NET 9 MAUI** - Modern cross-platform framework
- **Android WebView** - TikTok web automation interface
- **AlarmManager** - Precise 23-hour background scheduling
- **Foreground Service** - Reliable background execution on Android
- **JavaScript Injection** - Interaction with TikTok web elements
- **Semantic Theming** - Centralized theme tokens for consistent design

## Project Structure

```
Feener/
├── src/Feener/
│   ├── Models/                    # Data models
│   ├── Services/                  # Business logic services
│   ├── Platforms/Android/
│   │   ├── Services/              # Android foreground service
│   │   ├── Receivers/             # Alarm & boot receivers
│   │   └── Resources/             # Android resources & themes
│   ├── Resources/                 # MAUI resources (icons, fonts, styles)
│   ├── MainPage.xaml              # Modern main UI
│   └── LoginPage.xaml             # TikTok login interface
├── .github/workflows/             # CI/CD pipelines
└── docs/                          # Documentation & screenshots
```

## Configuration

### Changing the Interval

The default interval is 23 hours. To modify, update the `DefaultIntervalHours` constant in `Services/SettingsService.cs`:

```csharp
public const int DefaultIntervalHours = 23;
```

### Custom Message

You can set any message in the app's UI. The default message is:
```
Streak
```

## Contributing

Contributions are welcome. Here is how you can help:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Setup

```bash
# Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or VS Code with C# extension
- Android SDK (API 24+)

# Install MAUI workload
dotnet workload install maui-android

# Restore and build
cd src/Feener
dotnet restore
dotnet build -f net9.0-android
```

## Disclaimer

This application is for educational purposes only. Use responsibly and in accordance with TikTok's Terms of Service. The developers are not responsible for any account restrictions or bans that may result from using this application.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

This project is built upon an existing open-source foundation originally created as TiktokStreakSaver. 

A huge thank you to the original creator for providing the base implementation that made this project possible. Your work significantly accelerated development and helped shape this project. 

- Built with [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- Inspired by the need for reliable streak maintenance

---

<p align="center">
  Built with precision and reliability.
</p>
