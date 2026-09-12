
const _conversationId = getOrCreateConversationId();
const _eventSource = new EventSource(`api/music/chat/sse/${_conversationId}`);

document.addEventListener("DOMContentLoaded", () => {
    setupSse();
});

window.addEventListener("beforeunload", () => {
    console.log('Closing SSE connection');
    _eventSource?.close();
});


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


function setupSse(){
    console.log(`Starting SSE connection[ConversationId: ${_conversationId}]`);

    const chatWindow = document.getElementById('chat-window');
    
    _eventSource.onmessage = (event) => {
        console.log(`SSE message received [ConversationId: ${_conversationId}]`);
        if(!event.data) return;
        const data = JSON.parse(JSON.parse(event.data));
        console.log(data);
        
        if(data.EventType == 1) // play
        {
            showPlayerWidget(chatWindow, data.Track);
            return;
        }    

        if(data.EventType == 2) // stop/pause
        {
            document.querySelectorAll('audio').forEach(a => {
                if (!a.paused) {
                    a.pause();
                }
            });

            return;
        }    

        
    };

    _eventSource.onerror = (error) => {
        console.error(`SSE error[ConversationId: ${_conversationId}]`, error);
    };

    _eventSource.onopen = () => {
        console.log(`SSE connection opened[ConversationId: ${_conversationId}]`);
    };
}


function sendMessage() {
    const input = document.getElementById('chat-input');
    const messageText = input.value.trim();
    
    if (messageText === '') return;

    const chatWindow = document.getElementById('chat-window');
    
    showSentMessage(chatWindow, input, messageText);

    // Call API
    fetch('/api/music/chat', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            message: messageText,
            conversationId: _conversationId
        })
    })
    .then(response => {
        if (!response.ok) {
            const msg = 'Sorry, network response was not OK'
            showErrorMessage(chatWindow, msg)
            throw new Error(msg);
        }
        return response.json();
    })
    .then(data => {
        // Verify conversationId
        if (data.conversationId !== _conversationId) {
            const msg = 'Conversation ID mismatch in response';
            showErrorMessage(chatWindow, msg);
            console.error(msg);
        }

        //show response msg
        showReceivedMessage(chatWindow, data.response);
    })
    .catch(error => {
        console.error('Error:', error);
        showErrorMessage(chatWindow, "Sorry, there was an error connecting to the server.");
    });
}

function showSentMessage(chatWindow, input, msg){
    // Add sent message to UI
    const sentMsg = document.createElement('div');
    sentMsg.className = 'message sent';
    sentMsg.textContent = msg;
    chatWindow.appendChild(sentMsg);
    
    // Clear input and scroll down
    input.value = '';
    chatWindow.scrollTop = chatWindow.scrollHeight;

}

function showReceivedMessage(chatWindow, msg){
    // Add received message to UI
    const receivedMsg = document.createElement('div');
    receivedMsg.className = 'message received markdown-body';
    
    // Sometimes the AI returns everything on one line. We force a newline before numbered lists to help the markdown parser.
    let formattedMessage = msg.replace(/\s+(\d+\.\s+\*\*)/g, '\n$1');
    
    // Fallback to textContent if marked is somehow not loaded, but use marked.parse if available
    if (typeof marked !== 'undefined') {
        receivedMsg.innerHTML = marked.parse(formattedMessage, { breaks: true });
    } else if (window.marked) {
        receivedMsg.innerHTML = window.marked.parse(formattedMessage, { breaks: true });
    } else {
        receivedMsg.textContent = formattedMessage;
    }

    chatWindow.appendChild(receivedMsg);
    chatWindow.scrollTop = chatWindow.scrollHeight;
}

function showErrorMessage(chatWindow, msg){
    const errorMsg = document.createElement('div');
    errorMsg.className = 'message received';
    errorMsg.textContent = msg;
    errorMsg.style.color = "red";
    chatWindow.appendChild(errorMsg);
    chatWindow.scrollTop = chatWindow.scrollHeight;
}

function showPlayerWidget(chatWindow, track){
    if (track !== null && !isNaN(track.Id)) {
        const audioWrapper = document.createElement('div');
        audioWrapper.className = 'message player';
        audioWrapper.style.padding = '10px';
        audioWrapper.style.marginTop = '10px';
        audioWrapper.style.minWidth = '300px'; 
        
        const titleEl = document.createElement('div');
        titleEl.style.color = 'black';
        titleEl.innerHTML = `<strong>${track.Title}</strong>`
        
        const audioEl = document.createElement('audio');
        audioEl.controls = true;
        audioEl.src = `/api/music/stream/${track.Id}`;
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
        
        audioWrapper.appendChild(titleEl);
        audioWrapper.appendChild(audioEl);
        chatWindow.appendChild(audioWrapper);
        
        // Auto-play the track
        audioEl.play().catch(e => console.log('Autoplay prevented by browser:', e));
    }
}

function handleKeyPress(event) {
    if (event.key === 'Enter') {
        sendMessage();
    }
}
