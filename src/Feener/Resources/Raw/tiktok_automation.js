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

    var findChatItems = function () {
        // Primary selector (known working historically)
        var items = document.querySelectorAll("[data-e2e='chat-list-item']");
        if (items.length > 0 && items.length < 100) return items;

        // Fallback selectors, explicitly avoiding message-level items inside the chat
        var fallbacks = [
            // Exact data-e2e matches to avoid matching 'conversation-message' etc.
            "[data-e2e='chat-item']",
            "[data-e2e='inbox-item']",
            "[data-e2e='conversation-item']",
            "[data-e2e='conversation']", // Only exact, not *=
            // Class-based exact list children
            "ul[class*='ChatList'] > li",
            "ul[class*='chat-list'] > li",
            "ul[class*='InboxList'] > li",
            "div[class*='ChatList'] > div",
            "div[class*='InboxList'] > div",
            // Elements with generic list item classes
            "li[class*='ChatListItem']",
            "li[class*='ConversationItem']",
            "div[class*='ConversationItem']"
        ];

        for (var i = 0; i < fallbacks.length; i++) {
            try {
                var found = document.querySelectorAll(fallbacks[i]);
                // A valid chat list typically has between 1 and 50 visible items.
                // If it finds hundreds, it's matching messages in the history.
                if (found.length > 0 && found.length < 100) {
                    log('Found ' + found.length + ' items via fallback: ' + fallbacks[i]);
                    return found;
                }
            } catch (e) { }
        }
        
        return []; // Return empty so the caller triggers dumpPageState
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
        debugger;
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

    var dumpPageState = function () {
        log('=== PAGE DIAGNOSTICS ===');
        log('URL: ' + window.location.href);
        log('Title: ' + document.title);
        log('Body length: ' + (document.body ? document.body.innerHTML.length : 0));

        // Dump all data-e2e attributes on the page
        var allE2e = document.querySelectorAll('[data-e2e]');
        log('Total data-e2e elements: ' + allE2e.length);
        var e2eValues = [];
        for (var i = 0; i < Math.min(allE2e.length, 30); i++) {
            e2eValues.push(allE2e[i].getAttribute('data-e2e'));
        }
        if (e2eValues.length > 0) {
            log('data-e2e values: ' + e2eValues.join(', '));
        }

        // Check for common TikTok chat container classes
        var chatContainerSelectors = [
            '[class*="ChatList"]', '[class*="chatList"]', '[class*="chat-list"]',
            '[class*="Inbox"]', '[class*="inbox"]',
            '[class*="MessageList"]', '[class*="messageList"]',
            '[class*="Conversation"]', '[class*="conversation"]'
        ];
        for (var j = 0; j < chatContainerSelectors.length; j++) {
            var found = document.querySelectorAll(chatContainerSelectors[j]);
            if (found.length > 0) {
                log('Selector "' + chatContainerSelectors[j] + '" matched ' + found.length + ' elements');
                // Log first element's tag and class
                var first = found[0];
                log('  -> <' + first.tagName + ' class="' + (first.className || '').substring(0, 120) + '"> children=' + first.children.length);
            }
        }
        log('=== END DIAGNOSTICS ===');
    };

    var init = function () {
        try {
            if (userName.startsWith('@')) {
                userName = userName.substring(1);
            }
            log('Looking for user: ' + userName);
            setTimeout(function () {
            chatItems = findChatItems();
            log('Found ' + chatItems.length + ' chat items');

            if (chatItems.length === 0) {
                // Dump diagnostic info before giving up
                dumpPageState();

                // Retry once more after 5 additional seconds
                log('Retrying in 5 seconds...');
                setTimeout(function () {
                    chatItems = findChatItems();
                    log('Retry: Found ' + chatItems.length + ' chat items');
                    if (chatItems.length === 0) {
                        dumpPageState();
                        reportError('No chat items found');
                        return;
                    }
                    checkNextChat();
                }, 5000);
                return;
            }

            checkNextChat();
             }, 3000);

        } catch (e) {
            log('Error: ' + e.message);
            reportError('Error: ' + e.message);
        }
    };

    // Start the automation
    init();
})();
