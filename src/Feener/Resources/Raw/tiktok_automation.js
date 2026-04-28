// TikTok Message Automation Script
// This script is injected into the TikTok WebView to automate messaging

(function () {
    var userName = '[UserName]';
    var message = '[Message]';
    var found = false;
    var chatIndex = 0;
    var chatItems = [];
    var maxScrollAttempts = 10;
    var scrollAttempts = 0;

    var log = function (msg) {
        if (typeof StreakApp == 'undefined') {
            console.log(msg);
            return;
        }
        StreakApp.log(msg);
    };

    var findChatItems = function () {
        // Primary and fallback selectors for TikTok's conversation list items
        var selectors = [
            "[data-e2e*='chat-list-item']",
            "[data-e2e*='dm-new-conversation-item']",
            "[data-e2e*='chat-item']",
            "[class*='ChatListItem']",
            "[class*='ConversationItem']"
        ];
        
        for (var i = 0; i < selectors.length; i++) {
            try {
                var items = document.querySelectorAll(selectors[i]);
                if (items && items.length > 0) {
                    log('Found ' + items.length + ' items via: ' + selectors[i]);
                    return items;
                }
            } catch (e) {}
        }
        return [];
    };

    var findChatListContainer = function () {
        if (chatItems.length > 0) {
            var parent = chatItems[0].parentElement;
            while (parent && parent !== document.body) {
                if (parent.scrollHeight > parent.clientHeight + 10) {
                    return parent;
                }
                parent = parent.parentElement;
            }
        }
        var candidates = document.querySelectorAll('[class*="ChatList"], [class*="chatList"], [class*="conversation-list"], [data-e2e*="conversation-list"]');
        for (var i = 0; i < candidates.length; i++) {
            if (candidates[i].scrollHeight > candidates[i].clientHeight + 10) {
                return candidates[i];
            }
        }
        return null;
    };

    var scrollAndRetry = function () {
        scrollAttempts++;
        if (scrollAttempts > maxScrollAttempts) {
            log('Max scroll attempts reached (' + maxScrollAttempts + ')');
            reportError('User not found in chat list after ' + scrollAttempts + ' scrolls');
            return;
        }
        var container = findChatListContainer();
        if (!container) {
            log('No scrollable chat container found');
            reportError('User not found in chat list');
            return;
        }
        var prevCount = chatItems.length;
        var prevScrollTop = container.scrollTop;
        container.scrollTop = container.scrollHeight;
        log('Scrolling chat list (attempt ' + scrollAttempts + '/' + maxScrollAttempts + ')...');
        
        setTimeout(function () {
            chatItems = findChatItems();
            log('After scroll: ' + chatItems.length + ' items (was ' + prevCount + ')');
            if (chatItems.length > prevCount || container.scrollTop > prevScrollTop) {
                checkNextChat();
            } else {
                setTimeout(function () {
                    chatItems = findChatItems();
                    if (chatItems.length > prevCount) {
                        checkNextChat();
                    } else {
                        log('Scroll did not move — end of list');
                        reportError('User not found in chat list');
                    }
                }, 2000);
            }
        }, 2000);
    };

    var findCurrentChatUsername = function () {
        // Find the username from the chat header (the opened conversation)
        var selectors = [
            '[class*="ChatHeader"]',
            '[class*="chatHeader"]',
            '[class*="DivChatHeader"]',
            '[data-e2e="chat-header"]'
        ];
        
        for (var i = 0; i < selectors.length; i++) {
            var header = document.querySelector(selectors[i]);
            if (header) {
                var headerLink = header.querySelector('a[href*="/@"]');
                if (headerLink) {
                    var href = headerLink.getAttribute('href') || '';
                    var match = href.match(/\/@([^\/]+)/);
                    if (match) return match[1];
                }
            }
        }
        
        // Fallback: search for profile links in the header area
        var links = document.querySelectorAll('a[href*="/@"]');
        for (var i = 0; i < links.length; i++) {
            var link = links[i];
            var parent = link.closest('[data-e2e*="item"]') || link.closest('[class*="item"]');
            // If the link is NOT inside a list item, it's likely the header
            if (!parent) {
                var href = link.getAttribute('href') || '';
                var match = href.match(/\/@([^\/]+)/);
                if (match && match[1]) return match[1];
            }
        }
        return '';
    };

    var findMessageInput = function () {
        return document.querySelector('[class*="DraftEditor-editorContainer"] [contenteditable="true"]') ||
            document.querySelector('[class*="DraftEditor-root"] [contenteditable="true"]') ||
            document.querySelector('div[contenteditable="true"][role="textbox"]') ||
            document.querySelector('div[contenteditable="true"]');
    };

    var findMessageButton = function () {
        return document.querySelector("[data-e2e*='message-button']") ||
            document.querySelector("[data-e2e*='message-send']") ||
            document.querySelector("[data-e2e*='send']") ||
            document.querySelector('button[type="submit"]');
    };

    var isTargetUser = function (currentUsername) {
        if (!currentUsername) return false;
        var target = userName.toLowerCase();
        var current = currentUsername.toLowerCase();
        return current === target || current.includes(target);
    };

    var findDraftEditor = function (messageInput) {
        var key = Object.keys(messageInput).find(function(k) {
            return k.startsWith('__reactFiber$') || k.startsWith('__reactInternalInstance$');
        });
        if (!key) return null;
        var fiber = messageInput[key];
        var current = fiber;
        while (current) {
            if (current.stateNode && current.stateNode.editor) return current.stateNode;
            current = current.return;
        }
        return null;
    };

    var typeMessage = function (messageInput, callback) {
        log('Starting typeMessage...');
        var draftEditor = findDraftEditor(messageInput);
        if (draftEditor) {
            log('Found Draft.js editor, using _onPaste');
            draftEditor.focus();
            setTimeout(function () {
                var dataTransfer = new DataTransfer();
                dataTransfer.setData('text/plain', message);
                var pasteEvent = new ClipboardEvent('paste', {
                    bubbles: true, cancelable: true, clipboardData: dataTransfer
                });
                try {
                    draftEditor._onPaste(pasteEvent);
                } catch (e) {
                    log('_onPaste error: ' + e.message);
                }
                setTimeout(callback, 300);
            }, 200);
        } else {
            log('Draft.js not found, using execCommand');
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
                setTimeout(callback, 300);
            }, 200);
        }
    };

    var sendMessage = function (messageInput) {
        var sendBtn = findMessageButton();
        if (sendBtn) {
            log('Clicking send button...');
            sendBtn.dispatchEvent(new Event('click', { bubbles: true }));
        } else {
            log('No send button found, pressing Enter...');
            messageInput.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, cancelable: true }));
            messageInput.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true }));
        }
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

    var checkNextChat = function () {
        if (found) return;

        log('Scanning ' + chatItems.length + ' visible items for @' + userName);
        
        // Optimized: Search for the target username directly within the list item text
        var targetIdx = -1;
        var cleanTarget = userName.toLowerCase();
        for (var i = 0; i < chatItems.length; i++) {
            if (chatItems[i].textContent.toLowerCase().includes(cleanTarget)) {
                targetIdx = i;
                break;
            }
        }

        if (targetIdx !== -1) {
            log('Found target user in list at index ' + (targetIdx + 1) + ', clicking...');
            chatItems[targetIdx].click();
            
            setTimeout(function () {
                var current = findCurrentChatUsername();
                log('Opened chat: ' + current);
                
                if (isTargetUser(current)) {
                    found = true;
                    var input = findMessageInput();
                    if (input) {
                        typeMessage(input, function () {
                            sendMessage(input);
                            setTimeout(reportSuccess, 1000);
                        });
                    } else {
                        reportError('Message input not found');
                    }
                } else {
                    log('Username mismatch (found ' + current + '), continuing search...');
                    // Update index and resume if multiple users match the substring
                    chatIndex = targetIdx + 1;
                    if (chatIndex < chatItems.length) {
                        checkNextChat();
                    } else {
                        scrollAndRetry();
                    }
                }
            }, 2000);
            return;
        }

        log('User not found in visible list, trying scroll...');
        scrollAndRetry();
    };

    var dumpPageDiagnostics = function () {
        log('=== PAGE DIAGNOSTICS ===');
        log('URL: ' + window.location.href);
        log('Title: ' + document.title);
        
        var listArea = document.querySelector("[data-e2e*='dm-new-conversation-list']") || 
                       document.querySelector("[class*='ChatList']") ||
                       document.querySelector("[class*='ConversationList']");
                       
        if (listArea) {
            var children = listArea.querySelectorAll("[data-e2e*='item']") || listArea.children;
            log('Conversation list container found. Visible child count: ' + (children ? children.length : 0));
        } else {
            log('Conversation list container NOT found via standard selectors.');
        }

        var allE2e = document.querySelectorAll('[data-e2e]');
        log('Total data-e2e elements on page: ' + allE2e.length);
        log('=== END DIAGNOSTICS ===');
    };

    var init = function () {
        try {
            if (userName.startsWith('@')) userName = userName.substring(1);
            log('Starting automation for: ' + userName);

            var attempt = 0;
            var maxInitAttempts = 15; // Wait up to 30 seconds for hydration
            
            var pollForLoad = function () {
                attempt++;
                
                // 1. Check if target chat is already open (fast-path)
                var current = findCurrentChatUsername();
                if (isTargetUser(current)) {
                    log('Target chat is already open: ' + current);
                    found = true;
                    var input = findMessageInput();
                    if (input) {
                        typeMessage(input, function () {
                            sendMessage(input);
                            setTimeout(reportSuccess, 1000);
                        });
                    } else {
                        reportError('Message input not found');
                    }
                    return;
                }

                // 2. Poll for the conversation list items
                chatItems = findChatItems();
                if (chatItems && chatItems.length > 0) {
                    log('Chat list hydrated with ' + chatItems.length + ' items.');
                    checkNextChat();
                } else if (attempt < maxInitAttempts) {
                    // Still loading/hydrating - be patient
                    if (attempt % 5 === 0) log('Waiting for chat list to hydrate... (' + (attempt * 2) + 's)');
                    setTimeout(pollForLoad, 2000);
                } else {
                    // Exhausted all attempts
                    dumpPageDiagnostics();
                    reportError('Chat list failed to load/hydrate after ' + (attempt * 2) + 's');
                }
            };

            // Start polling
            pollForLoad();
        } catch (e) {
            log('Error in init: ' + e.message);
            reportError('Initialization error: ' + e.message);
        }
    };

    // Begin automation
    init();
})();
