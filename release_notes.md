## What's new in this release (v2.5.0)

### Deep Architectural Overhaul

This major update completely rewrites the core logic to eliminate "Miss Logics", making the background automation perfectly reliable and predictable.

- **Attempt-Based Scheduling**: Fixed the "Death Loop" bug where a failed run would retry every 60 seconds. The app now strictly adheres to the 23-hour interval relative to the start of the last attempt, saving your battery.
- **Truth-Based Dashboards**: Eliminated the "fake 100%" progress. The dashboard now reads the actual historical evidence from the background service, honestly reporting success percentages even if a run fails halfway.
- **Data Sandbox Isolation**: Implemented a frozen "Run Context". If you change your message or delete a friend while the service is already running in the background, it will no longer corrupt the active streak.
- **Exhaustive Session Validation**: The "auto-check" session logic now uses active DOM verification (evaluating JavaScript) rather than just looking at the URL. This prevents the "Ghost Login" where the app thought it was logged in when it wasn't.
- **Hard Permission Gating**: The Run button now strictly requires Notification permissions, ensuring you always have visibility and control over the background process.
- **Reboot Resilience**: The 23-hour scheduled streak is now restored safely after your phone restarts or updates.

---

### Previous core fixes (v1.7.0)

### Fixed: In-app updates now work correctly
The release signing now always uses the permanent production keystore, so all future releases will update seamlessly without requiring an uninstall.
> **Note:** If you have a version older than v1.7.0 installed, you need to uninstall it once before installing this release.

---

### Installation
1. Download `Feener-v2.5.0.apk` below
2. Enable **Install from unknown sources** on your Android device
3. Install the APK

### Requirements
- Android 7.0 (API 24) or higher