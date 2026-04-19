// TikTok Message Automation Script
// This script is injected into the TikTok WebView to automate messaging

(function () {
    var userName = '[UserName]';
    var message = '[Message]';
    var found = false;
    var chatIndex = 0;
    var chatItems = [];

    var log = function (msg) {
        if (typeof StreakApp == 'undefined') {
            console.log(msg);
            return;
        }
        StreakApp.log(msg);
    };

    // ── SIDEBAR ITEM DETECTION ──────────────────────────────────────
    // TikTok's current DOM uses div[id^='more-acton-icon-'] inside each
    // sidebar chat entry. We target the parent wrapper of that element.
    // Fallback: scan for any sidebar-area elements containing /@username links.

    var findSidebarItems = function () {
        // Strategy 1: Stable ID-pattern elements — each sidebar item has a
        // "more actions" icon div with id="more-acton-icon-NNN".
        // The clickable wrapper is 2 levels up from that.
        var icons = document.querySelectorAll("div[id^='more-acton-icon-']");
        if (icons.length > 0) {
            log('Found ' + icons.length + ' sidebar items via id pattern');
            var wrappers = [];
            for (var i = 0; i < icons.length; i++) {
                // Walk up to the clickable wrapper (grandparent of the icon)
                var wrapper = icons[i].parentElement;
                if (wrapper && wrapper.parentElement) {
                    wrapper = wrapper.parentElement;
                }
                wrappers.push(wrapper);
            }
            return wrappers;
        }

        // Strategy 2: Legacy selector
        var items = document.querySelectorAll("[data-e2e*='chat-list-item']");
        if (items.length > 0) {
            log('Found ' + items.length + ' sidebar items via legacy selector');
            return items;
        }

        // Strategy 3: Find all sidebar links to user profiles, then return
        // their closest clickable container. Sidebar profile links have
        // href matching /@username and live inside the left panel.
        var profileLinks = document.querySelectorAll('a[href*="/@"]');
        var seen = {};
        var containers = [];
        for (var j = 0; j < profileLinks.length; j++) {
            var link = profileLinks[j];
            var href = link.getAttribute('href') || '';
            // Only process sidebar links (skip header/conversation area links)
            // by checking if the link is in the left portion of the viewport
            var rect = link.getBoundingClientRect();
            if (rect.left > 400) continue; // right side = conversation area, skip
            if (seen[href]) continue;
            seen[href] = true;
            // Walk up to find a reasonable clickable container
            var container = link.parentElement;
            for (var k = 0; k < 5; k++) {
                if (!container || !container.parentElement) break;
                // Stop when we hit something that looks like the list container
                if (container.parentElement.children.length > 3) break;
                container = container.parentElement;
            }
            containers.push(container);
        }
        if (containers.length > 0) {
            log('Found ' + containers.length + ' sidebar items via profile links');
            return containers;
        }

        log('No sidebar items found by any strategy');
        return [];
    };

    // ── EXTRACT USERNAME FROM SIDEBAR ITEM ──────────────────────────
    // Each sidebar item contains an <a href="/@username"> link.
    // Extract the username directly without needing to click.
    var getUsernameFromSidebarItem = function (item) {
        if (!item) return '';
        var links = item.querySelectorAll('a[href*="/@"]');
        for (var i = 0; i < links.length; i++) {
            var href = links[i].getAttribute('href') || '';
            var match = href.match(/\/@([^\/\?]+)/);
            if (match && match[1]) {
                return match[1];
            }
        }
        return '';
    };

    var findCurrentChatUsername = function () {
        // Find the username from the chat header (the opened conversation)
        var chatHeader = document.querySelector('[class*="ChatHeader"]') ||
                         document.querySelector('[class*="chatHeader"]') ||
                         document.querySelector('[class*="DivChatHeader"]');
        
        if (chatHeader) {
            var headerLink = chatHeader.querySelector('a[href*="/@"]');
            if (headerLink) {
                var href = headerLink.getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?]+)/);
                return match ? match[1] : '';
            }
        }
        
        // Fallback: look for links with no data-e2e parent (usually header area)
        var links = document.querySelectorAll('[class*="StyledLink"]');
        for (var i = 0; i < links.length; i++) {
            var link = links[i];
            var parent = link.closest('[data-e2e]');
            var parentAttr = parent ? parent.getAttribute('data-e2e') : '';
            
            // Skip inbox items, only look at header/none area
            if (!parentAttr || parentAttr === 'chat-header') {
                var href = link.getAttribute('href') || '';
                var match = href.match(/\/@([^\/\?]+)/);
                if (match && match[1]) {
                    return match[1];
                }
            }
        }
        
        return '';
    };

    var findMessageInput = function () {
        // Find the contenteditable editor container for Draft.js
        var editor = document.querySelector('[class*="DraftEditor-editorContainer"] [contenteditable="true"]') ||
            document.querySelector('[class*="DraftEditor-root"] [contenteditable="true"]') ||
            document.querySelector('div[contenteditable="true"][role="textbox"]') ||
            document.querySelector('div[contenteditable="true"]');

        return editor;
    };

    var findMessageButton = function () {
        return document.querySelector("[data-e2e*='message-button']") ||
            document.querySelector("[data-e2e*='message-send']");
    };

    var isTargetUser = function (currentUsername) {
        return currentUsername && currentUsername.toLowerCase().includes(userName.toLowerCase());
    };

    var findDraftEditor = function (messageInput) {
        // Find React fiber and Draft.js editor instance
        var key = Object.keys(messageInput).find(function(k) {
            return k.startsWith('__reactFiber$') || k.startsWith('__reactInternalInstance$');
        });
        
        if (!key) {
            log('React fiber not found');
            return null;
        }
        
        var fiber = messageInput[key];
        var current = fiber;
        
        while (current) {
            if (current.stateNode && current.stateNode.editor) {
                return current.stateNode;
            }
            current = current.return;
        }
        
        log('Draft editor instance not found');
        return null;
    };

    var typeMessage = function (messageInput, callback) {
        log('Starting typeMessage...');
        
        var draftEditor = findDraftEditor(messageInput);
        
        if (draftEditor) {
            log('Found Draft.js editor, using _onPaste method');
            draftEditor.focus();
            
            setTimeout(function () {
                var dataTransfer = new DataTransfer();
                dataTransfer.setData('text/plain', message);
                
                var pasteEvent = new ClipboardEvent('paste', {
                    bubbles: true,
                    cancelable: true,
                    clipboardData: dataTransfer
                });
                
                try {
                    draftEditor._onPaste(pasteEvent);
                    log('_onPaste called successfully');
                } catch (e) {
                    log('_onPaste error: ' + e.message);
                }
                
                setTimeout(function () {
                    var content = messageInput.textContent || '';
                    log('Content after _onPaste: "' + content.substring(0, 50) + '"');
                    
                    if (content.length > 0) {
                        callback();
                    } else {
                        // Fallback to execCommand
                        log('Draft.js paste failed, trying execCommand...');
                        messageInput.focus();
                        document.execCommand('insertText', false, message);
                        setTimeout(callback, 500);
                    }
                }, 500);
            }, 300);
        } else {
            // Fallback: try direct input
            log('No Draft.js editor, using direct input...');
            messageInput.click();
            messageInput.focus();
            
            setTimeout(function () {
                var selection = window.getSelection();
                var range = document.createRange();
                range.selectNodeContents(messageInput);
                range.collapse(false);
                selection.removeAllRanges();
                selection.addRange(range);
                
                document.execCommand('insertText', false, message);
                setTimeout(callback, 500);
            }, 300);
        }
    };

    var sendMessage = function (messageInput) {
        // Try clicking the send button first
        var sendButton = document.querySelector("[data-e2e*='send-button']") ||
            document.querySelector("[data-e2e*='send_msg']") ||
            document.querySelector("[data-e2e*='msg-send']") ||
            document.querySelector('button[type="submit"]');

        if (sendButton) {
            log('Found send button, clicking...');
            sendButton.click();
            return;
        }

        // Fallback: press Enter key
        log('No send button found, pressing Enter...');
        messageInput.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true,
            cancelable: true
        }));

        messageInput.dispatchEvent(new KeyboardEvent('keyup', {
            key: 'Enter',
            code: 'Enter',
            keyCode: 13,
            which: 13,
            bubbles: true
        }));
    };

    var reportSuccess = function () {
        log('Message sent to ' + userName);
        if (typeof StreakApp !== 'undefined') {
        StreakApp.onMessageSent(userName, true, '');
        }
    };

    var reportError = function (errorMessage) {
        log(errorMessage);
         if (typeof StreakApp !== 'undefined') {
        StreakApp.onMessageSent(userName, false, errorMessage);
        }
    };

    var sendMessageDirect = function () {
        var messageInput = findMessageInput();

        if (messageInput) {
            typeMessage(messageInput, function () {
                sendMessage(messageInput);
                setTimeout(reportSuccess, 1000);
            });
        } else {
            reportError('Message input not found');
        }
    };

    var sendMessageViaButton = function () {
        var messageInput = findMessageInput();
        
        if (messageInput) {
            log('Found message input, typing...');
            typeMessage(messageInput, function () {
                sendMessage(messageInput);
                setTimeout(reportSuccess, 1000);
            });
        } else {
            reportError('Message input not found');
        }
    };

    // ── SMART USER SEARCH ───────────────────────────────────────────
    // Instead of blindly clicking every item and reading the header,
    // scan each sidebar item's profile link to find the target username
    // BEFORE clicking. This is far more efficient and avoids the stale-
    // header problem that caused the old approach to fail.

    var findAndClickTargetUser = function () {
        var sidebarItems = findSidebarItems();
        log('Scanning ' + sidebarItems.length + ' sidebar items for @' + userName);

        for (var i = 0; i < sidebarItems.length; i++) {
            var itemUsername = getUsernameFromSidebarItem(sidebarItems[i]);
            if (itemUsername && isTargetUser(itemUsername)) {
                log('Found target @' + userName + ' at sidebar position ' + (i + 1) + ' (username: ' + itemUsername + ')');
                sidebarItems[i].click();

                // Wait for the conversation to load, then send the message
                setTimeout(function () {
                    // Verify via chat header that the right conversation opened
                    var headerUsername = findCurrentChatUsername();
                    log('Chat header username after click: ' + headerUsername);

                    if (headerUsername && isTargetUser(headerUsername)) {
                        found = true;
                        log('Confirmed target user: ' + headerUsername);
                        sendMessageViaButton();
                    } else {
                        // Header didn't match — might be display name mismatch
                        // but the sidebar link matched, so proceed anyway
                        log('Header username mismatch, but sidebar link matched. Proceeding...');
                        found = true;
                        sendMessageViaButton();
                    }
                }, 2000);
                return true;
            }
        }

        return false; // User not found in visible sidebar items
    };

    // ── SCROLL-AND-SCAN ─────────────────────────────────────────────
    // If the user isn't in the initially visible sidebar items,
    // scroll the chat list down and re-scan.

    var scrollAttempts = 0;
    var maxScrollAttempts = 10;

    var scrollAndScan = function () {
        if (scrollAttempts >= maxScrollAttempts) {
            log('Exhausted ' + maxScrollAttempts + ' scroll attempts');
            reportError('User not found in chat list');
            return;
        }

        scrollAttempts++;
        log('Scroll attempt ' + scrollAttempts + '/' + maxScrollAttempts);

        // Find the scrollable sidebar container
        var scrollContainer = document.querySelector('[class*="DivScrollWrapper"]') ||
                              document.querySelector('[class*="ChatList"]') ||
                              document.querySelector('[class*="chatList"]');

        if (scrollContainer) {
            scrollContainer.scrollTop += 500;
        } else {
            // Try scrolling the sidebar area by finding a scrollable parent
            var firstIcon = document.querySelector("div[id^='more-acton-icon-']");
            if (firstIcon) {
                var scrollParent = firstIcon.parentElement;
                for (var i = 0; i < 10; i++) {
                    if (!scrollParent) break;
                    if (scrollParent.scrollHeight > scrollParent.clientHeight) {
                        scrollParent.scrollTop += 500;
                        break;
                    }
                    scrollParent = scrollParent.parentElement;
                }
            }
        }

        // Wait for new items to render, then re-scan
        setTimeout(function () {
            var foundUser = findAndClickTargetUser();
            if (!foundUser) {
                scrollAndScan();
            }
        }, 1500);
    };

    // ── LEGACY CLICK-THROUGH APPROACH ───────────────────────────────
    // Kept as last-resort fallback if sidebar link scanning finds nothing.

    var findChatItems = function () {
        var items = document.querySelectorAll("[data-e2e*='chat-list-item']");
        if (items.length > 0) return items;
        // Only use conversation selector if count is reasonable (< 50)
        var convItems = document.querySelectorAll("[data-e2e*='conversation']");
        if (convItems.length > 0 && convItems.length < 50) return convItems;
        return [];
    };

    var searchForUserInCurrentChat = function () {
        var currentUsername = findCurrentChatUsername();
        log('Current chat username: ' + currentUsername);

        if (currentUsername && isTargetUser(currentUsername)) {
            found = true;
            log('Found target user: ' + currentUsername);
            sendMessageViaButton();
            return true;
        }
        
        log('Not the target user, moving to next chat...');
        return false;
    };

    var checkNextChat = function () {
        if (found || chatIndex >= chatItems.length) {
            if (!found) {
                log('User not found after checking all chats');
                reportError('User not found in chat list');
            }
            return;
        }

        var chatItem = chatItems[chatIndex];
        log('Clicking chat item ' + (chatIndex + 1) + '/' + chatItems.length);
        chatItem.click();

        setTimeout(function () {
            var userFound = searchForUserInCurrentChat();

            if (!userFound) {
                chatIndex++;
                checkNextChat();
            }
        }, 1500);
    };

    // ── ENTRY POINT ─────────────────────────────────────────────────

    var init = function () {
        try {
            if (userName.startsWith('@')) {
                userName = userName.substring(1);
            }
            log('Looking for user: ' + userName);

            setTimeout(function () {
                // Primary approach: smart sidebar scan (no blind clicking)
                var foundUser = findAndClickTargetUser();

                if (!foundUser) {
                    log('User not found in visible sidebar, scrolling...');
                    scrollAndScan();
                }
            }, 3000);

        } catch (e) {
            log('Error: ' + e.message);
            reportError('Error: ' + e.message);
        }
    };

    // Start the automation
    init();
})();
