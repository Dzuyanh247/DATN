// Script điều khiển nút chat và đồng bộ thông tin user sang Tawk.to.
(function () {
    const openChatButton = document.getElementById("tawk-chat-toggle");
    let isWidgetLoaded = false;

    function setVisitorInfo() {
        if (!window.Tawk_API || typeof window.Tawk_API.setAttributes !== "function") return;

        const user = window.tawkChatUser || {};
        const attributes = {};

        if (user.name) attributes.name = user.name;
        if (user.email) attributes.email = user.email;
        if (user.role) attributes.role = user.role;

        // Chỉ gửi khi có dữ liệu thực tế.
        if (Object.keys(attributes).length > 0) {
            window.Tawk_API.setAttributes(attributes, function () { });
        }
    }

    function maximizeChat() {
        if (window.Tawk_API && typeof window.Tawk_API.maximize === "function") {
            window.Tawk_API.maximize();
        }
    }

    window.Tawk_API = window.Tawk_API || {};
    window.Tawk_API.onLoad = function () {
        isWidgetLoaded = true;
        setVisitorInfo();
    };

    if (openChatButton) {
        openChatButton.addEventListener("click", function () {
            // Nếu widget đã load thì mở ngay, chưa load thì đợi một chút rồi thử lại.
            maximizeChat();

            if (!isWidgetLoaded) {
                setTimeout(maximizeChat, 500);
            }
        });
    }
})();
