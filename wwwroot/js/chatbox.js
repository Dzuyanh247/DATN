(() => {
    const root = document.getElementById('kk-support-chat');
    if (!root) return;

    const launcher = document.getElementById('kk-chat-launcher');
    const panel = document.getElementById('kk-chat-panel');
    const closeButton = document.getElementById('kk-chat-close');
    const startForm = document.getElementById('kk-chat-start-form');
    const conversationView = document.getElementById('kk-chat-conversation');
    const welcome = document.getElementById('kk-chat-welcome');
    const messageForm = document.getElementById('kk-chat-message-form');
    const messageInput = document.getElementById('kk-chat-message');
    const messagesElement = document.getElementById('kk-chat-messages');
    const errorElement = document.getElementById('kk-chat-start-error');
    const connectionElement = document.getElementById('kk-chat-connection');
    const closedElement = document.getElementById('kk-chat-closed');
    const unreadElement = document.getElementById('kk-chat-unread');
    const storageKey = root.dataset.storageKey;
    const createUrl = root.dataset.createUrl;
    const messagesUrlTemplate = root.dataset.messagesUrlTemplate;
    const sendUrlTemplate = root.dataset.sendUrlTemplate;
    const systemMessageUrlTemplate = root.dataset.systemMessageUrlTemplate;
    const csrf = document.querySelector('#kk-chat-antiforgery input[name="__RequestVerificationToken"]')?.value || '';
    let session = readSession();
    let connection;
    let unread = 0;

    function readSession() {
        try { return JSON.parse(localStorage.getItem(storageKey) || 'null'); } catch { return null; }
    }
    function saveSession(value) {
        session = value;
        if (value) localStorage.setItem(storageKey, JSON.stringify(value));
        else localStorage.removeItem(storageKey);
    }
    function scrollToLatest() {
        requestAnimationFrame(() => { messagesElement.scrollTop = messagesElement.scrollHeight; });
    }
    function setOpen(open) {
        root.classList.toggle('is-open', open);
        panel.setAttribute('aria-hidden', String(!open));
        launcher.setAttribute('aria-expanded', String(open));
        if (open) {
            unread = 0;
            updateUnread();
            scrollToLatest();
        }
    }
    function updateUnread() {
        unreadElement.textContent = String(unread);
        unreadElement.classList.toggle('d-none', unread === 0);
    }
    function setConnection(online) {
        connectionElement.classList.toggle('is-online', online);
        connectionElement.innerHTML = `<span></span> ${online ? 'Đang trực tuyến' : 'Shop sẽ phản hồi sớm'}`;
    }
    function formatTime(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }
    function addMessage(message) {
        if (message.id && messagesElement.querySelector(`[data-chat-message-id="${message.id}"]`)) return;
        const item = document.createElement('div');
        const customer = String(message.senderType).toLowerCase() === 'customer';
        item.className = `kk-chat-message ${customer ? 'customer' : 'admin'}`;
        if (message.id) item.dataset.chatMessageId = message.id;
        const bubble = document.createElement('div');
        bubble.className = 'kk-chat-bubble';
        bubble.textContent = message.message;
        const time = document.createElement('div');
        time.className = 'kk-chat-time';
        time.textContent = `${customer ? 'Bạn' : 'KKSHOP'} • ${formatTime(message.createdAt)}`;
        item.append(bubble, time);
        messagesElement.appendChild(item);
        scrollToLatest();
    }
    function showConversation(status) {
        welcome.classList.add('d-none');
        startForm.classList.add('d-none');
        conversationView.classList.remove('d-none');
        const closed = String(status).toLowerCase() === 'closed';
        closedElement.classList.toggle('d-none', !closed);
        messageForm.classList.toggle('d-none', closed);
    }
    function resetConversation() {
        saveSession(null);
        messagesElement.replaceChildren();
        conversationView.classList.add('d-none');
        welcome.classList.remove('d-none');
        startForm.classList.remove('d-none');
        closedElement.classList.add('d-none');
        messageForm.classList.remove('d-none');
    }
    function conversationUrl(template, conversationId) {
        return template.replace('987654321', encodeURIComponent(conversationId));
    }
    async function jsonFetch(url, options = {}) {
        let response;
        try {
            response = await fetch(url, {
                ...options,
                credentials: 'same-origin',
                headers: { Accept: 'application/json', 'Content-Type': 'application/json', RequestVerificationToken: csrf, ...(options.headers || {}) }
            });
        } catch (error) {
            console.error('[SupportChat] Network error', { url, error });
            throw new Error('Không thể kết nối máy chủ. Vui lòng kiểm tra mạng và thử lại.');
        }

        const contentType = response.headers.get('content-type') || '';
        const responseText = await response.text();
        let data = null;
        if (contentType.toLowerCase().includes('application/json')) {
            try {
                data = responseText ? JSON.parse(responseText) : null;
            } catch (error) {
                console.error('[SupportChat] Invalid JSON response', { url, status: response.status, contentType, responseText, error });
            }
        } else {
            console.error('[SupportChat] Expected JSON but received another content type', {
                url,
                status: response.status,
                contentType,
                responseText
            });
        }

        if (!data || typeof data !== 'object') {
            throw new Error(response.ok
                ? 'Máy chủ trả về dữ liệu không đúng định dạng. Vui lòng thử lại.'
                : `Máy chủ không thể xử lý yêu cầu (${response.status}). Vui lòng thử lại.`);
        }
        if (!response.ok || data.success === false) {
            const serverError = data.error || data.message || `Không thể xử lý yêu cầu (${response.status}).`;
            console.error('[SupportChat] Server rejected request', { url, status: response.status, data });
            throw new Error(serverError);
        }
        return data;
    }
    async function loadMessages() {
        if (!session?.conversationId || !session?.accessToken) return resetConversation();
        try {
            const data = await jsonFetch(`${conversationUrl(messagesUrlTemplate, session.conversationId)}?accessToken=${encodeURIComponent(session.accessToken)}`);
            messagesElement.replaceChildren();
            data.messages.forEach(addMessage);
            showConversation(data.status);
        } catch {
            resetConversation();
        }
    }
    async function addCloseMessage() {
        if (!session?.conversationId || !session?.accessToken || !systemMessageUrlTemplate) return;
        const closeMessageKey = `kkshop_chat_close_message_sent_${session.conversationId}`;
        if (localStorage.getItem(closeMessageKey) === 'true') return;

        localStorage.setItem(closeMessageKey, 'true');
        try {
            const data = await jsonFetch(conversationUrl(systemMessageUrlTemplate, session.conversationId), {
                method: 'POST',
                body: JSON.stringify({ accessToken: session.accessToken, messageType: 'close' })
            });
            if (data.message) addMessage(data.message);
        } catch (error) {
            localStorage.removeItem(closeMessageKey);
            console.error('[SupportChat] Could not add close message', error);
        }
    }

    async function connectRealtime() {
        if (!window.signalR || !session?.conversationId) { setConnection(false); return; }
        if (connection) await connection.stop().catch(() => {});
        connection = new signalR.HubConnectionBuilder().withUrl('/hubs/support-chat').withAutomaticReconnect().build();
        connection.on('MessageReceived', (conversationId, message) => {
            if (Number(conversationId) !== Number(session?.conversationId)) return;
            addMessage(message);
            if (!root.classList.contains('is-open')) { unread += 1; updateUnread(); }
        });
        connection.on('ConversationClosed', conversationId => {
            if (Number(conversationId) !== Number(session?.conversationId)) return;
            showConversation('Closed');
        });
        connection.onreconnecting(() => setConnection(false));
        connection.onreconnected(async () => { setConnection(true); await connection.invoke('JoinConversation', session.conversationId, session.accessToken); });
        connection.onclose(() => setConnection(false));
        try {
            await connection.start();
            const joined = await connection.invoke('JoinConversation', session.conversationId, session.accessToken);
            setConnection(joined === true);
        } catch { setConnection(false); }
    }

    launcher.addEventListener('click', () => setOpen(true));
    closeButton.addEventListener('click', () => {
        setOpen(false);
        void addCloseMessage();
    });
    document.getElementById('kk-chat-new-conversation').addEventListener('click', resetConversation);
    startForm.addEventListener('submit', async event => {
        event.preventDefault();
        errorElement.textContent = '';
        const button = startForm.querySelector('button[type="submit"]');
        const payload = {
            name: document.getElementById('kk-chat-name')?.value.trim() || root.dataset.name || null,
            email: document.getElementById('kk-chat-email')?.value.trim() || root.dataset.email || null,
            phone: document.getElementById('kk-chat-phone')?.value.trim() || null,
            message: document.getElementById('kk-chat-first-message').value.trim()
        };
        if (!payload.message) { errorElement.textContent = 'Vui lòng nhập nội dung cần hỗ trợ.'; return; }
        if (root.dataset.authenticated !== 'true' && (!payload.name || (!payload.email && !payload.phone))) {
            errorElement.textContent = 'Vui lòng nhập tên và ít nhất email hoặc số điện thoại.'; return;
        }
        button.disabled = true;
        try {
            const data = await jsonFetch(createUrl, { method: 'POST', body: JSON.stringify(payload) });
            saveSession({ conversationId: data.conversationId, accessToken: data.accessToken });
            messagesElement.replaceChildren();
            (data.messages || []).forEach(addMessage);
            showConversation(data.status);
            await connectRealtime();
        } catch (error) {
            console.error('[SupportChat] Could not start conversation', error);
            errorElement.textContent = error.message;
        }
        finally { button.disabled = false; }
    });
    messageForm.addEventListener('submit', async event => {
        event.preventDefault();
        const text = messageInput.value.trim();
        if (!text || !session) return;
        const button = messageForm.querySelector('button');
        button.disabled = true;
        try {
            const data = await jsonFetch(conversationUrl(sendUrlTemplate, session.conversationId), {
                method: 'POST', body: JSON.stringify({ accessToken: session.accessToken, message: text })
            });
            addMessage(data.message);
            messageInput.value = '';
            messageInput.style.height = '';
            scrollToLatest();
        } catch (error) {
            console.error('[SupportChat] Could not send message', error);
            if (window.showGlobalToast) window.showGlobalToast(error.message, 'danger');
            else window.alert(error.message);
        }
        finally { button.disabled = false; }
    });
    messageInput.addEventListener('input', () => {
        messageInput.style.height = 'auto';
        messageInput.style.height = `${Math.min(messageInput.scrollHeight, 86)}px`;
    });
    messageInput.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); messageForm.requestSubmit(); }
    });

    if (session) { loadMessages(); connectRealtime(); } else setConnection(false);
})();
