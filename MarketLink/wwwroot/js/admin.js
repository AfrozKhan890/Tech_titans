// MarketLink Admin — shared client behavior
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var toggleBtn = document.getElementById("mlSidebarToggle");
        var sidebar = document.getElementById("mlSidebar");
        var backdrop = document.getElementById("mlSidebarBackdrop");

        function openSidebar() {
            sidebar?.classList.add("ml-sidebar-open");
            backdrop?.classList.add("show");
        }

        function closeSidebar() {
            sidebar?.classList.remove("ml-sidebar-open");
            backdrop?.classList.remove("show");
        }

        toggleBtn?.addEventListener("click", function () {
            if (sidebar?.classList.contains("ml-sidebar-open")) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });

        backdrop?.addEventListener("click", closeSidebar);

        // Generic confirmation for destructive / status-changing actions.
        // Usage: add data-confirm="Are you sure?" to any form submit button or link.
        document.querySelectorAll("[data-confirm]").forEach(function (el) {
            el.addEventListener("click", function (e) {
                var message = el.getAttribute("data-confirm") || "Are you sure?";
                if (!window.confirm(message)) {
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        });
    });
})();
