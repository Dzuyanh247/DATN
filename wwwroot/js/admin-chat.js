(() => {
    const root = document.getElementById('admin-chat');
    if (!root) return;
    const list = document.getElementById('admin-chat-list');
    const summary = document.getElementById('admin-chat-summary');
    const room = document.getElementById('admin-chat-room');
    const placeholder = document.getElementById('admin-chat-placeholder');
    const messages = document.getElementById('admin-chat-messages');
    const compose = document.getElementById('admin-chat-compose');
    const input = document.getElementById('admin-chat-message');
    const closeButton = document.getElementById('admin-chat-close-conversation');
    const csrf = document.querySelector('#admin-chat-antiforgery input[name="__RequestVerificationToken"]')?.value || '';
    let conversations = [];
    let activeId = null;
    let filter = 'all';

    async function request(url, options = {}) {
        const response = await fetch(url, {
            ...options,
            credentials: 'same-origin',
            headers: { Accept: 'application/json', 'Content-Type': 'application/json', RequestVerificationToken: csrf, ...(options.headers || {}) }
        });
        const contentType = response.headers.get('content-type') || '';
        const responseText = await response.text();
        let data = null;
        if (contentType.toLowerCase().includes('application/json')) {
            try { data = responseText ? JSON.parse(responseText) : null; }
            catch (error) { console.error('[AdminChat] Invalid JSON response', { url, status: response.status, responseText, error }); }
        } else {
            console.error('[AdminChat] Expected JSON response', { url, status: response.status, contentType, responseText });
        }
        if (!data || typeof data !== 'object') throw new Error(`Máy chủ trả về dữ liệu không hợp lệ (${response.status}).`);
        if (!response.ok || data.success === false) throw new Error(data.error || data.message || 'Không thể xử lý yêu cầu.');
        return data;
    }
    function formatDate(value, timeOnly = false) {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return '';
        if (timeOnly) return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        const today = new Date();
        return date.toDateString() === today.toDateString() ? formatDate(value, true) : date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
    }
    function visibleConversations() {
        if (filter === 'open') return conversations.filter(x => x.status.toLowerCase() === 'open');
        if (filter === 'unread') return conversations.filter(x => x.unreadCount > 0);
        return conversations;
    }
    function renderList() {
        const visible = visibleConversations();
        summary.textContent = `${conversations.filter(x => x.status.toLowerCase() === 'open').length} đang mở • ${conversations.reduce((n, x) => n + x.unreadCount, 0)} chưa đọc`;
        list.replaceChildren();
        if (!visible.length) {
            const empty = document.createElement('div'); empty.className = 'admin-chat-empty'; empty.textContent = 'Chưa có cuộc trò chuyện phù hợp.'; list.appendChild(empty); return;
        }
        visible.forEach(item => {
            const button = document.createElement('button');
            button.type = 'button'; button.className = `admin-chat-item${item.id === activeId ? ' active' : ''}`; button.dataset.id = item.id;
            const avatar = document.createElement('div'); avatar.className = 'admin-chat-item-avatar'; avatar.textContent = (item.name || 'K').trim().charAt(0).toUpperCase();
            const main = document.createElement('div'); main.className = 'admin-chat-item-main';
            const row = document.createElement('div'); row.className = 'admin-chat-item-row';
            const name = document.createElement('span'); name.className = 'admin-chat-item-name'; name.textContent = item.name || 'Khách hàng';
            const time = document.createElement('span'); time.className = 'admin-chat-item-time'; time.textContent = formatDate(item.updatedAt);
            const preview = document.createElement('div'); preview.className = 'admin-chat-item-message'; preview.textContent = item.lastMessage || 'Chưa có tin nhắn';
            row.append(name, time); main.append(row, preview); button.append(avatar, main);
            if (item.unreadCount > 0) { const badge = document.createElement('span'); badge.className = 'admin-chat-unread'; badge.textContent = item.unreadCount > 99 ? '99+' : item.unreadCount; button.appendChild(badge); }
            button.addEventListener('click', () => openConversation(item.id)); list.appendChild(button);
        });
    }
    async function loadConversations() {
        try { const data = await request('/AdminChat/conversations'); conversations = data.conversations; renderList(); }
        catch (error) { list.innerHTML = `<div class="admin-chat-empty">${error.message}</div>`; }
    }
    function addMessage(item) {
        if (messages.querySelector(`[data-message-id="${item.id}"]`)) return;
        const wrapper = document.createElement('div');
        const admin = item.senderType.toLowerCase() === 'admin';
        wrapper.className = `admin-chat-message ${admin ? 'admin' : 'customer'}`; wrapper.dataset.messageId = item.id;
        const bubble = document.createElement('div'); bubble.className = 'admin-chat-bubble'; bubble.textContent = item.message;
        const time = document.createElement('div'); time.className = 'admin-chat-message-time'; time.textContent = `${admin ? 'Admin' : 'Khách hàng'} • ${formatDate(item.createdAt, true)}`;
        wrapper.append(bubble, time); messages.appendChild(wrapper); messages.scrollTop = messages.scrollHeight;
    }
    function setRoomStatus(status) {
        const closed = status.toLowerCase() === 'closed';
        const badge = document.getElementById('admin-chat-status'); badge.textContent = closed ? 'Đã đóng' : 'Đang mở'; badge.classList.toggle('closed', closed);
        document.getElementById('admin-chat-closed').classList.toggle('d-none', !closed);
        compose.classList.toggle('d-none', closed); closeButton.classList.toggle('d-none', closed);
    }
    async function openConversation(id) {
        try {
            const data = await request(`/AdminChat/conversations/${id}/messages`); activeId = id;
            placeholder.classList.add('d-none'); room.classList.remove('d-none'); root.classList.add('room-open');
            document.getElementById('admin-chat-name').textContent = data.conversation.name || 'Khách hàng';
            document.getElementById('admin-chat-avatar').textContent = (data.conversation.name || 'K').trim().charAt(0).toUpperCase();
            document.getElementById('admin-chat-contact').textContent = [data.conversation.email, data.conversation.phone].filter(Boolean).join(' • ') || 'Chưa có thông tin liên hệ';
            setRoomStatus(data.conversation.status); messages.replaceChildren(); data.messages.forEach(addMessage);
            const found = conversations.find(x => x.id === id); if (found) found.unreadCount = 0; renderList();
        } catch (error) { window.showGlobalToast?.(error.message, 'danger'); }
    }
    compose.addEventListener('submit', async event => {
        event.preventDefault(); const text = input.value.trim(); if (!text || !activeId) return;
        const button = compose.querySelector('button'); button.disabled = true;
        try { const data = await request(`/AdminChat/conversations/${activeId}/messages`, { method: 'POST', body: JSON.stringify({ message: text }) }); addMessage(data.message); input.value = ''; await loadConversations(); }
        catch (error) { window.showGlobalToast?.(error.message, 'danger'); }
        finally { button.disabled = false; }
    });
    input.addEventListener('keydown', event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); compose.requestSubmit(); } });
    closeButton.addEventListener('click', async () => {
        if (!activeId || !confirm('Đóng cuộc trò chuyện này?')) return;
        try { await request(`/AdminChat/conversations/${activeId}/close`, { method: 'POST', body: '{}' }); setRoomStatus('Closed'); await loadConversations(); }
        catch (error) { window.showGlobalToast?.(error.message, 'danger'); }
    });
    document.getElementById('admin-chat-back').addEventListener('click', () => root.classList.remove('room-open'));
    document.getElementById('admin-chat-refresh').addEventListener('click', loadConversations);
    document.querySelectorAll('[data-filter]').forEach(button => button.addEventListener('click', () => {
        document.querySelectorAll('[data-filter]').forEach(x => x.classList.remove('active')); button.classList.add('active'); filter = button.dataset.filter; renderList();
    }));

    if (window.signalR) {
        const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/support-chat').withAutomaticReconnect().build();
        connection.on('MessageReceived', async (conversationId, message) => { if (Number(conversationId) === Number(activeId)) addMessage(message); await loadConversations(); });
        connection.on('ConversationUpdated', loadConversations);
        connection.start().then(() => connection.invoke('JoinAdmin')).catch(() => setInterval(loadConversations, 5000));
    } else setInterval(loadConversations, 5000);
    loadConversations();
})();
