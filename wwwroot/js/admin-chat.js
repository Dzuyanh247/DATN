(() => {
    const root = document.getElementById('admin-chat'); if (!root) return;
    const $ = id => document.getElementById(id);
    const list = $('admin-chat-list'), summary = $('admin-chat-summary'), room = $('admin-chat-room'), placeholder = $('admin-chat-placeholder'), messages = $('admin-chat-messages'), compose = $('admin-chat-compose'), input = $('admin-chat-message'), closeButton = $('admin-chat-close-conversation'), claimButton = $('admin-chat-claim'), staffSelect = $('admin-chat-staff');
    const csrf = document.querySelector('#admin-chat-antiforgery input[name="__RequestVerificationToken"]')?.value || '';
    const topicLabels = { PCConsultation: 'Tư vấn', Order: 'Đơn hàng', Warranty: 'Bảo hành', Payment: 'Thanh toán', StaffSupport: 'Cần xử lý' };
    let conversations = [], activeId = null, activeConversation = null, filter = 'all', polling;

    async function request(url, options = {}) {
        let response;
        try { response = await fetch(url, { ...options, credentials: 'same-origin', headers: { Accept: 'application/json', 'Content-Type': 'application/json', RequestVerificationToken: csrf, ...(options.headers || {}) } }); }
        catch { throw new Error('Không thể kết nối máy chủ.'); }
        const text = await response.text(); let result;
        try { result = text ? JSON.parse(text) : null; } catch { throw new Error(`Máy chủ trả về dữ liệu không hợp lệ (${response.status}).`); }
        if (!response.ok || !result?.success) throw new Error(result?.message || 'Không thể xử lý yêu cầu.');
        return result.data ?? result;
    }
    function formatDate(value, timeOnly = false) {
        const d = new Date(value); if (Number.isNaN(d.getTime())) return '';
        if (timeOnly) return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        return d.toDateString() === new Date().toDateString() ? formatDate(value, true) : d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: d.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined });
    }
    function visible() {
        if (filter === 'open') return conversations.filter(x => x.status.toLowerCase() === 'open');
        if (filter === 'closed') return conversations.filter(x => x.status.toLowerCase() === 'closed');
        if (filter === 'unread') return conversations.filter(x => x.unreadCount > 0);
        if (filter === 'needs') return conversations.filter(x => x.needsStaff || x.priority > 0);
        return conversations;
    }
    function renderList() {
        const rows = visible();
        summary.textContent = `${conversations.filter(x => x.status.toLowerCase() === 'open').length} đang mở • ${conversations.reduce((n, x) => n + x.unreadCount, 0)} chưa đọc`;
        list.replaceChildren();
        if (!rows.length) { const empty = document.createElement('div'); empty.className = 'admin-chat-empty'; empty.textContent = 'Chưa có hội thoại phù hợp.'; list.append(empty); return; }
        rows.forEach(item => {
            const button = document.createElement('button'); button.type = 'button'; button.className = `admin-chat-item${item.id === activeId ? ' active' : ''}${item.needsStaff ? ' needs-staff' : ''}`;
            const avatar = document.createElement('div'); avatar.className = 'admin-chat-item-avatar'; avatar.textContent = (item.name || 'K').trim()[0].toUpperCase();
            const main = document.createElement('div'); main.className = 'admin-chat-item-main';
            const row = document.createElement('div'); row.className = 'admin-chat-item-row';
            const name = document.createElement('span'); name.className = 'admin-chat-item-name'; name.textContent = item.name || 'Khách hàng';
            const time = document.createElement('span'); time.className = 'admin-chat-item-time'; time.textContent = formatDate(item.lastMessageAt || item.updatedAt);
            const topic = document.createElement('span'); topic.className = 'admin-chat-topic'; topic.textContent = item.needsStaff ? 'Cần xử lý' : (topicLabels[item.topic] || 'Tư vấn');
            const preview = document.createElement('div'); preview.className = 'admin-chat-item-message'; preview.textContent = item.lastMessage || 'Chưa có tin nhắn';
            const meta = document.createElement('div'); meta.className = 'admin-chat-item-meta';
            const assigned = document.createElement('span'); assigned.className = 'admin-chat-item-assignee'; assigned.textContent = item.assignedStaffName || 'Chưa phân công';
            const status = document.createElement('span'); const closed = item.status.toLowerCase() === 'closed'; status.className = `admin-chat-item-status${closed ? ' closed' : ''}`; status.textContent = closed ? 'Đã đóng' : 'Đang mở';
            row.append(name, time); meta.append(assigned, status); main.append(row, topic, preview, meta); button.append(avatar, main);
            if (item.unreadCount > 0) { const badge = document.createElement('span'); badge.className = 'admin-chat-unread'; badge.textContent = item.unreadCount > 99 ? '99+' : item.unreadCount; button.append(badge); }
            button.onclick = () => openConversation(item.id); list.append(button);
        });
    }
    async function loadConversations() {
        try { conversations = (await request('/AdminChat/conversations')).conversations || []; renderList(); }
        catch (error) { list.innerHTML = `<div class="admin-chat-empty">${error.message}</div>`; }
    }
    function metadataValue(value, name) { return value?.[name] ?? value?.[name.charAt(0).toUpperCase() + name.slice(1)]; }
    function renderMetadata(metadata, wrapper) {
        const cards = metadataValue(metadata, 'cards') || []; if (!cards.length) return;
        const container = document.createElement('div'); container.className = 'admin-chat-message-metadata';
        cards.forEach(card => {
            const item = document.createElement('div'); item.className = 'admin-chat-meta-card';
            if (card.imageUrl) { const image = document.createElement('img'); image.src = card.imageUrl; image.alt = ''; item.append(image); }
            const body = document.createElement('div'); body.className = 'admin-chat-meta-card-body';
            const title = document.createElement('strong'); title.textContent = card.title || card.orderCode || 'Thông tin liên quan'; body.append(title);
            const detail = [card.orderCode, card.orderStatus, card.subtitle, card.warrantyStatus].filter(Boolean).join(' • ');
            if (detail) { const small = document.createElement('small'); small.textContent = detail; body.append(small); }
            const action = (card.actions || [])[0]; if (action?.url) { const link = document.createElement('a'); link.href = action.url; link.textContent = action.label || 'Xem chi tiết'; body.append(link); }
            item.append(body); container.append(item);
        });
        wrapper.append(container);
    }
    function addMessage(item) {
        if (messages.querySelector(`[data-message-id="${item.id}"]`)) return;
        const type = String(item.senderType).toLowerCase(), system = item.isSystem || type === 'system', staff = type === 'staff' || type === 'admin';
        const wrapper = document.createElement('div'); wrapper.className = `admin-chat-message ${system ? 'system' : staff ? 'staff' : 'customer'}`; wrapper.dataset.messageId = item.id;
        const bubble = document.createElement('div'); bubble.className = 'admin-chat-bubble'; bubble.textContent = item.message;
        const time = document.createElement('div'); time.className = 'admin-chat-message-time'; time.textContent = `${system ? 'KKSHOP' : (item.senderName || (staff ? 'Nhân viên KKSHOP' : 'Khách hàng'))} • ${formatDate(item.createdAt, true)}`;
        wrapper.append(bubble, time); renderMetadata(item.metadata, wrapper); messages.append(wrapper); messages.scrollTop = messages.scrollHeight;
    }
    function setStatus(status) {
        const closed = status.toLowerCase() === 'closed', badge = $('admin-chat-status'); badge.textContent = closed ? 'Đã đóng' : 'Đang mở'; badge.classList.toggle('closed', closed);
        $('admin-chat-closed').classList.toggle('d-none', !closed); $('admin-chat-compose-area').classList.toggle('d-none', closed); closeButton.classList.toggle('d-none', closed); claimButton.classList.toggle('d-none', closed);
    }
    function setAssignment(c) { $('admin-chat-assignee').textContent = c.assignedStaffName ? `Phụ trách: ${c.assignedStaffName}` : 'Chưa phân công'; claimButton.classList.toggle('d-none', !!c.assignedStaffId || c.status.toLowerCase() === 'closed'); if (staffSelect) staffSelect.value = c.assignedStaffId || ''; }
    function parseContext(c) { try { return JSON.parse(c.automationContext || '{}'); } catch { return {}; } }
    function showContext(c) {
        const context = parseContext(c), topic = topicLabels[c.topic] || 'Tư vấn';
        $('admin-chat-header-topic').textContent = topic;
        const priority = $('admin-chat-priority'); priority.classList.toggle('d-none', !c.needsStaff && !c.priority); priority.textContent = c.priority > 0 ? `Ưu tiên ${c.priority}` : 'Cần xử lý';
        $('admin-chat-info-empty').classList.add('d-none'); $('admin-chat-info-content').classList.remove('d-none');
        $('admin-info-name').textContent = c.name || 'Chưa có thông tin'; $('admin-info-email').textContent = c.email || 'Chưa có thông tin'; $('admin-info-phone').textContent = c.phone || 'Chưa có thông tin'; $('admin-info-type').textContent = c.customerType || 'Khách vãng lai';
        $('admin-info-topic').textContent = topic; $('admin-info-order').textContent = context.orderCode || 'Chưa có thông tin'; $('admin-info-product').textContent = context.warrantyProduct || context.productName || 'Chưa có thông tin'; $('admin-info-need').textContent = context.pcNeed || 'Chưa có thông tin';
        $('admin-chat-history').textContent = context.orderCode ? `Đơn hàng gần nhất: ${context.orderCode}` : context.warrantyProduct ? `Yêu cầu gần nhất liên quan đến ${context.warrantyProduct}` : 'Chưa có thông tin';
    }
    async function openConversation(id) {
        try {
            const data = await request(`/AdminChat/conversations/${id}/messages`); activeId = id; activeConversation = data.conversation;
            placeholder.classList.add('d-none'); room.classList.remove('d-none'); root.classList.add('room-open');
            $('admin-chat-name').textContent = activeConversation.name || 'Khách hàng'; $('admin-chat-avatar').textContent = (activeConversation.name || 'K').trim()[0].toUpperCase();
            $('admin-chat-contact').textContent = [activeConversation.phone, activeConversation.email].filter(Boolean).join(' • ') || 'Chưa có thông tin liên hệ';
            setStatus(activeConversation.status); setAssignment(activeConversation); showContext(activeConversation); messages.replaceChildren(); data.messages.forEach(addMessage);
            const found = conversations.find(x => x.id === id); if (found) found.unreadCount = 0; renderList();
        } catch (error) { window.showGlobalToast?.(error.message, 'danger'); }
    }
    async function assign(staffId) {
        if (!activeId) return;
        try { const data = await request(`/AdminChat/conversations/${activeId}/assign`, { method: 'POST', body: JSON.stringify({ staffId: staffId || null }) }); activeConversation.assignedStaffId = data.assignedStaffId; activeConversation.assignedStaffName = data.assignedStaffName; activeConversation.needsStaff = false; setAssignment(activeConversation); showContext(activeConversation); await loadConversations(); }
        catch (error) { window.showGlobalToast?.(error.message, 'danger'); }
    }
    compose.onsubmit = async event => {
        event.preventDefault(); const text = input.value.trim(); if (!text || !activeId) return; const button = compose.querySelector('button'); button.disabled = true;
        try { addMessage(await request(`/AdminChat/conversations/${activeId}/messages`, { method: 'POST', body: JSON.stringify({ message: text }) })); input.value = ''; input.style.height = ''; input.focus(); await loadConversations(); }
        catch (error) { window.showGlobalToast?.(error.message, 'danger'); } finally { button.disabled = false; }
    };
    input.oninput = () => { input.style.height = 'auto'; input.style.height = `${Math.min(input.scrollHeight, 110)}px`; };
    input.onkeydown = event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); compose.requestSubmit(); } };
    document.querySelectorAll('[data-reply]').forEach(button => button.onclick = () => { input.value = button.dataset.reply; input.dispatchEvent(new Event('input')); input.focus(); });
    claimButton.onclick = () => assign(null); if (staffSelect) staffSelect.onchange = () => { if (staffSelect.value) assign(Number(staffSelect.value)); };
    closeButton.onclick = async () => { if (!activeId || !confirm('Bạn có chắc muốn đóng hội thoại này? Tin nhắn vẫn được lưu lại.')) return; try { await request(`/AdminChat/conversations/${activeId}/close`, { method: 'POST', body: '{}' }); setStatus('Closed'); activeConversation.status = 'Closed'; await loadConversations(); } catch (error) { window.showGlobalToast?.(error.message, 'danger'); } };
    $('admin-chat-back').onclick = () => root.classList.remove('room-open'); $('admin-chat-refresh').onclick = loadConversations;
    document.querySelectorAll('[data-filter]').forEach(button => button.onclick = () => { document.querySelectorAll('[data-filter]').forEach(item => item.classList.remove('active')); button.classList.add('active'); filter = button.dataset.filter; renderList(); });
    async function loadStaff() { if (!staffSelect) return; try { const data = await request('/AdminChat/staff'); (data.staff || []).forEach(item => { const option = document.createElement('option'); option.value = item.id; option.textContent = item.fullName; staffSelect.append(option); }); } catch {} }
    function fallbackPolling() { if (!polling) polling = setInterval(loadConversations, 7000); }
    if (window.signalR) { const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/support-chat').withAutomaticReconnect().build(); connection.on('MessageReceived', async (id, message) => { if (Number(id) === Number(activeId)) addMessage(message); await loadConversations(); }); connection.on('ConversationUpdated', loadConversations); connection.onreconnected(() => connection.invoke('JoinStaff')); connection.start().then(() => connection.invoke('JoinStaff')).catch(fallbackPolling); } else fallbackPolling();
    loadStaff(); loadConversations();
})();
