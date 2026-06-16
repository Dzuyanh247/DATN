(() => {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {
        const root = document.getElementById('admin-chat');
        if (!root || root.dataset.adminChatBound === 'true') return;
        root.dataset.adminChatBound = 'true';

        const $ = id => document.getElementById(id);
        const required = ['admin-chat-list', 'admin-chat-summary', 'admin-chat-room', 'admin-chat-placeholder', 'admin-chat-messages', 'admin-chat-compose', 'admin-chat-message'];
        if (required.some(id => !$(id))) return;

        const list = $('admin-chat-list');
        const summary = $('admin-chat-summary');
        const room = $('admin-chat-room');
        const placeholder = $('admin-chat-placeholder');
        const messages = $('admin-chat-messages');
        const compose = $('admin-chat-compose');
        const input = $('admin-chat-message');
        const closeButton = $('admin-chat-close-conversation');
        const resolveButton = $('admin-chat-resolve');
        const drawer = $('admin-chat-drawer');
        const drawerBackdrop = $('admin-chat-drawer-backdrop');
        const drawerTitle = $('admin-chat-drawer-title');
        const drawerBody = $('admin-chat-drawer-body');
        const drawerClose = $('admin-chat-drawer-close');
        const claimButton = $('admin-chat-claim');
        const staffSelect = $('admin-chat-staff');
        const backButton = $('admin-chat-back');
        const refreshButton = $('admin-chat-refresh');
        const composeArea = $('admin-chat-compose-area') || root.querySelector('.admin-chat-compose-area');
        const csrf = document.querySelector('#admin-chat-antiforgery input[name="__RequestVerificationToken"]')?.value || '';
        const topicLabels = { PCConsultation: 'Tư vấn cấu hình PC', Order: 'Đơn hàng', Warranty: 'Bảo hành', Payment: 'Thanh toán', StaffSupport: 'Gặp nhân viên' };
        const systemMessages = ['đã đóng', 'đã được đóng', 'hội thoại đã', 'conversation closed', 'đã kết thúc'];
        let conversations = [], activeId = null, activeConversation = null, filter = 'all', polling;

        const setText = (id, text) => { const el = $(id); if (el) el.textContent = text; };
        const toggleClass = (el, className, force) => { if (el) el.classList.toggle(className, force); };
        const showError = message => window.showGlobalToast?.(message || 'Đã xảy ra lỗi. Vui lòng thử lại.', 'danger');
        const scrollToBottom = () => requestAnimationFrame(() => { messages.scrollTop = messages.scrollHeight; });
        const statusValue = value => String(value || '').toLowerCase();

        function resetEmptyState() {
            activeId = null;
            activeConversation = null;
            toggleClass(placeholder, 'd-none', false);
            toggleClass(room, 'd-none', true);
            root.classList.remove('room-open');
            const infoEmpty = $('admin-chat-info-empty');
            const infoContent = $('admin-chat-info-content');
            toggleClass(infoEmpty, 'd-none', false);
            toggleClass(infoContent, 'd-none', true);
            if (closeButton) closeButton.disabled = true;
            if (resolveButton) resolveButton.disabled = true;
            closeDrawer();
            if (claimButton) claimButton.disabled = true;
            if (staffSelect) staffSelect.disabled = true;
            if (input) input.disabled = true;
            const send = compose?.querySelector('button[type="submit"]');
            if (send) send.disabled = true;
        }

        async function request(url, options = {}) {
            let response;
            try {
                response = await fetch(url, { ...options, credentials: 'same-origin', headers: { Accept: 'application/json', 'Content-Type': 'application/json', RequestVerificationToken: csrf, ...(options.headers || {}) } });
            } catch {
                throw new Error('Không thể kết nối máy chủ. Vui lòng kiểm tra mạng và thử lại.');
            }
            const text = await response.text();
            let result;
            try { result = text ? JSON.parse(text) : null; } catch { throw new Error('Máy chủ trả về dữ liệu không hợp lệ. Vui lòng thử lại sau.'); }
            if (!response.ok || !result?.success) throw new Error(result?.message || 'Không thể xử lý yêu cầu. Vui lòng thử lại.');
            return result.data ?? result;
        }

        function formatDate(value, timeOnly = false) {
            const d = new Date(value); if (Number.isNaN(d.getTime())) return '';
            if (timeOnly) return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
            return d.toDateString() === new Date().toDateString() ? formatDate(value, true) : d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: d.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined });
        }
        function visible() {
            if (filter === 'open') return conversations.filter(x => statusValue(x.status) === 'open');
            if (filter === 'closed') return conversations.filter(x => statusValue(x.status) === 'closed');
            if (filter === 'unread') return conversations.filter(x => (x.unreadCount || 0) > 0);
            if (filter === 'needs') return conversations.filter(x => x.needsStaff || (x.priority || 0) > 0);
            return conversations;
        }
        function renderList() {
            const rows = visible();
            summary.textContent = `${conversations.filter(x => statusValue(x.status) === 'open').length} đang mở • ${conversations.reduce((n, x) => n + (x.unreadCount || 0), 0)} chưa đọc`;
            list.replaceChildren();
            if (!rows.length) { const empty = document.createElement('div'); empty.className = 'admin-chat-empty'; empty.textContent = 'Chưa có hội thoại phù hợp.'; list.append(empty); return; }
            rows.forEach(item => {
                const button = document.createElement('button'); button.type = 'button'; button.className = `admin-chat-item${item.id === activeId ? ' active' : ''}${item.needsStaff ? ' needs-staff' : ''}`;
                const avatar = document.createElement('div'); avatar.className = 'admin-chat-item-avatar'; avatar.textContent = (item.name || 'K').trim()[0]?.toUpperCase() || 'K';
                const main = document.createElement('div'); main.className = 'admin-chat-item-main';
                const row = document.createElement('div'); row.className = 'admin-chat-item-row';
                const name = document.createElement('span'); name.className = 'admin-chat-item-name'; name.textContent = item.name || 'Khách hàng';
                const time = document.createElement('span'); time.className = 'admin-chat-item-time'; time.textContent = formatDate(item.lastMessageAt || item.updatedAt);
                const topic = document.createElement('span'); topic.className = 'admin-chat-topic'; topic.textContent = item.needsStaff ? 'Cần xử lý' : (topicLabels[item.topic] || 'Tư vấn');
                const preview = document.createElement('div'); preview.className = 'admin-chat-item-message'; preview.textContent = item.lastMessage || 'Chưa có tin nhắn';
                const meta = document.createElement('div'); meta.className = 'admin-chat-item-meta';
                const assigned = document.createElement('span'); assigned.className = 'admin-chat-item-assignee'; assigned.textContent = item.assignedStaffName || 'Chưa phân công';
                const status = document.createElement('span'); const closed = statusValue(item.status) === 'closed'; const unread = (item.unreadCount || 0) > 0; const needs = item.needsStaff || (item.priority || 0) > 0; status.className = `admin-chat-item-status${closed ? ' closed' : unread ? ' unread' : needs ? ' needs' : ''}`; status.textContent = closed ? 'Đã đóng' : unread ? 'Chưa đọc' : needs ? 'Cần xử lý' : 'Đang mở';
                row.append(name, time); meta.append(assigned, status); main.append(row, topic, preview, meta); button.append(avatar, main);
                if ((item.unreadCount || 0) > 0) { const badge = document.createElement('span'); badge.className = 'admin-chat-unread'; badge.textContent = item.unreadCount > 99 ? '99+' : item.unreadCount; button.append(badge); }
                button.addEventListener('click', () => openConversation(item.id)); list.append(button);
            });
        }
        async function loadConversations() {
            try { conversations = (await request('/AdminChat/conversations')).conversations || []; renderList(); }
            catch (error) { list.innerHTML = ''; const empty = document.createElement('div'); empty.className = 'admin-chat-empty'; empty.textContent = error.message; list.append(empty); }
        }
        function metadataValue(value, name) { return value?.[name] ?? value?.[name.charAt(0).toUpperCase() + name.slice(1)]; }
        function formatMoney(value) { const number = Number(value); return Number.isFinite(number) ? number.toLocaleString('vi-VN') + ' đ' : ''; }
        function formatDay(value) {
            const d = new Date(value); if (Number.isNaN(d.getTime())) return '';
            const today = new Date(); const yesterday = new Date(); yesterday.setDate(today.getDate() - 1);
            if (d.toDateString() === today.toDateString()) return 'Hôm nay';
            if (d.toDateString() === yesterday.toDateString()) return 'Hôm qua';
            return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
        }
        function appendDateSeparator(value) {
            const label = formatDay(value); if (!label) return;
            const last = messages.lastElementChild;
            if (last?.classList.contains('admin-chat-date-separator') && last.textContent === label) return;
            if (messages.querySelector(`[data-day-label="${CSS.escape(label)}"]`)) return;
            const separator = document.createElement('div'); separator.className = 'admin-chat-date-separator'; separator.dataset.dayLabel = label; separator.textContent = label; messages.append(separator);
        }
        function drawerRow(label, value) { return value ? `<div class="drawer-row"><b>${label}</b><span>${String(value).replace(/[&<>]/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[ch]))}</span></div>` : ''; }
        function drawerCode(label, value) { return value ? `<div class="drawer-row"><b>${label}</b><code>${String(value).replace(/[&<>]/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[ch]))}</code></div>` : ''; }
        function closeDrawer() { toggleClass(drawer, 'd-none', true); toggleClass(drawerBackdrop, 'd-none', true); }
        function openDrawer(kind) {
            if (!activeConversation || !drawer || !drawerBody || !drawerTitle) return;
            const context = parseContext(activeConversation);
            const titles = { orders: 'Đơn hàng', products: 'Sản phẩm', warranty: 'Bảo hành', history: 'Lịch sử mua' };
            drawerTitle.textContent = titles[kind] || 'Ngữ cảnh';
            let html = '';
            if (kind === 'orders') html = drawerCode('Mã đơn gần nhất', context.orderCode) + drawerRow('Trạng thái', context.orderStatus) + drawerRow('Thanh toán', context.paymentStatus) + drawerRow('Gợi ý', 'Dùng card trong luồng chat để xem đúng góc nhìn của khách.');
            if (kind === 'products') html = drawerRow('Sản phẩm đang trao đổi', context.productName || context.warrantyProduct) + drawerRow('Nhu cầu cấu hình', context.pcNeed) + drawerRow('Gợi ý', 'Tư vấn ngắn gọn, sau đó gửi card sản phẩm trong luồng chat.');
            if (kind === 'warranty') html = drawerRow('Sản phẩm bảo hành', context.warrantyProduct || context.productName) + drawerRow('Trạng thái bảo hành', context.warrantyStatus) + drawerRow('Gợi ý', 'Xin mã đơn, sản phẩm và mô tả lỗi trước khi hướng dẫn tiếp.');
            if (kind === 'history') html = drawerRow('Khách hàng', activeConversation.name || 'Khách hàng') + drawerRow('Liên hệ', [activeConversation.phone, activeConversation.email].filter(Boolean).join(' • ')) + drawerRow('Lần chat gần nhất', activeConversation.closedAt ? formatDay(activeConversation.closedAt) : formatDay(activeConversation.createdAt)) + drawerRow('Số cuộc trò chuyện', '1');
            drawerBody.innerHTML = html || '<div class="drawer-row">Chưa có dữ liệu ngữ cảnh cho hội thoại này.</div>';
            toggleClass(drawer, 'd-none', false); toggleClass(drawerBackdrop, 'd-none', false);
        }
        function addDetail(container, label, value, extraClass) { if (!value) return; const row = document.createElement('span'); if (extraClass) row.className = extraClass; const text = document.createElement('span'); text.textContent = label; const strong = document.createElement('b'); strong.textContent = value; row.append(text, strong); container.append(row); }
        function isTechnicalSystemMessage(item) { const text = String(item.message || '').toLowerCase(); return item.isSystem && systemMessages.some(token => text.includes(token)); }
        function normalizeCardActions(card) { const actions = (card.actions || []).filter(action => action.url); if (actions.length) return actions; return [card.checkUrl ? { label: 'Kiểm tra', url: card.checkUrl } : null, card.detailUrl || card.url ? { label: 'Xem chi tiết', url: card.detailUrl || card.url } : null, card.reviewUrl ? { label: 'Đánh giá', url: card.reviewUrl } : null].filter(Boolean); }
        function renderMetadata(metadata, wrapper) {
            const cards = metadataValue(metadata, 'cards') || []; if (!cards.length) return;
            const container = document.createElement('div'); container.className = 'admin-chat-message-metadata';
            cards.forEach(card => {
                const item = document.createElement('div'); item.className = 'admin-chat-meta-card';
                if (card.imageUrl) { const image = document.createElement('img'); image.src = card.imageUrl; image.alt = card.title || 'Sản phẩm'; item.append(image); }
                const body = document.createElement('div'); body.className = 'admin-chat-meta-card-body';
                const header = document.createElement('div'); header.className = 'admin-chat-meta-card-header';
                const code = document.createElement('span'); code.textContent = card.orderCode || (card.type === 'product' ? 'Sản phẩm' : 'Đơn hàng'); header.append(code);
                const statusBadge = document.createElement('span'); statusBadge.className = 'kk-chat-status-badge'; statusBadge.textContent = card.orderStatus || card.warrantyStatus || card.paymentStatus || 'Thông tin'; header.append(statusBadge);
                const title = document.createElement('strong'); title.textContent = card.title || card.orderCode || 'Thông tin liên quan';
                const details = document.createElement('div'); details.className = 'admin-chat-meta-details';
                addDetail(details, 'Mã đơn', card.orderCode);
                addDetail(details, 'Trạng thái đơn', card.orderStatus);
                addDetail(details, 'Thanh toán', card.paymentStatus);
                addDetail(details, 'Ngày đặt', card.orderedAt ? formatDay(card.orderedAt) : '');
                addDetail(details, 'Tổng tiền', formatMoney(card.totalAmount), 'total');
                addDetail(details, 'Bảo hành', card.warrantyStatus);
                const actions = document.createElement('div'); actions.className = 'admin-chat-meta-actions';
                normalizeCardActions(card).slice(0, 3).forEach((action, index) => { const link = document.createElement('a'); link.href = action.url; link.textContent = action.label || (index === 0 ? 'Kiểm tra' : index === 1 ? 'Xem chi tiết' : 'Đánh giá'); if (index) link.className = 'secondary'; actions.append(link); });
                body.append(header, title); if (card.subtitle) { const sub = document.createElement('small'); sub.textContent = card.subtitle; body.append(sub); } body.append(details); if (actions.childElementCount) body.append(actions);
                item.append(body); container.append(item);
            });
            wrapper.append(container);
        }
        function addMessage(item) {
            if (!item || !messages || messages.querySelector(`[data-message-id="${item.id}"]`)) return;
            appendDateSeparator(item.createdAt);
            const type = String(item.senderType || '').toLowerCase();
            const technicalSystem = isTechnicalSystemMessage(item) || type === 'system';
            const staff = type === 'staff' || type === 'admin';
            const bot = !technicalSystem && (item.isSystem || type === 'bot' || type === 'assistant' || type === 'automation');
            const wrapper = document.createElement('div');
            wrapper.className = `admin-chat-message ${technicalSystem ? 'system' : staff ? 'staff' : bot ? 'bot' : 'customer'}`;
            wrapper.dataset.messageId = item.id;
            const bubble = document.createElement('div');
            bubble.className = 'admin-chat-bubble';
            bubble.textContent = item.message || '';
            const time = document.createElement('div');
            time.className = 'admin-chat-message-time';
            time.textContent = `${technicalSystem || bot ? 'KKSHOP' : (item.senderName || (staff ? 'Nhân viên KKSHOP' : 'Khách hàng'))} • ${formatDate(item.createdAt, true)}`;
            wrapper.append(bubble, time);
            renderMetadata(item.metadata, wrapper);
            messages.append(wrapper);
            scrollToBottom();
        }
        function setStatus(status) { const closed = statusValue(status) === 'closed', badge = $('admin-chat-status'); if (badge) { badge.textContent = closed ? 'Đã đóng' : 'Đang mở'; badge.classList.toggle('closed', closed); } toggleClass($('admin-chat-closed'), 'd-none', !closed); if (input) input.disabled = closed || !activeId; const send = compose?.querySelector('button[type="submit"]'); if (send) send.disabled = closed || !activeId; if (closeButton) { closeButton.classList.toggle('d-none', closed); closeButton.disabled = closed || !activeId; } if (resolveButton) { resolveButton.disabled = closed || !activeId; } if (claimButton) { claimButton.classList.toggle('d-none', closed); claimButton.disabled = closed || !activeId; } }
        function setAssignment(c) { setText('admin-chat-assignee', c.assignedStaffName ? `Phụ trách: ${c.assignedStaffName}` : 'Chưa phân công'); if (claimButton) { claimButton.classList.toggle('d-none', !!c.assignedStaffId || statusValue(c.status) === 'closed'); claimButton.disabled = !activeId || statusValue(c.status) === 'closed'; } if (staffSelect) { staffSelect.value = c.assignedStaffId || ''; staffSelect.disabled = !activeId || statusValue(c.status) === 'closed'; } }
        function parseContext(c) { if (!c.automationContext) return {}; const value = String(c.automationContext).trim(); return value.startsWith('{') ? JSON.parse(value) : {}; }
        function showContext(c) { const context = parseContext(c), topic = topicLabels[c.topic] || 'Tư vấn'; setText('admin-chat-header-topic', topic); const priority = $('admin-chat-priority'); if (priority) { priority.classList.toggle('d-none', !c.needsStaff && !c.priority); priority.textContent = c.priority > 0 ? `Ưu tiên ${c.priority}` : 'Cần xử lý'; } toggleClass($('admin-chat-info-empty'), 'd-none', true); toggleClass($('admin-chat-info-content'), 'd-none', false); setText('admin-info-name', c.name || 'Chưa có thông tin'); setText('admin-info-email', c.email || 'Chưa có thông tin'); setText('admin-info-phone', c.phone || 'Chưa có thông tin'); setText('admin-info-type', c.customerType || 'Khách vãng lai'); setText('admin-info-topic', topic); setText('admin-info-order', context.orderCode || 'Chưa có thông tin'); setText('admin-info-product', context.warrantyProduct || context.productName || 'Chưa có thông tin'); setText('admin-info-need', context.pcNeed || 'Chưa có thông tin'); const history = $('admin-chat-history'); if (history) { history.replaceChildren(); const rows = [context.orderCode ? `Đơn hàng gần nhất: ${context.orderCode}` : '', c.closedAt ? `Lần chat gần nhất: ${formatDay(c.closedAt)}` : (c.createdAt ? `Lần chat gần nhất: ${formatDay(c.createdAt)}` : ''), 'Số cuộc trò chuyện: 1']; rows.filter(Boolean).forEach(text => { const span = document.createElement('span'); span.textContent = text; history.append(span); }); if (!history.childElementCount) history.textContent = 'Chưa có thông tin'; } }
        async function openConversation(id) { if (!id) return resetEmptyState(); try { const data = await request(`/AdminChat/conversations/${id}/messages`); activeId = id; activeConversation = data.conversation || {}; placeholder.classList.add('d-none'); room.classList.remove('d-none'); root.classList.add('room-open'); setText('admin-chat-name', activeConversation.name || 'Khách hàng'); setText('admin-chat-avatar', (activeConversation.name || 'K').trim()[0]?.toUpperCase() || 'K'); setText('admin-chat-contact', [activeConversation.phone, activeConversation.email].filter(Boolean).join(' • ') || 'Chưa có thông tin liên hệ'); setStatus(activeConversation.status); setAssignment(activeConversation); showContext(activeConversation); messages.replaceChildren(); (data.messages || []).forEach(addMessage); scrollToBottom(); const found = conversations.find(x => x.id === id); if (found) found.unreadCount = 0; renderList(); } catch (error) { showError(error.message); } }
        async function assign(staffId) { if (!activeId || !activeConversation) return; try { const data = await request(`/AdminChat/conversations/${activeId}/assign`, { method: 'POST', body: JSON.stringify({ staffId: staffId || null }) }); activeConversation.assignedStaffId = data.assignedStaffId; activeConversation.assignedStaffName = data.assignedStaffName; activeConversation.needsStaff = false; setAssignment(activeConversation); showContext(activeConversation); await loadConversations(); } catch (error) { showError(error.message); } }

        compose.addEventListener('submit', async event => { event.preventDefault(); const text = input.value.trim(); if (!text || !activeId) return; const button = compose.querySelector('button'); if (button) button.disabled = true; try { addMessage(await request(`/AdminChat/conversations/${activeId}/messages`, { method: 'POST', body: JSON.stringify({ message: text }) })); input.value = ''; input.style.height = ''; input.focus(); await loadConversations(); } catch (error) { showError(error.message); } finally { if (button) button.disabled = false; } });
        input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = `${Math.min(input.scrollHeight, 110)}px`; });
        input.addEventListener('keydown', event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); compose.requestSubmit(); } });
        document.querySelectorAll('[data-reply]').forEach(button => button.addEventListener('click', () => { if (!activeId) return; input.value = button.dataset.reply || ''; input.dispatchEvent(new Event('input')); input.focus(); }));
        claimButton?.addEventListener('click', () => assign(null));
        staffSelect?.addEventListener('change', () => { if (activeId && staffSelect.value) assign(Number(staffSelect.value)); });
        resolveButton?.addEventListener('click', async () => { if (!activeId) return; input.value = 'Hội thoại đã được KKSHOP đánh dấu đã xử lý. Nếu bạn cần hỗ trợ thêm, cứ nhắn lại cho KKSHOP nhé.'; input.dispatchEvent(new Event('input')); input.focus(); });
        closeButton?.addEventListener('click', async () => { if (!activeId || !activeConversation || !confirm('Bạn có chắc muốn đóng hội thoại này? Tin nhắn vẫn được lưu lại.')) return; try { await request(`/AdminChat/conversations/${activeId}/close`, { method: 'POST', body: '{}' }); setStatus('Closed'); activeConversation.status = 'Closed'; await loadConversations(); } catch (error) { showError(error.message); } });
        backButton?.addEventListener('click', () => root.classList.remove('room-open'));
        refreshButton?.addEventListener('click', loadConversations);
        drawerClose?.addEventListener('click', closeDrawer);
        drawerBackdrop?.addEventListener('click', closeDrawer);
        document.querySelectorAll('[data-panel]').forEach(button => button.addEventListener('click', () => openDrawer(button.dataset.panel)));
        document.querySelectorAll('[data-filter]').forEach(button => button.addEventListener('click', () => { document.querySelectorAll('[data-filter]').forEach(item => item.classList.remove('active')); button.classList.add('active'); filter = button.dataset.filter || 'all'; renderList(); }));
        async function loadStaff() { if (!staffSelect) return; const data = await request('/AdminChat/staff'); (data.staff || []).forEach(item => { const option = document.createElement('option'); option.value = item.id; option.textContent = item.fullName; staffSelect.append(option); }); }
        function fallbackPolling() { if (!polling) polling = setInterval(loadConversations, 7000); }
        if (window.signalR) { const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/support-chat').withAutomaticReconnect().build(); connection.on('MessageReceived', async (id, message) => { if (Number(id) === Number(activeId)) addMessage(message); await loadConversations(); }); connection.on('ConversationUpdated', loadConversations); connection.onreconnected(() => connection.invoke('JoinStaff')); connection.start().then(() => connection.invoke('JoinStaff')).catch(fallbackPolling); } else fallbackPolling();
        resetEmptyState(); loadStaff().catch(error => showError(error.message)); loadConversations();
    });
})();
