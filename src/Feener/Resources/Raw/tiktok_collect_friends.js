// TikTok Friend Collection Script
// Injected into the TikTok messages WebView to scrape all chat usernames.
// Communicates state via window.__feenerState, polled by C#.

(function () {
    'use strict';

    window.__feenerState = {
        status: 'initializing',
        count: 0,
        friends: [],
        error: null
    };

    var collected = {};        // lowercase username → { username, displayName }
    var maxScrollAttempts = 15;
    var scrollAttempts = 0;
    var needsClickFallback = false;

    var log = function (msg) {
        console.log('[Feener Collect] ' + msg);
    };

    var updateState = function (status) {
        var list = [];
        var keys = Object.keys(collected);
        for (var i = 0; i < keys.length; i++) {
            list.push(collected[keys[i]]);
        }
        window.__feenerState = {
            status: status,
            count: list.length,
            friends: list,
            error: null
        };
    };

    var reportError = function (msg) {
        log('ERROR: ' + msg);
        window.__feenerState.status = 'error';
        window.__feenerState.error = msg;
    };

    var reportDone = function () {
        updateState('done');
        log('Collection complete. Found ' + window.__feenerState.count + ' unique friends.');
    };

    // ── Selector reuse from tiktok_automation.js ────────────────────────────

    var findChatItems = function () {
        var selectors = [
            "[data-e2e*='chat-list-item']",
            "[data-e2e*='dm-new-conversation-item']",
            "[data-e2e*='chat-item']"
        ];
        for (var i = 0; i < selectors.length; i++) {
            try {
                var items = document.querySelectorAll(selectors[i]);
                if (items.length > 0) {
                    log('Found ' + items.length + ' items via: ' + selectors[i]);
                    return items;
                }
            } catch (e) { }
        }
        return [];
    };

    var findChatListContainer = function (items) {
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

    // ── Username extraction ─────────────────────────────────────────────────

    // Strategy 1: Direct link extraction from a chat list item (fast, no clicks)
    var extractFromItem = function (item) {
        var links = item.querySelectorAll('a[href*="/@"]');
        for (var i = 0; i < links.length; i++) {
            var href = links[i].getAttribute('href') || '';
            var match = href.match(/\/@([^\/\?\#]+)/);
            if (match && match[1]) {
                // Also try to grab a visible display name from the item
                var displayName = '';
                var nameEl = item.querySelector('[class*="UserName"], [class*="username"], [class*="NickName"], [class*="nickname"]');
                if (nameEl) {
                    displayName = (nameEl.textContent || '').trim();
                }
                // If no class-based match, try the link text itself
                if (!displayName) {
                    var linkText = (links[i].textContent || '').trim();
                    if (linkText && linkText !== '@' + match[1]) {
                        displayName = linkText;
                    }
                }
                return { username: match[1], displayName: displayName };
            }
        }
        return null;
    };

    // Strategy 2: Click the item, read username from chat header (slower fallback)
    var extractFromHeader = function () {
        var chatHeader = document.querySelector('[class*="ChatHeader"]') ||
                         document.querySelector('[class*="chatHeader"]') ||
                         document.querySelector('[class*="DivChatHeader"]');

        if (chatHeader) {
            var headerLink = chatHeader.querySelector('a[href*="/@"]');
            if (headerLink) {
                var href = headerLink.getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?\#]+)/);
                if (match && match[1]) {
                    var displayName = (headerLink.textContent || '').trim();
                    return { username: match[1], displayName: displayName || '' };
                }
            }
        }

        // Fallback: styled links outside of data-e2e containers
        var links = document.querySelectorAll('[class*="StyledLink"]');
        for (var i = 0; i < links.length; i++) {
            var link = links[i];
            var parent = link.closest('[data-e2e]');
            var parentAttr = parent ? parent.getAttribute('data-e2e') : '';
            if (!parentAttr || parentAttr === 'chat-header') {
                var href = link.getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?\#]+)/);
                if (match && match[1]) {
                    return { username: match[1], displayName: (link.textContent || '').trim() };
                }
            }
        }
        return null;
    };

    var addFriend = function (info) {
        if (!info || !info.username) return false;
        var key = info.username.toLowerCase().trim();
        if (key.length < 1 || collected[key]) return false;
        collected[key] = {
            username: info.username.trim(),
            displayName: info.displayName || ''
        };
        log('Collected: @' + info.username + (info.displayName ? ' (' + info.displayName + ')' : ''));
        return true;
    };

    // ── Direct extraction pass (no clicking) ────────────────────────────────

    var directExtractAll = function (items) {
        var foundAny = false;
        for (var i = 0; i < items.length; i++) {
            var info = extractFromItem(items[i]);
            if (info) {
                addFriend(info);
                foundAny = true;
            }
        }
        return foundAny;
    };

    // ── Click fallback pass ─────────────────────────────────────────────────

    var clickExtractAll = function (items, startIndex, callback) {
        if (startIndex >= items.length) {
            callback();
            return;
        }

        var item = items[startIndex];
        item.click();
        updateState('collecting');

        setTimeout(function () {
            var info = extractFromHeader();
            if (info) {
                addFriend(info);
            }
            updateState('collecting');
            clickExtractAll(items, startIndex + 1, callback);
        }, 800);
    };

    // ── Scroll to load more ─────────────────────────────────────────────────

    var processCurrentItems = function (callback) {
        var items = findChatItems();
        if (items.length === 0) {
            callback();
            return;
        }

        if (!needsClickFallback) {
            // Try direct extraction first
            var directWorked = directExtractAll(items);
            if (!directWorked && Object.keys(collected).length === 0) {
                // Direct extraction found nothing — switch to click fallback
                log('Direct extraction failed, switching to click fallback');
                needsClickFallback = true;
                clickExtractAll(items, 0, callback);
            } else {
                updateState('collecting');
                callback();
            }
        } else {
            // Already in click fallback mode — only click items we haven't processed
            // Mark items we've already clicked by a data attribute
            var unprocessed = [];
            for (var i = 0; i < items.length; i++) {
                if (!items[i].getAttribute('data-feener-processed')) {
                    items[i].setAttribute('data-feener-processed', '1');
                    unprocessed.push(items[i]);
                }
            }
            if (unprocessed.length > 0) {
                clickExtractAll(unprocessed, 0, callback);
            } else {
                callback();
            }
        }
    };

    var scrollAndContinue = function () {
        scrollAttempts++;
        if (scrollAttempts > maxScrollAttempts) {
            log('Max scroll attempts reached (' + maxScrollAttempts + ')');
            reportDone();
            return;
        }

        var items = findChatItems();
        var container = findChatListContainer(items);
        if (!container) {
            log('No scrollable container found — done');
            reportDone();
            return;
        }

        var prevCount = items.length;
        var prevScrollTop = container.scrollTop;
        container.scrollTop = container.scrollHeight;
        updateState('scrolling');

        log('Scrolling chat list (attempt ' + scrollAttempts + '/' + maxScrollAttempts + ')...');

        setTimeout(function () {
            var newItems = findChatItems();
            log('After scroll: ' + newItems.length + ' items (was ' + prevCount + ')');

            if (newItems.length > prevCount) {
                // New items loaded — process them
                processCurrentItems(function () {
                    scrollAndContinue();
                });
            } else if (container.scrollTop > prevScrollTop) {
                // Scroll moved but no new items yet — wait a bit more
                setTimeout(function () {
                    var retryItems = findChatItems();
                    if (retryItems.length > prevCount) {
                        processCurrentItems(function () {
                            scrollAndContinue();
                        });
                    } else {
                        // No new items after extra wait — we've reached the end
                        log('No new items after scroll — end of list');
                        reportDone();
                    }
                }, 2000);
            } else {
                // Scroll didn't move — end of list
                log('Scroll position unchanged — end of list');
                reportDone();
            }
        }, 2000);
    };

    // ── Entry point ─────────────────────────────────────────────────────────

    var init = function () {
        try {
            log('Starting friend collection...');
            updateState('collecting');

            var items = findChatItems();
            log('Initial chat items: ' + items.length);

            if (items.length === 0) {
                // Page might still be loading — retry once after 5 seconds
                log('No chat items found, retrying in 5s...');
                setTimeout(function () {
                    items = findChatItems();
                    log('Retry: ' + items.length + ' chat items');
                    if (items.length === 0) {
                        reportError('No chat items found on this page. Make sure you have DM conversations.');
                        return;
                    }
                    processCurrentItems(function () {
                        scrollAndContinue();
                    });
                }, 5000);
                return;
            }

            processCurrentItems(function () {
                scrollAndContinue();
            });
        } catch (e) {
            reportError('Unexpected error: ' + e.message);
        }
    };

    // Wait for the messages page to fully render
    setTimeout(init, 3000);
})();
