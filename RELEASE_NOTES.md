## What's new in this release (v1.8.3)

### UI Improvements

- **Formatted Release Notes**: The update dialog now renders release notes as rich formatted text instead of displaying raw Markdown syntax. Headings, bold text, bullet lists, numbered lists, and horizontal rules are all properly rendered.
- **Theme-Aware Rendering**: Release notes automatically adapt to the system's dark or light theme with matching colors and typography.
- **Auto-Sizing Content**: The changelog area dynamically adjusts its height to fit the content without clipping or excessive whitespace.

### Build Pipeline Fix

- **Version Placeholder Substitution**: Fixed the CI workflow so the `{{ VERSION }}` placeholder in the Installation section is correctly replaced with the actual release version before publishing.

---

## What's new in this release (v1.8.2)

### Stability & Logic Fixes

* **Fixed Manual Run Conflicts**: Tapping "Run Now" now explicitly cancels any previously scheduled alarms, preventing redundant background executions.
* **Service Lifecycle Guard**: Modified the background service to only re-schedule the next run if the user's "Scheduled" toggle is currently enabled.

### Behavior Adjustments

* **Username Not Found Handling**: Confirmed that the automation correctly handles missing users by either stopping the run or skipping based on user settings, without erroneously resetting the entire processing queue.

---

## What's new in this release (v1.8.1)

### Fixed: Idle Battery Drain
- Optimized background service lifecycle by preventing unnecessary automatic restarts after task completion.
- Removed redundant WakeLock and WebView re-initialization cycles that occurred while the app was idle.

---

## What's new in this release (v1.8.0)

### Improved UI Feedback
- Updated notification logic to accurately reflect run outcome when all users fail to send.

---

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
