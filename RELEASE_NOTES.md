## What's new in this release

### 🔐 Fixed: In-app updates now work correctly
Every previous release was signed with a different temporary debug certificate, making it impossible for the Android installer to accept updates (showing *"App not installed as package conflicts with an existing package"*). The release signing now always uses the permanent production keystore, so all future releases will update seamlessly without requiring an uninstall.

> **One-time action required:** If you have a version older than v1.7.0 installed, you need to uninstall it once before installing this release. After that, all future in-app updates will work automatically.

### 🧹 Fixed: Only one APK published per release
Previous releases accidentally attached two APK files — the in-app updater would sometimes download the wrong one and show *"package is not valid"*. Now exactly one correctly named APK is published per release.

### ✨ Improved: Cleaner first-launch experience
The welcome screen on first install no longer shows the app changelog. It now shows a simple introduction to the app instead.

### ⚙️ Improved: Version number always matches release tag
The app version (shown in the About screen) is now automatically derived from the release tag during the build. Manual version bumps in the project file are no longer needed.

---

### Installation
1. Download `Feener-v{{ VERSION }}.apk` below
2. Enable **Install from unknown sources** on your Android device
3. Install the APK

### Requirements
- Android 7.0 (API 24) or higher
