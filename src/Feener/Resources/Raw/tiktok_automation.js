// TikTok Message Automation Script
// This script is injected into the TikTok WebView to automate messaging

(function () {
    var userName = '[UserName]';
    var message = '[Message]';
    var found = false;
    var chatIndex = 0;
    var chatItems = [];
    var maxScrollAttempts = 5;
    var scrollAttempts = 0;

    var log = function (msg) {
        if (typeof StreakApp == 'undefined') {
            console.log(msg);
            return;
        }
        StreakApp.log(msg);
    };

    var findChatItems = function () {
        // Primary selector (v1.8.0 original)
        var items = document.querySelectorAll("[data-e2e*='chat-list-item']");
        if (items.length > 0) {
            log('Found ' + items.length + ' items via primary: chat-list-item');
            return items;
        }

        // Fallback selectors — TikTok periodically renames data-e2e values
        var fallbacks = [
            "[data-e2e*='dm-new-conversation-item']",
            "[data-e2e*='chat-item']"
        ];
        for (var i = 0; i < fallbacks.length; i++) {
            try {
                items = document.querySelectorAll(fallbacks[i]);
                if (items.length > 0) {
                    log('Found ' + items.length + ' items via fallback: ' + fallbacks[i]);
                    return items;
                }
            } catch (e) { }
        }

        // Nothing found
        return document.querySelectorAll("[data-e2e*='chat-list-item']");
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
        var candidates = document.querySelectorAll('[class*="ChatList"], [class*="chatList"], [class*="conversation-list"]');
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
            reportError('User not found in chat list');
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
            if (chatItems.length > prevCount) {
                checkNextChat();
            } else if (container.scrollTop > prevScrollTop) {
                setTimeout(function () {
                    chatItems = findChatItems();
                    if (chatItems.length > prevCount) {
                        checkNextChat();
                    } else {
                        scrollAndRetry();
                    }
                }, 2000);
            } else {
                log('Scroll did not move — end of list');
                reportError('User not found in chat list');
            }
        }, 2000);
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
                var match = href.match(/\/@([^\/]+)/);
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
                var match = href.match(/\/@([^\/]+)/);
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
        
        // Find Draft.js editor instance
        var draftEditor = findDraftEditor(messageInput);
        
        if (draftEditor) {
            log('Found Draft.js editor, using _onPaste method');
            
            // Focus the editor using Draft.js focus method
            draftEditor.focus();
            
            setTimeout(function () {
                // Create a paste event with DataTransfer containing our message
                var dataTransfer = new DataTransfer();
                dataTransfer.setData('text/plain', message);
                
                var pasteEvent = new ClipboardEvent('paste', {
                    bubbles: true,
                    cancelable: true,
                    clipboardData: dataTransfer
                });
                
                // Call Draft.js internal _onPaste handler directly
                try {
                    draftEditor._onPaste(pasteEvent);
                    log('_onPaste called successfully');
                } catch (e) {
                    log('_onPaste error: ' + e.message);
                }
                
                setTimeout(function () {
                    log('Content after _onPaste: "' + messageInput.textContent + '"');
                    callback();
                }, 300);
            }, 200);
        } else {
            // Fallback: try execCommand if Draft.js not found
            log('Draft.js not found, falling back to execCommand');
            
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
                log('Content after execCommand: "' + messageInput.textContent + '"');
                
                setTimeout(callback, 300);
            }, 200);
        }
    };

    var sendMessage = function (messageInput) {
        // Try to find and click send button first
        var sendBtn = document.querySelector('[data-e2e*="send"]') ||
                      document.querySelector('[data-e2e*="Send"]') ||
                      document.querySelector('button[type="submit"]');
        
        if (sendBtn) {
            log('Found send button, clicking...');
            sendBtn.dispatchEvent(new Event('click', { bubbles: true }));
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

    var searchForUserInCurrentChat = function () {
        // Get the username from the chat header (current open conversation)
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
        if (found) return;

        log('Scanning ' + chatItems.length + ' visible items for @' + userName);
        
        // Optimized: Search for the target username directly within the list item text
        var targetIdx = -1;
        var cleanTarget = userName.toLowerCase();
        for (var i = chatIndex; i < chatItems.length; i++) {
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
            if (userName.startsWith('@')) {
                userName = userName.substring(1);
            }
            log('Looking for user: ' + userName);

            // Pre-check: if the target chat is already open (burst mode repeat)
            var preCheckUsername = findCurrentChatUsername();
            if (preCheckUsername && isTargetUser(preCheckUsername)) {
                log('Target chat already open: ' + preCheckUsername);
                found = true;
                setTimeout(sendMessageViaButton, 500);
                return;
            }

            var attempt = 0;
            var maxInitAttempts = 15;
            
            var pollForLoad = function () {
                attempt++;
                chatItems = findChatItems();
                
                if (chatItems && chatItems.length > 0) {
                    log('Chat list hydrated with ' + chatItems.length + ' items.');
                    checkNextChat();
                } else if (attempt < maxInitAttempts) {
                    if (attempt % 5 === 0) log('Waiting for chat list to hydrate... (' + (attempt * 2) + 's)');
                    setTimeout(pollForLoad, 2000);
                } else {
                    dumpPageDiagnostics();
                    reportError('Chat list failed to load/hydrate after ' + (attempt * 2) + 's');
                }
            };
            
            pollForLoad();

        } catch (e) {
            log('Error: ' + e.message);
            reportError('Error: ' + e.message);
        }
    };

    // Start the automation
    init();
})();
