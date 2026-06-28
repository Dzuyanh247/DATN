(() => {
    const dragThreshold = 5;
    const rails = document.querySelectorAll('[data-product-rail]');

    rails.forEach((rail) => {
        let isPointerDown = false;
        let hasDragged = false;
        let suppressClick = false;
        let startX = 0;
        let startScrollLeft = 0;

        // Drag only works when this exact element is the horizontal scroller.
        // In DevTools, the active rail must satisfy: scrollWidth > clientWidth.
        const hasHorizontalOverflow = () => rail.scrollWidth > rail.clientWidth + 1;

        const stopDragging = () => {
            if (!isPointerDown) {
                return;
            }

            isPointerDown = false;
            rail.classList.remove('is-dragging');

            if (hasDragged) {
                suppressClick = true;
                window.setTimeout(() => {
                    suppressClick = false;
                }, 0);
            }
        };

        rail.addEventListener('mousedown', (event) => {
            if (event.button !== 0 || !hasHorizontalOverflow()) {
                return;
            }

            isPointerDown = true;
            hasDragged = false;
            startX = event.pageX;
            startScrollLeft = rail.scrollLeft;
            rail.dataset.dragStartScrollLeft = String(startScrollLeft);
        });

        rail.addEventListener('mousemove', (event) => {
            if (!isPointerDown) {
                return;
            }

            // If the left button is no longer held (for example after releasing
            // outside the rail), end the drag without treating hover movement as scroll.
            if ((event.buttons & 1) !== 1) {
                stopDragging();
                return;
            }

            const deltaX = event.pageX - startX;

            if (Math.abs(deltaX) > dragThreshold) {
                hasDragged = true;
                rail.classList.add('is-dragging');
            }

            if (!hasDragged) {
                return;
            }

            event.preventDefault();
            rail.scrollLeft = startScrollLeft - deltaX;
        });

        rail.addEventListener('mouseup', stopDragging);
        rail.addEventListener('mouseleave', stopDragging);
        document.addEventListener('mouseup', stopDragging);

        rail.addEventListener('click', (event) => {
            if (!suppressClick) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            suppressClick = false;
        }, true);

        rail.addEventListener('dragstart', (event) => {
            if (isPointerDown) {
                event.preventDefault();
            }
        });

        if (new URLSearchParams(window.location.search).has('debugRails')) {
            // Required manual checks: the page must not overflow horizontally,
            // this exact rail must overflow horizontally, and dragging must
            // change this element's scrollLeft.
            // eslint-disable-next-line no-console
            console.debug('KKSHOP rail debug', {
                bodyFits: document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1,
                railOverflows: hasHorizontalOverflow(),
                scrollWidth: rail.scrollWidth,
                clientWidth: rail.clientWidth,
                scrollLeft: rail.scrollLeft,
            });
        }
    });
})();
