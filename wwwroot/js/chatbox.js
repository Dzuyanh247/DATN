(() => {
    const root = document.getElementById('kk-support-chat');
    if (!root) return;

    const launcher = document.getElementById('kk-chat-launcher');
    const panel = document.getElementById('kk-chat-panel');
    const closeButton = document.getElementById('kk-chat-close');
    const startForm = document.getElementById('kk-chat-start-form');
    const conversationView = document.getElementById('kk-chat-conversation');
    const messageForm = document.getElementById('kk-chat-message-form');
    const messageInput = document.getElementById('kk-chat-message');
    const messagesElement = document.getElementById('kk-chat-messages');
    const errorElement = document.getElementById('kk-chat-start-error');
    const sendErrorElement = document.getElementById('kk-chat-send-error');
    const firstMessageInput = document.getElementById('kk-chat-first-message');
    const startSubmit = document.getElementById('kk-chat-start-submit');
    const startSubmitLabel = startSubmit?.querySelector('span');
    const quickQuestions = (() => {
        try { return JSON.parse(document.getElementById('kk-chat-quick-questions')?.textContent || '[]'); }
        catch (error) { console.error('[SupportChat] Invalid quick question configuration', error); return []; }
    })();
    const connectionElement = document.getElementById('kk-chat-connection');
    const closedElement = document.getElementById('kk-chat-closed');
    const unreadElement = document.getElementById('kk-chat-unread');
    const storageKey = root.dataset.storageKey;
    const createUrl = root.dataset.createUrl;
    const messagesUrlTemplate = root.dataset.messagesUrlTemplate;
    const sendUrlTemplate = root.dataset.sendUrlTemplate;
    const systemMessageUrlTemplate = root.dataset.systemMessageUrlTemplate;
    const quickActionUrl = root.dataset.quickActionUrl;
    const guestIdKey = root.dataset.guestIdKey || 'kkshop-support-guest-id';
    let guestId = localStorage.getItem(guestIdKey);
    if (!guestId) { guestId = (window.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`).replace(/[^a-zA-Z0-9-]/g, '').slice(0, 64); localStorage.setItem(guestIdKey, guestId); }
    const csrf = document.querySelector('#kk-chat-antiforgery input[name="__RequestVerificationToken"]')?.value || '';
    let session = readSession();
    let connection;
    let unread = 0;
    let pendingStartAction = null;
    let quickActionPending = false;

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
        connectionElement.innerHTML = `<span></span> ${online ? 'AI tư vấn tự động' : 'Shop sẽ phản hồi sớm'}`;
    }
    function formatTime(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }
    function addMessage(message) {
        const existing = message.id && messagesElement.querySelector(`[data-chat-message-id="${message.id}"]`);
        if (existing) return existing;
        const cluster = document.createElement('div');
        cluster.className = 'kk-chat-response-cluster';
        const item = document.createElement('div');
        const type = String(message.senderType).toLowerCase();
        const customer = type === 'customer';
        const system = message.isSystem === true || type === 'system';
        item.className = `kk-chat-message ${system ? 'shop' : (customer ? 'customer' : 'admin')}`;
        if (message.id) item.dataset.chatMessageId = message.id;
        const bubble = document.createElement('div');
        bubble.className = 'kk-chat-bubble';
        bubble.textContent = message.message;
        const time = document.createElement('div');
        time.className = 'kk-chat-time';
        const sender = system ? 'KKSHOP' : (customer ? 'Bạn' : (message.displaySenderName || message.senderName || 'KKSHOP'));
        time.textContent = `${sender}${formatTime(message.createdAt) ? ` • ${formatTime(message.createdAt)}` : ''}`;
        item.append(bubble, time);
        cluster.appendChild(item);
        renderMessageMetadata(message.metadata, cluster);
        messagesElement.appendChild(cluster);
        scrollToLatest();
        return cluster;
    }
    function showConversation(status) {
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
        startForm.classList.remove('d-none');
        closedElement.classList.add('d-none');
        messageForm.classList.remove('d-none');
    }
    function renderAutomation(data) {
        const messages = data?.messages || [];
        messages.forEach((message, index) => {
            if (index === messages.length - 1) {
                message.metadata = mergeMetadata(message.metadata, {
                    cards: data?.cards,
                    messageActions: data?.messageActions,
                    quickReplies: data?.quickReplies
                });
            }
            addMessage(message);
        });
        scrollToLatest();
    }
    function metadataValue(metadata, camelName, pascalName) {
        return metadata?.[camelName] ?? metadata?.[pascalName];
    }
    function normalizeMetadataValue(value) {
        if (Array.isArray(value)) return value.map(normalizeMetadataValue);
        if (!value || typeof value !== 'object') return value;
        return Object.fromEntries(Object.entries(value).map(([key, item]) => [
            key.charAt(0).toLowerCase() + key.slice(1),
            normalizeMetadataValue(item)
        ]));
    }
    function mergeMetadata(metadata, fallback) {
        metadata = normalizeMetadataValue(metadata);
        const normalized = {
            type: metadataValue(metadata, 'type', 'Type') ?? metadataValue(metadata, 'messageType', 'MessageType') ?? 'text',
            cards: metadataValue(metadata, 'cards', 'Cards') || [],
            messageActions: metadataValue(metadata, 'messageActions', 'MessageActions') || [],
            quickReplies: metadataValue(metadata, 'quickReplies', 'QuickReplies') || []
        };
        if (!normalized.cards.length) normalized.cards = fallback?.cards || [];
        if (!normalized.messageActions.length) normalized.messageActions = fallback?.messageActions || [];
        if (!normalized.quickReplies.length) normalized.quickReplies = fallback?.quickReplies || [];
        return normalized;
    }
    function renderMessageMetadata(metadata, parent) {
        if (!metadata || !parent) return;
        const normalized = mergeMetadata(metadata);
        renderCards(normalized.cards, parent);
        renderMessageActions(normalized.messageActions, parent);
        renderQuickReplies(normalized.quickReplies, parent);
    }
    function renderMessageActions(actions, parent) {
        if (!parent || actions.length === 0) return;
        const list = document.createElement('div');
        list.className = 'chat-message-actions';
        actions.forEach(action => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `chat-message-action chat-message-action--${action.style === 'secondary' ? 'secondary' : 'primary'}`;
            button.textContent = action.label;
            button.onclick = () => {
                if (action.url) {
                    if (action.target === 'newTab') window.open(action.url, '_blank', 'noopener');
                    else window.location.href = action.url;
                    return;
                }
                if (action.actionType) runQuickAction(action.actionType, action.payload);
            };
            list.append(button);
        });
        parent.append(list);
    }
    function renderQuickReplies(replies, parent) {
        if (!parent || replies.length === 0) return;
        const list = document.createElement('div');
        list.className = 'kk-chat-inline-actions';
        replies.forEach(reply => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'kk-chat-action-button';
            button.textContent = reply.label;
            button.onclick = () => reply.url ? (window.location.href = reply.url) : runQuickAction(reply.actionType, reply.payload);
            list.append(button);
        });
        parent.append(list);
    }
    function renderCards(cards, parent = messagesElement) {
        cards.forEach(card => {
            const wrapper = document.createElement('div');
            wrapper.className = `kk-chat-card kk-chat-card--${String(card.type || '').toLowerCase()}`;
            if (card.imageUrl) { const image = document.createElement('img'); image.src = card.imageUrl; image.alt = ''; wrapper.append(image); }
            const body = document.createElement('div'); body.className = 'kk-chat-card-body';
            if (card.type === 'order' && card.orderCode) {
                const header = document.createElement('div'); header.className = 'kk-chat-card-header';
                const code = document.createElement('b'); code.textContent = card.orderCode;
                const badge = document.createElement('span'); badge.className = 'kk-chat-status-badge'; badge.textContent = card.orderStatus || '';
                header.append(code, badge); body.append(header);
            }
            const title = document.createElement('strong'); title.textContent = card.title; title.title = card.title;
            body.append(title);
            if (card.subtitle) { const subtitle = document.createElement('small'); subtitle.textContent = card.subtitle; body.append(subtitle); }
            if (card.type === 'order') {
                const details = document.createElement('div'); details.className = 'kk-chat-order-details';
                if (card.paymentStatus) details.append(detailLine('Thanh toán', card.paymentStatus));
                if (card.orderedAt) details.append(detailLine('Ngày đặt', formatDateTime(card.orderedAt)));
                if (card.totalAmount != null) {
                    const total = detailLine('Tổng tiền', formatMoney(card.totalAmount));
                    total.classList.add('kk-chat-card-total'); details.append(total);
                }
                body.append(details);
            } else if (card.type === 'product') {
                const details = document.createElement('div'); details.className = 'kk-chat-order-details';
                if (card.orderCode) details.append(detailLine('Đơn hàng', card.orderCode));
                if (card.orderedAt) details.append(detailLine('Ngày mua', formatDate(card.orderedAt)));
                if (card.warrantyStatus) details.append(detailLine('Bảo hành', card.warrantyStatus));
                body.append(details);
            }
            const buttons = document.createElement('div'); buttons.className = 'kk-chat-card-actions';
            (card.actions || []).forEach(action => {
                const button = document.createElement('button'); button.type = 'button'; button.textContent = action.label;
                button.onclick = () => action.url ? (window.location.href = action.url) : runQuickAction(action.actionType, action.payload);
                buttons.append(button);
            });
            if (buttons.childElementCount) body.append(buttons);
            wrapper.append(body); parent.append(wrapper);
        });
    }
    function detailLine(label, value) {
        const line = document.createElement('span');
        const name = document.createElement('span'); name.textContent = label;
        const content = document.createElement('b'); content.textContent = value;
        line.append(name, content);
        return line;
    }
    function formatMoney(value) {
        return `${Number(value || 0).toLocaleString('vi-VN')} đ`;
    }
    function formatDate(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('vi-VN');
    }
    function formatDateTime(value) {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' });
    }
    function setBotLoading(show) {
        document.getElementById('kk-chat-bot-loading')?.remove();
        if (!show) return;
        const loading = document.createElement('div'); loading.id = 'kk-chat-bot-loading'; loading.className = 'kk-chat-bot-loading';
        loading.setAttribute('aria-label', 'KKSHOP AI đang trả lời...'); loading.title = 'KKSHOP AI đang trả lời...'; loading.innerHTML = '<small>KKSHOP AI đang trả lời...</small><span></span><span></span><span></span>'; messagesElement.append(loading); scrollToLatest();
    }
    async function runQuickAction(actionType, payload = null) {
        if (quickActionPending) return;
        if (!session) {
            pendingStartAction = { actionType, payload };
            const question = quickQuestions.find(x => x.actionType === actionType);
            firstMessageInput.value = question?.label || 'Yêu cầu hỗ trợ';
            firstMessageInput.dispatchEvent(new Event('input', { bubbles: true }));
            if (root.dataset.authenticated === 'true') startForm.requestSubmit();
            else firstMessageInput.focus();
            return;
        }
        quickActionPending = true;
        sendErrorElement.textContent = ''; setBotLoading(true);
        try {
            await new Promise(resolve => setTimeout(resolve, 350));
            const data = await jsonFetch(quickActionUrl, { method: 'POST', body: JSON.stringify({ conversationId: session.conversationId, accessToken: session.accessToken, actionType, payload }) });
            setBotLoading(false); renderAutomation(data);
        } catch (error) {
            setBotLoading(false);
            sendErrorElement.textContent = error.message;
        } finally {
            quickActionPending = false;
        }
    }
    function updateStartButton() {
        if (startSubmitLabel) startSubmitLabel.textContent = firstMessageInput.value.trim() ? 'Gửi' : 'Bắt đầu trò chuyện';
    }
    function renderQuickQuestions() {
        document.querySelectorAll('[data-chat-quick-list]').forEach(list => {
            list.replaceChildren();
            quickQuestions.forEach(question => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'kk-chat-quick-chip';
                button.textContent = question.label;
                button.addEventListener('click', () => {
                    runQuickAction(question.actionType);
                });
                list.append(button);
            });
        });
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
        return data.data ?? data;
    }
    async function loadMessages() {
        if (!session?.conversationId || !session?.accessToken) return resetConversation();
        try {
            const data = await jsonFetch(`${conversationUrl(messagesUrlTemplate, session.conversationId)}?accessToken=${encodeURIComponent(session.accessToken)}`);
            messagesElement.replaceChildren();
            data.messages.forEach(addMessage);
            showConversation(data.status);
        } catch (error) {
            console.error('[SupportChat] Could not restore conversation', error);
            resetConversation();
            errorElement.textContent = error.message;
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
            if (data?.id) addMessage(data);
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
    closeButton.addEventListener('click', () => setOpen(false));
    document.getElementById('kk-chat-new-conversation').addEventListener('click', resetConversation);
    startForm.addEventListener('submit', async event => {
        event.preventDefault();
        errorElement.textContent = '';
        const button = startForm.querySelector('button[type="submit"]');
        const payload = {
            guestId,
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
            firstMessageInput.value = '';
            updateStartButton();
            await connectRealtime();
            if (pendingStartAction) {
                const action = pendingStartAction; pendingStartAction = null;
                await runQuickAction(action.actionType, action.payload);
            }
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
        sendErrorElement.textContent = '';
        const button = messageForm.querySelector('button');
        button.disabled = true;
        try {
            const data = await jsonFetch(conversationUrl(sendUrlTemplate, session.conversationId), {
                method: 'POST', body: JSON.stringify({ accessToken: session.accessToken, message: text })
            });
            addMessage(data.customerMessage || data);
            if (data.automation) renderAutomation(data.automation);
            if (data.aiMessage) addMessage(data.aiMessage);
            messageInput.value = '';
            messageInput.style.height = '';
            messageInput.focus();
            scrollToLatest();
        } catch (error) {
            console.error('[SupportChat] Could not send message', error);
            sendErrorElement.textContent = error.message;
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
    firstMessageInput.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); startForm.requestSubmit(); }
    });
    firstMessageInput.addEventListener('input', updateStartButton);

    renderQuickQuestions();
    updateStartButton();
    if (session) { loadMessages(); connectRealtime(); } else setConnection(false);
})();
