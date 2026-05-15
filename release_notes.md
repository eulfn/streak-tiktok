### Added

- **Self-Healing Retry Architecture**: Implemented a robust retry mechanism that automatically reschedules failed streak runs within a defined budget before falling back to the standard daily schedule.
- **Event-Driven Network Recovery**: Introduced a specialized network listener that triggers recovery runs immediately upon restoration of Wi-Fi or cellular connectivity if the previous attempt failed due to an offline state.
- **Low Battery Anticipation Trigger**: Added a proactive execution listener that fires today's streak ahead of schedule if the Android system broadcasts a low battery warning, ensuring delivery before power loss.

### Improved

- **Service Lifecycle Hardening**: Upgraded the core background service to utilize `StartCommandResult.Sticky`, ensuring the Android memory manager will automatically recreate and resume the service if terminated prematurely.
- **Mode-Dependent Power Management**: Refactored WakeLock allocation to be context-aware, applying strict 30-minute locks for normal background runs while preserving extended allocations exclusively for intensive operations.
- **Notification Visibility**: Created a dedicated, high-importance notification channel specifically for completion alerts, separating them from the low-priority background execution channel.
