(() => {
    const searchForm = document.querySelector("[data-header-search]");

    if (searchForm) {
        const categoryInput = searchForm.querySelector("[data-search-category-id]");
        const categoryLabel = searchForm.querySelector("[data-search-category-label]");
        const categoryItems = searchForm.querySelectorAll("[data-category-id]");
        const searchInput = searchForm.querySelector("[data-search-input]");
        const suggestBox = searchForm.querySelector("[data-search-suggest]");
        const historyKey = "kkshop.searchHistory";

        const readJson = (name) => {
            try { return JSON.parse(searchForm.dataset[name] || "[]").filter(Boolean); }
            catch { return []; }
        };
        const unique = (items) => [...new Map(items.map((item) => [item.trim().toLowerCase(), item.trim()])).values()].filter(Boolean);
        const hotKeywords = unique(readJson("hotKeywords")).slice(0, 5);
        const suggestionPool = [
            ...readJson("productSuggestions").map((text) => ({ text, type: "Sản phẩm" })),
            ...readJson("categorySuggestions").map((text) => ({ text, type: "Danh mục" })),
            ...readJson("brandSuggestions").map((text) => ({ text, type: "Thương hiệu" }))
        ];
        const getHistory = () => {
            try { return JSON.parse(localStorage.getItem(historyKey) || "[]").filter(Boolean).slice(0, 5); }
            catch { return []; }
        };
        const saveHistory = (keyword) => {
            const value = (keyword || "").trim();
            if (!value) return;
            localStorage.setItem(historyKey, JSON.stringify(unique([value, ...getHistory()]).slice(0, 5)));
        };
        const submitSearch = (keyword) => {
            searchInput.value = keyword;
            saveHistory(keyword);
            searchForm.submit();
        };
        const section = (title, rows, icon, removable = false) => rows.length ? `
            <div class="ttg-search-suggest-section">
                <div class="ttg-search-suggest-title">${title}${removable ? '<button type="button" data-clear-history>Xóa tất cả lịch sử</button>' : ''}</div>
                ${rows.map((row) => `<button class="ttg-search-suggest-item" type="button" data-search-term="${escapeHtml(row)}"><span>${icon}</span><strong>${escapeHtml(row)}</strong>${removable ? `<i data-remove-history="${escapeHtml(row)}" aria-label="Xóa lịch sử">×</i>` : ''}</button>`).join("")}
            </div>` : "";
        const escapeHtml = (value) => String(value).replace(/[&<>'"]/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[ch]));
        const renderIdle = () => {
            const history = getHistory();
            suggestBox.innerHTML = `${section("LỊCH SỬ TÌM KIẾM", history, "🕒", true)}${history.length ? '<div class="ttg-search-suggest-divider"></div>' : ''}${section("TỪ KHÓA HOT", hotKeywords, "🔥")}`;
            suggestBox.hidden = !suggestBox.innerHTML.trim();
        };
        const renderRealtime = () => {
            const q = searchInput.value.trim().toLowerCase();
            if (!q) return renderIdle();
            const matches = unique(suggestionPool.filter((item) => item.text.toLowerCase().includes(q)).map((item) => item.text)).slice(0, 8);
            suggestBox.innerHTML = section("GỢI Ý TÌM KIẾM", matches, "🔎") || '<div class="ttg-search-suggest-empty">Không có gợi ý phù hợp</div>';
            suggestBox.hidden = false;
        };

        categoryItems.forEach((item) => {
            item.addEventListener("click", () => {
                categoryInput.value = item.dataset.categoryId || "";
                categoryLabel.textContent = item.dataset.categoryName || "Tất cả danh mục";
                categoryItems.forEach((candidate) => candidate.classList.remove("active"));
                item.classList.add("active");
            });
        });
        searchInput?.addEventListener("focus", renderRealtime);
        searchInput?.addEventListener("input", renderRealtime);
        searchForm.addEventListener("submit", () => saveHistory(searchInput?.value));
        suggestBox?.addEventListener("mousedown", (event) => event.preventDefault());
        suggestBox?.addEventListener("click", (event) => {
            const remove = event.target.closest("[data-remove-history]");
            if (remove) {
                localStorage.setItem(historyKey, JSON.stringify(getHistory().filter((item) => item !== remove.dataset.removeHistory)));
                renderIdle();
                return;
            }
            if (event.target.closest("[data-clear-history]")) { localStorage.removeItem(historyKey); renderIdle(); return; }
            const item = event.target.closest("[data-search-term]");
            if (item) submitSearch(item.dataset.searchTerm);
        });
        document.addEventListener("click", (event) => { if (!searchForm.contains(event.target)) suggestBox.hidden = true; });
        document.addEventListener("keydown", (event) => { if (event.key === "Escape" && suggestBox) suggestBox.hidden = true; });
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
