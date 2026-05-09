### Automation & Scroll Reliability
This update implements a major optimization for the TikTok Direct Message scanning engine, specifically targeting virtualized list recycling and search efficiency.
- **Virtualized List Navigation**: Refined scrolling mechanics to use incremental offsets (`clientHeight * 3`) instead of absolute jumps, ensuring no conversations are skipped during the virtualization recycling process.
- **Pre-Click Identification**: Integrated inline username extraction from chat list items, allowing the bot to validate and skip non-target users without triggering expensive page-load waits.
- **Global Scanning History**: Implemented a session-aware `checkedUsernames` registry to prevent redundant processing of previously scanned conversations.
- **Extended Search Depth**: Increased maximum scroll attempts from 5 to 50, providing robust support for accounts with high-volume message histories.

### Logic Refinements
- **Username Extraction Filter**: Enhanced the username detection logic to exclude navigation-level profile links, preventing false-positive matches with the authenticated user's own profile.
- **Scroll Movement Validation**: Added strict `scrollTop` verification to detect the true end of the conversation list more accurately.

### Requirements
- Android 7.0 (API 24) or higher
- TikTok account with Direct Messaging enabled
