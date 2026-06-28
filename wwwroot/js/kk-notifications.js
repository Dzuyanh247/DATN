(() => {
    'use strict';

    const icons = { success: 'bi-check-circle-fill', error: 'bi-x-circle-fill', danger: 'bi-x-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
    const normalizeToastType = type => (type === 'danger' ? 'error' : (type || 'info').toLowerCase());
    const ensureToastHost = () => {
        let host = document.querySelector('[data-kk-toast-host]');
        if (!host) {
            host = document.createElement('div');
            host.className = 'kk-toast-host';
            host.setAttribute('data-kk-toast-host', '');
            host.setAttribute('aria-live', 'polite');
            host.setAttribute('aria-atomic', 'true');
            document.body.append(host);
        }
        return host;
    };

    function showToast(message, type = 'info', options = {}) {
        if (!message) return null;
        const tone = normalizeToastType(type);
        const toast = document.createElement('div');
        toast.className = `kk-toast kk-toast--${tone}`;
        toast.setAttribute('role', tone === 'error' ? 'alert' : 'status');
        toast.innerHTML = `<span class="kk-toast__icon"><i class="bi ${icons[tone] || icons.info}"></i></span><span class="kk-toast__message"></span><button type="button" class="kk-toast__close" aria-label="Đóng thông báo"><i class="bi bi-x-lg"></i></button>`;
        toast.querySelector('.kk-toast__message').textContent = message;
        const close = () => {
            toast.classList.remove('is-visible');
            toast.addEventListener('transitionend', () => toast.remove(), { once: true });
            window.setTimeout(() => toast.remove(), 250);
        };
        toast.querySelector('.kk-toast__close').addEventListener('click', close);
        ensureToastHost().append(toast);
        requestAnimationFrame(() => toast.classList.add('is-visible'));
        window.setTimeout(close, options.delay || 3600);
        return toast;
    }

    window.kkToast = {
        success: message => showToast(message, 'success'),
        error: message => showToast(message, 'error'),
        warning: message => showToast(message, 'warning'),
        info: message => showToast(message, 'info'),
        show: showToast
    };

    function ensureConfirmModal() {
        let modal = document.getElementById('kkConfirmModal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'kkConfirmModal';
            modal.className = 'kk-confirm';
            modal.setAttribute('aria-hidden', 'true');
            modal.innerHTML = `<div class="kk-confirm__overlay" data-kk-confirm-cancel></div><div class="kk-confirm__dialog" role="dialog" aria-modal="true" aria-labelledby="kkConfirmTitle" aria-describedby="kkConfirmMessage" tabindex="-1"><button type="button" class="kk-confirm__x" data-kk-confirm-cancel aria-label="Đóng"><i class="bi bi-x-lg"></i></button><div class="kk-confirm__badge"><i class="bi bi-question-lg"></i></div><h2 id="kkConfirmTitle" class="kk-confirm__title"></h2><p id="kkConfirmMessage" class="kk-confirm__message"></p><div class="kk-confirm__actions"><button type="button" class="btn btn-light kk-confirm__cancel" data-kk-confirm-cancel>Hủy</button><button type="button" class="btn kk-confirm__ok">Xác nhận</button></div></div>`;
            document.body.append(modal);
        }
        return modal;
    }

    window.kkConfirm = options => {
        const settings = { title: 'Xác nhận thao tác', message: 'Bạn có chắc muốn tiếp tục không?', confirmText: 'Xác nhận', cancelText: 'Hủy', type: 'primary', onConfirm: null, ...options };
        const modal = ensureConfirmModal();
        const dialog = modal.querySelector('.kk-confirm__dialog');
        const ok = modal.querySelector('.kk-confirm__ok');
        const cancel = modal.querySelector('.kk-confirm__cancel');
        modal.className = `kk-confirm kk-confirm--${settings.type || 'primary'}`;
        modal.querySelector('.kk-confirm__title').textContent = settings.title;
        modal.querySelector('.kk-confirm__message').textContent = settings.message;
        ok.textContent = settings.confirmText;
        cancel.textContent = settings.cancelText;
        const close = () => { modal.classList.remove('is-open'); modal.setAttribute('aria-hidden', 'true'); document.body.classList.remove('kk-confirm-open'); document.removeEventListener('keydown', onKeydown); };
        const confirm = () => { close(); if (typeof settings.onConfirm === 'function') settings.onConfirm(); };
        const onKeydown = event => { if (event.key === 'Escape') close(); };
        modal.querySelectorAll('[data-kk-confirm-cancel]').forEach(el => { el.onclick = close; });
        ok.onclick = confirm;
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('kk-confirm-open');
        modal.classList.add('is-open');
        document.addEventListener('keydown', onKeydown);
        window.setTimeout(() => dialog.focus(), 30);
    };

    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.dataset.kkConfirmed === 'true') return;
        const message = form.dataset.confirmMessage;
        if (!message) return;
        event.preventDefault();
        window.kkConfirm({
            title: form.dataset.confirmTitle || 'Xác nhận thao tác',
            message,
            confirmText: form.dataset.confirmText || 'Xác nhận',
            cancelText: form.dataset.cancelText || 'Hủy',
            type: form.dataset.confirmType || 'primary',
            onConfirm: () => { form.dataset.kkConfirmed = 'true'; form.requestSubmit(); }
        });
    }, true);

    document.addEventListener('click', event => {
        const link = event.target.closest('a[data-confirm-message]');
        if (!link) return;
        event.preventDefault();
        window.kkConfirm({
            title: link.dataset.confirmTitle || 'Xác nhận thao tác',
            message: link.dataset.confirmMessage,
            confirmText: link.dataset.confirmText || 'Xác nhận',
            cancelText: link.dataset.cancelText || 'Hủy',
            type: link.dataset.confirmType || 'primary',
            onConfirm: () => { window.location.href = link.href; }
        });
    });
})();
