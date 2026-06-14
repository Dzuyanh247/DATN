(() => {
    const searchForm = document.querySelector("[data-header-search]");

    if (searchForm) {
        const categoryInput = searchForm.querySelector("[data-search-category-id]");
        const categoryLabel = searchForm.querySelector("[data-search-category-label]");
        const categoryItems = searchForm.querySelectorAll("[data-category-id]");

        categoryItems.forEach((item) => {
            item.addEventListener("click", () => {
                categoryInput.value = item.dataset.categoryId || "";
                categoryLabel.textContent = item.dataset.categoryName || "Tất cả danh mục";
                categoryItems.forEach((candidate) => candidate.classList.remove("active"));
                item.classList.add("active");
            });
        });
    }

    const menuToggle = document.querySelector("[data-category-menu-toggle]");
    const categoryMenu = document.querySelector("[data-category-menu]");

    if (!menuToggle || !categoryMenu) {
        return;
    }

    const setMenuOpen = (isOpen) => {
        categoryMenu.hidden = !isOpen;
        menuToggle.setAttribute("aria-expanded", String(isOpen));
        menuToggle.classList.toggle("is-open", isOpen);
    };

    menuToggle.addEventListener("click", (event) => {
        event.stopPropagation();
        setMenuOpen(categoryMenu.hidden);
    });

    categoryMenu.addEventListener("click", (event) => event.stopPropagation());
    document.addEventListener("click", () => setMenuOpen(false));
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            setMenuOpen(false);
            menuToggle.focus();
        }
    });
})();
