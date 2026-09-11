// Helper to get or create conversationId
function getOrCreateConversationId() {
    let convId = sessionStorage.getItem('conversationId');
    if (!convId) {
        // Generate a simple UUID or use crypto API
        convId = crypto.randomUUID ? crypto.randomUUID() : 'conv-' + Math.random().toString(36).substr(2, 9);
        sessionStorage.setItem('conversationId', convId);
    }
    return convId;
}

function sendMessage() {
    const input = document.getElementById('chat-input');
    const messageText = input.value.trim();
    
    if (messageText === '') return;

    const chatWindow = document.getElementById('chat-window');
    
    // Add sent message to UI
    const sentMsg = document.createElement('div');
    sentMsg.className = 'message sent';
    sentMsg.textContent = messageText;
    chatWindow.appendChild(sentMsg);
    
    // Clear input and scroll down
    input.value = '';
    chatWindow.scrollTop = chatWindow.scrollHeight;

    const conversationId = getOrCreateConversationId();

    // Call API
    fetch('/api/music/chat', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            message: messageText,
            conversationId: conversationId
        })
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        // Verify conversationId
        if (data.conversationId !== conversationId) {
            console.error('Conversation ID mismatch in response');
        }

        // Deserialize inner response field and get message
        let botMessage = 'Error reading response format';
        let trackId = null;

        try {
            const innerResponse = JSON.parse(data.response);
            botMessage = innerResponse.message || data.response; // Fallback if format is slightly different
            
            // Check for valid numerical id
            if (innerResponse.id !== undefined && innerResponse.id !== null && typeof innerResponse.id === 'number') {
                trackId = innerResponse.id;
            } else if (typeof innerResponse.id === 'string' && !isNaN(parseInt(innerResponse.id))) {
                trackId = parseInt(innerResponse.id);
            }
        } catch (e) {
            console.error('Failed to parse inner response JSON:', e);
            botMessage = data.response; // Fallback to raw string if it wasn't JSON
        }

        // Add received message to UI
        const receivedMsg = document.createElement('div');
        receivedMsg.className = 'message received markdown-body';
        
        // Sometimes the AI returns everything on one line. We force a newline before numbered lists to help the markdown parser.
        let formattedMessage = botMessage.replace(/\s+(\d+\.\s+\*\*)/g, '\n$1');
        
        // Fallback to textContent if marked is somehow not loaded, but use marked.parse if available
        if (typeof marked !== 'undefined') {
            receivedMsg.innerHTML = marked.parse(formattedMessage, { breaks: true });
        } else if (window.marked) {
            receivedMsg.innerHTML = window.marked.parse(formattedMessage, { breaks: true });
        } else {
            receivedMsg.textContent = formattedMessage;
        }
        chatWindow.appendChild(receivedMsg);

        if (trackId !== null && !isNaN(trackId)) {
            const audioWrapper = document.createElement('div');
            audioWrapper.className = 'message received';
            audioWrapper.style.padding = '10px';
            audioWrapper.style.marginTop = '10px';
            audioWrapper.style.minWidth = '260px'; // Give it some breathing room
            
            const audioEl = document.createElement('audio');
            audioEl.controls = true;
            audioEl.src = `/api/music/stream/${trackId}`;
            audioEl.style.width = '100%';
            audioEl.style.display = 'block';
            audioEl.style.height = '45px'; // Ensure height is set
            audioEl.style.outline = 'none';
            
            // Add event listener to stop other audios when this one starts playing
            audioEl.addEventListener('play', () => {
                document.querySelectorAll('audio').forEach(a => {
                    if (a !== audioEl && !a.paused) {
                        a.pause();
                    }
                });
            });
            
            audioWrapper.appendChild(audioEl);
            chatWindow.appendChild(audioWrapper);
            
            // Auto-play the track
            audioEl.play().catch(e => console.log('Autoplay prevented by browser:', e));
        }
        
        chatWindow.scrollTop = chatWindow.scrollHeight;
    })
    .catch(error => {
        console.error('Error:', error);
        const errorMsg = document.createElement('div');
        errorMsg.className = 'message received';
        errorMsg.textContent = "Sorry, there was an error connecting to the server.";
        errorMsg.style.color = "red";
        chatWindow.appendChild(errorMsg);
        chatWindow.scrollTop = chatWindow.scrollHeight;
    });
}

function handleKeyPress(event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
}
