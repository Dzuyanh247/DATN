(() => {
    const initializedRails = new WeakSet();
    const mouseDragThreshold = 6;
    const touchDragThreshold = 10;
    const interactiveSelector = 'a[href], button, input, select, textarea, label, .add-to-cart, .compare, .ttg-add-btn, .ttg-compare-btn, .ttg-card-actions, [role="button"]';

    const getPageX = (event) => {
        if (event.touches && event.touches.length) return event.touches[0].pageX;
        if (event.changedTouches && event.changedTouches.length) return event.changedTouches[0].pageX;
        return event.pageX;
    };

    const initRail = (rail) => {
        if (!rail || initializedRails.has(rail)) return;
        initializedRails.add(rail);

        let isDown = false;
        let hasDragged = false;
        let suppressClick = false;
        let startX = 0;
        let startScrollLeft = 0;
        let activeThreshold = mouseDragThreshold;
        let frame = 0;
        let pendingScrollLeft = 0;
        let activePointerId = null;

        const hasHorizontalOverflow = () => rail.scrollWidth > rail.clientWidth + 1;

        const isInteractiveTarget = (target) => target?.closest?.(interactiveSelector);

        const getDragThreshold = (event) => {
            if (event.pointerType === 'touch' || event.type?.startsWith('touch')) return touchDragThreshold;
            return mouseDragThreshold;
        };

        const setScrollLeft = (value) => {
            pendingScrollLeft = value;
            if (frame) return;
            frame = window.requestAnimationFrame(() => {
                rail.scrollLeft = pendingScrollLeft;
                frame = 0;
            });
        };

        const beginDrag = (event) => {
            if (!hasHorizontalOverflow()) return;
            if (event.type === 'mousedown' && event.button !== 0) return;
            if (isInteractiveTarget(event.target) && !event.target.closest?.('.ttg-product-image-wrap, .ttg-product-title')) return;

            isDown = true;
            hasDragged = false;
            startX = getPageX(event);
            startScrollLeft = rail.scrollLeft;
            activeThreshold = getDragThreshold(event);
            activePointerId = event.pointerId ?? null;

            if (event.pointerId != null && rail.setPointerCapture) {
                try { rail.setPointerCapture(event.pointerId); } catch { /* no-op */ }
            }
        };

        const moveDrag = (event) => {
            if (!isDown) return;
            if (event.pointerId != null && activePointerId != null && event.pointerId !== activePointerId) return;
            if (event.type === 'mousemove' && (event.buttons & 1) !== 1) {
                endDrag(event);
                return;
            }

            const deltaX = getPageX(event) - startX;
            if (Math.abs(deltaX) > activeThreshold) {
                hasDragged = true;
                rail.classList.add('is-dragging');
            }
            if (!hasDragged) return;

            if (event.cancelable) event.preventDefault();
            setScrollLeft(startScrollLeft - deltaX);
        };

        const endDrag = (event) => {
            if (!isDown) return;
            isDown = false;
            rail.classList.remove('is-dragging');

            if (event?.pointerId != null && rail.releasePointerCapture) {
                try { rail.releasePointerCapture(event.pointerId); } catch { /* no-op */ }
            }
            activePointerId = null;

            if (hasDragged) {
                suppressClick = true;
                window.setTimeout(() => { suppressClick = false; }, 100);
            }
        };

        if (window.PointerEvent) {
            rail.addEventListener('pointerdown', beginDrag);
            rail.addEventListener('pointermove', moveDrag);
            rail.addEventListener('pointerup', endDrag);
            rail.addEventListener('pointercancel', endDrag);
            rail.addEventListener('pointerleave', endDrag);
        } else {
            rail.addEventListener('mousedown', beginDrag);
            rail.addEventListener('mousemove', moveDrag);
            rail.addEventListener('mouseup', endDrag);
            rail.addEventListener('mouseleave', endDrag);
            document.addEventListener('mouseup', endDrag);

            rail.addEventListener('touchstart', beginDrag, { passive: true });
            rail.addEventListener('touchmove', moveDrag, { passive: false });
            rail.addEventListener('touchend', endDrag);
            rail.addEventListener('touchcancel', endDrag);
        }

        rail.addEventListener('wheel', (event) => {
            if (!hasHorizontalOverflow()) return;
            const useHorizontalWheel = Math.abs(event.deltaX) > Math.abs(event.deltaY);
            const useShiftWheel = event.shiftKey && Math.abs(event.deltaY) > 0;
            if (!useHorizontalWheel && !useShiftWheel) return;

            const delta = useHorizontalWheel ? event.deltaX : event.deltaY;
            if (event.cancelable) event.preventDefault();
            setScrollLeft(rail.scrollLeft + delta);
        }, { passive: false });

        rail.addEventListener('click', (event) => {
            if (!suppressClick) return;
            suppressClick = false;
            if (isInteractiveTarget(event.target)) return;
            event.preventDefault();
        }, true);

        rail.addEventListener('dragstart', (event) => {
            if (isDown || hasDragged) event.preventDefault();
        });
    };

    const initAllRails = () => document.querySelectorAll('[data-product-rail]').forEach(initRail);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAllRails, { once: true });
    } else {
        initAllRails();
    }

    window.KKSHOPProductRails = { init: initAllRails, initRail };
})();
