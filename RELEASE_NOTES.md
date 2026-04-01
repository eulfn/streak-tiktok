## What's new in this release (v1.7.2)

### 🐛 Stability & Logic Fixes
This update focuses on deep stability improvements and resolving runtime bugs identified during static analysis:

- **🚀 Automation Reliability**: Fixed a critical bug in `StreakService` where the automation chain could fire multiple times on a single page load, potentially skipping friends or sending duplicate messages.
- **📱 UI Fixes**: Resolved an issue where the update popup would appear multiple times during pull-to-refresh.
- **🔄 Improved Startup flow**: Fixed a race condition that caused the silent update check to be skipped whenever the welcome screen was dismissed.
- **🔐 Session Validation**: Improved the reliability of the "Login to TikTok" status check by properly cleaning up background timers during rapid navigation.
- **🛠️ Crash Prevention**: Fixed a potential `NullReferenceException` in the background service that could occur on slow networks.
- **🧹 Resource Management**: Cleaned up "zombie" event handlers and orphan timers that were leaking memory on download failures.

---

### Previous core fixes (v1.7.0)

### 🔐 Fixed: In-app updates now work correctly
The release signing now always uses the permanent production keystore, so all future releases will update seamlessly without requiring an uninstall.
> **Note:** If you have a version older than v1.7.0 installed, you need to uninstall it once before installing this release.

---

### Installation
1. Download `Feener-v{{ VERSION }}.apk` below
2. Enable **Install from unknown sources** on your Android device
3. Install the APK

### Requirements
- Android 7.0 (API 24) or higher
