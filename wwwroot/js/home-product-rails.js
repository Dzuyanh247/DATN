(() => {
    const dragThreshold = 5;
    const rails = document.querySelectorAll('[data-product-rail]');

    rails.forEach((rail) => {
        let isPointerDown = false;
        let hasDragged = false;
        let suppressClick = false;
        let startX = 0;
        let startScrollLeft = 0;

        // Drag only works when the rail itself is the overflowing element; if
        // scrollWidth <= clientWidth, every card already fits so there is nothing to drag.
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
        });

        rail.addEventListener('mousemove', (event) => {
            if (!isPointerDown) {
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
    });
})();
