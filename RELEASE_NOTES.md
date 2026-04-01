What's new in this release (v1.7.1)

Critical Bug Fixes

This update addresses several stability and reliability issues found during a deep static analysis of the codebase:

- Fixed Startup Update Check: Resolved a deadlock that caused the automatic update check on app startup to never trigger.
- Improved UI Stability: Fixed a race condition that could cause the update popup to appear multiple times or open the GitHub page multiple times on failure.
- Automation Reliability: Added guards to prevent the streak service from processing the same friend multiple times or skipping friends due to duplicate page events.
- Crash Prevention: Fixed a NullReferenceException in the background service that could occur on slow networks.
- Log Export Thread-Safety: Switched to thread-safe logging to prevent intermittent crashes when exporting system logs while the service is active.
- Resource Leak Fix: Removed an orphaned background timer that was leaking resources on download failures.

Installation
1. Download the Feener APK file below
2. Enable "Install from unknown sources" on your Android device
3. Install the APK

Requirements
- Android 7.0 (API 24) or higher
