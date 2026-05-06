// TikTok Friend Collection Script (Fast Mode)
// Instead of clicking each chat item one-by-one (slow: 1.5s per click),
// this scans the chat list DOM directly to extract usernames and display
// names from the visible items, then scrolls for more. No clicking needed.
//
// Bridge: StreakApp.onFriendFound(username, displayName)
//         StreakApp.onCollectComplete(total)
//         StreakApp.onCollectError(error)
//         StreakApp.log(msg)

(function () {
    'use strict';

    var collected = {};
    var maxScrollAttempts = 15;
    var scrollAttempts = 0;
    var lastItemCount = 0;

    var log = function (msg) {
        if (typeof StreakApp == 'undefined') {
            console.log('[Feener Collect] ' + msg);
            return;
        }
        StreakApp.log('[COLLECT] ' + msg);
    };

    // ── Selectors (from tiktok_automation.js) ───────────────────────────────

    var findChatItems = function () {
        var items = document.querySelectorAll("[data-e2e*='chat-list-item']");
        if (items.length > 0) return items;
        var fallbacks = [
            "[data-e2e*='dm-new-conversation-item']",
            "[data-e2e*='chat-item']"
        ];
        for (var i = 0; i < fallbacks.length; i++) {
            try {
                items = document.querySelectorAll(fallbacks[i]);
                if (items.length > 0) return items;
            } catch (e) { }
        }
        return document.querySelectorAll("[data-e2e*='chat-list-item']");
    };

    var findChatListContainer = function () {
        var items = findChatItems();
        if (items.length > 0) {
            var parent = items[0].parentElement;
            while (parent && parent !== document.body) {
                if (parent.scrollHeight > parent.clientHeight + 10) {
                    return parent;
                }
                parent = parent.parentElement;
            }
        }
        var candidates = document.querySelectorAll('[class*="ChatList"], [class*="chatList"], [class*="conversation-list"]');
        for (var i = 0; i < candidates.length; i++) {
            if (candidates[i].scrollHeight > candidates[i].clientHeight + 10) {
                return candidates[i];
            }
        }
        return null;
    };

    var dumpPageDiagnostics = function () {
        log('=== PAGE DIAGNOSTICS ===');
        log('URL: ' + window.location.href);
        log('Title: ' + document.title);
        var allE2e = document.querySelectorAll('[data-e2e]');
        var uniqueVals = {};
        for (var i = 0; i < allE2e.length; i++) {
            var val = allE2e[i].getAttribute('data-e2e');
            if (val) uniqueVals[val] = true;
        }
        var keys = Object.keys(uniqueVals);
        log('Total data-e2e: ' + allE2e.length + ', Unique: ' + keys.length);
        log('Attributes: ' + keys.join(', '));
        log('=== END DIAGNOSTICS ===');
    };

    // ── Fast extraction: scan DOM directly, no clicking ─────────────────────

    var extractFromItem = function (item) {
        // 1. Find the profile link (/@username) within this chat list item
        var link = item.querySelector('a[href*="/@"]');
        if (!link) return null;

        var href = link.getAttribute('href') || '';
        var match = href.match(/\/@([^\/\?]+)/);
        if (!match || !match[1]) return null;

        var username = match[1].trim();

        // 2. Extract display name from the item
        //    The display name is usually the most prominent text near the profile link.
        //    Strategy: look for the link text itself, or a nearby styled span/p.
        var displayName = '';

        // Try the link's own text content first (often is the display name)
        var linkText = (link.textContent || '').trim();
        if (linkText && linkText.length > 0 && linkText.indexOf('@') !== 0) {
            displayName = linkText;
        }

        // If link text is empty or is the @username, search for name-like elements
        if (!displayName || displayName.toLowerCase() === username.toLowerCase()) {
            // Look for elements that commonly hold the display name
            var nameSelectors = [
                '[class*="Name"]', '[class*="name"]',
                '[class*="Nickname"]', '[class*="nickname"]',
                'p', 'span'
            ];
            for (var i = 0; i < nameSelectors.length; i++) {
                var els = item.querySelectorAll(nameSelectors[i]);
                for (var j = 0; j < els.length; j++) {
                    var text = (els[j].textContent || '').trim();
                    // Skip if empty, is a timestamp, is a message preview, or is @username
                    if (text.length > 0 && text.length < 50 &&
                        text.indexOf('@') !== 0 &&
                        !text.match(/^\d/) &&
                        !text.match(/^(yesterday|today|just now|\d+[smhd])/i) &&
                        text.toLowerCase() !== username.toLowerCase()) {
                        displayName = text;
                        break;
                    }
                }
                if (displayName) break;
            }
        }

        return { username: username, displayName: displayName || '' };
    };

    var addFriend = function (username, displayName) {
        if (!username) return false;
        var key = username.toLowerCase().trim();
        if (key.length < 1 || collected[key]) return false;
        collected[key] = { username: username.trim(), displayName: displayName || '' };
        log('Collected: @' + username + ' (' + (displayName || 'no name') + ') [' + Object.keys(collected).length + ' total]');
        if (typeof StreakApp !== 'undefined') {
            StreakApp.onFriendFound(username.trim(), displayName || '');
        }
        return true;
    };

    // ── Main collection loop: scan visible items, then scroll ───────────────

    var scanVisibleItems = function () {
        var items = findChatItems();
        log('Scanning ' + items.length + ' chat items...');

        var newFound = 0;
        for (var i = 0; i < items.length; i++) {
            var result = extractFromItem(items[i]);
            if (result && addFriend(result.username, result.displayName)) {
                newFound++;
            }
        }

        log('Scan complete: ' + newFound + ' new, ' + Object.keys(collected).length + ' total');
        lastItemCount = items.length;

        // Try scrolling for more
        scrollForMore();
    };

    var scrollForMore = function () {
        scrollAttempts++;
        if (scrollAttempts > maxScrollAttempts) {
            log('Max scroll attempts reached');
            reportDone();
            return;
        }

        var container = findChatListContainer();
        if (!container) {
            log('No scrollable container found');
            reportDone();
            return;
        }

        var prevScrollTop = container.scrollTop;
        container.scrollTop = container.scrollHeight;

        log('Scrolling (attempt ' + scrollAttempts + '/' + maxScrollAttempts + ')...');

        setTimeout(function () {
            var items = findChatItems();
            log('After scroll: ' + items.length + ' items (was ' + lastItemCount + ')');

            if (items.length > lastItemCount) {
                // New items loaded — scan them
                scanVisibleItems();
            } else if (container.scrollTop > prevScrollTop) {
                // Scroll moved but no new items yet — wait a bit
                setTimeout(function () {
                    var items2 = findChatItems();
                    if (items2.length > lastItemCount) {
                        scanVisibleItems();
                    } else {
                        scrollForMore();
                    }
                }, 1500);
            } else {
                // Scroll didn't move — end of list
                log('Scroll position unchanged — end of list');
                reportDone();
            }
        }, 1500);
    };

    var reportDone = function () {
        var total = Object.keys(collected).length;
        log('Collection complete: ' + total + ' unique friends');
        if (typeof StreakApp !== 'undefined') {
            StreakApp.onCollectComplete(total);
        }
    };

    var reportError = function (msg) {
        log('ERROR: ' + msg);
        if (typeof StreakApp !== 'undefined') {
            StreakApp.onCollectError(msg);
        }
    };

    // ── Entry point ─────────────────────────────────────────────────────────

    var init = function () {
        try {
            log('Starting fast friend collection...');
            log('URL: ' + window.location.href);

            if (window.location.href.toLowerCase().indexOf('/login') !== -1) {
                reportError('Not logged in. Please log in to TikTok first.');
                return;
            }

            // Wait for SPA to render
            setTimeout(function () {
                var items = findChatItems();
                log('Initial scan: ' + items.length + ' chat items');

                if (items.length === 0) {
                    dumpPageDiagnostics();
                    log('Retrying in 5 seconds...');
                    setTimeout(function () {
                        items = findChatItems();
                        if (items.length === 0) {
                            dumpPageDiagnostics();
                            reportError('No chat items found. Make sure you have DM conversations.');
                            return;
                        }
                        scanVisibleItems();
                    }, 5000);
                    return;
                }

                scanVisibleItems();
            }, 3000);

        } catch (e) {
            reportError('Unexpected error: ' + e.message);
        }
    };

    init();
})();
