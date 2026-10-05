
'use strict';


const DarkMode = {
    init() {
        this.apply(localStorage.getItem('ml-theme') || 'light');
        document.querySelectorAll('.dark-mode-toggle').forEach(btn => {
            btn.setAttribute('aria-checked', document.documentElement.dataset.theme === 'dark');
            btn.addEventListener('click', () => this.toggle());
        });
    },
    toggle() {
        this.apply(document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark');
        localStorage.setItem('ml-theme', document.documentElement.dataset.theme);
        document.querySelectorAll('.dark-mode-toggle').forEach(btn => {
            btn.setAttribute('aria-checked', document.documentElement.dataset.theme === 'dark');
        });
    },


    apply(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
    }
};


const Toast = {
    container: null,
    init() {
        this.container = document.querySelector('.toast-container-green');
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container-green';
            document.body.appendChild(this.container);
        }
    },
    show(message, type = 'success', duration = 4000) {
        const icons = { success: '✅', error: '❌', warning: '⚠️', info: 'ℹ️' };
        const toast = document.createElement('div');
        toast.className = `toast-green toast-${type}`;

        const row = document.createElement('div');
        row.style.cssText = 'display:flex;align-items:flex-start;gap:10px;';

        const icon = document.createElement('span');
        icon.style.fontSize = '18px';
        icon.textContent = icons[type] || icons.info;

        const body = document.createElement('div');
        body.style.flex = '1';

        const title = document.createElement('div');
        title.style.cssText = 'font-size:14px;font-weight:600;color:var(--heading);margin-bottom:3px;';
        title.textContent = type.charAt(0).toUpperCase() + type.slice(1);

        const detail = document.createElement('div');
        detail.style.cssText = 'font-size:13px;color:var(--body-text);';
        detail.textContent = message ?? '';

        const close = document.createElement('button');
        close.type = 'button';
        close.style.cssText = 'background:none;border:none;cursor:pointer;color:var(--muted);font-size:16px;';
        close.setAttribute('aria-label', 'Dismiss notification');
        close.textContent = '×';
        close.addEventListener('click', () => toast.remove());

        body.append(title, detail);
        row.append(icon, body, close);
        toast.appendChild(row);
        this.container.appendChild(toast);
        setTimeout(() => toast.style.opacity = '0', duration - 300);
        setTimeout(() => toast.remove(), duration);
    }
};


const Sidebar = {
    init() {
        const sidebar = document.querySelector('.dashboard-sidebar');
        const overlay = document.querySelector('.dashboard-overlay');
        const toggleBtn = document.querySelector('#sidebar-toggle');
        const closeBtn = document.querySelector('#sidebar-close-btn');
        if (!sidebar) return;


        const isCollapsed = localStorage.getItem('ml-sidebar-collapsed') === 'true';
        if (isCollapsed && window.innerWidth >= 993) {
            document.body.classList.add('sidebar-hover-collapsed');
        } else {
            document.body.classList.remove('sidebar-hover-collapsed');
        }

        toggleBtn?.addEventListener('click', (e) => {
            e.preventDefault();
            if (window.innerWidth < 993) {

                this.toggleMobile(sidebar, overlay);
            } else {

                document.body.classList.toggle('sidebar-hover-collapsed');
                const collapsed = document.body.classList.contains('sidebar-hover-collapsed');
                localStorage.setItem('ml-sidebar-collapsed', collapsed);
            }
        });

        closeBtn?.addEventListener('click', (e) => {
            e.preventDefault();
            this.closeMobile(sidebar, overlay);
        });

        overlay?.addEventListener('click', () => this.closeMobile(sidebar, overlay));


        sidebar.querySelectorAll('.sidebar-nav-item').forEach(link => {
            link.addEventListener('click', () => {
                if (window.innerWidth < 993) {
                    this.closeMobile(sidebar, overlay);
                }
            });
        });
    },
    toggleMobile(sidebar, overlay) {
        sidebar.classList.toggle('open');
        overlay?.classList.toggle('active');
    },
    closeMobile(sidebar, overlay) {
        sidebar.classList.remove('open');
        overlay?.classList.remove('active');
    }
};


const Cart = {
    addItemDefault(button, productId) {
        let quantities = [];
        try {
            quantities = JSON.parse(button?.dataset?.quantities || "[]")
                .map(Number)
                .filter(q => Number.isFinite(q) && q > 0);
        } catch {
            quantities = [];
        }
        const quantity = quantities.length ? quantities[0] : 1;
        return this.addItem(productId, quantity);
    },

    async addItem(productId, quantityKg) {
        if (!isAuthenticated()) {
            window.location.href = window.MarketLinkRoutes.login + '?returnUrl=' + encodeURIComponent(window.location.pathname);
            return;
        }
        try {
            const response = await fetch(window.MarketLinkRoutes.cartAdd, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
                body: JSON.stringify({ productId, quantityKg })
            });




            if (response.status === 401 || response.status === 403 || response.redirected) {
                window.location.href = window.MarketLinkRoutes.login + '?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            if (!response.ok) {
                Toast.show('Could not add this item right now. Please try again.', 'error');
                return;
            }

            const data = await response.json();
            if (data.success) {
                Toast.show(`Added to cart (${quantityKg} unit${quantityKg > 1 ? 's' : ''}).`, 'success');
                this.updateBadge(data.cartCount);
            } else {
                Toast.show(data.message || 'Failed to add to cart', 'error');
            }
        } catch (e) {
            Toast.show('Network error. Please try again.', 'error');
        }
    },
    updateBadge(count) {
        document.querySelectorAll('.cart-badge').forEach(el => {
            el.textContent = count;
            el.style.display = count > 0 ? 'flex' : 'none';
        });
    }
};


const Favorites = {

    async toggle(type, id, btn) {
        if (!isAuthenticated()) {
            window.location.href = window.MarketLinkRoutes.login + '?returnUrl=' + encodeURIComponent(window.location.pathname);
            return;
        }

        const body = new URLSearchParams();
        body.append('type', type);
        body.append('id', id);

        try {
            const response = await fetch(window.MarketLinkRoutes.favoriteToggle, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: body.toString()
            });

            if (response.status === 404 || response.redirected) {
                window.location.href = window.MarketLinkRoutes.login;
                return;
            }

            const data = await response.json();
            if (!data.success) {
                Toast.show(data.message || 'Please use a buyer account to save favorites.', 'warning');
                return;
            }

            this.paint(btn, data.isFavorite);
            Toast.show(data.isFavorite ? 'Added to favorites!' : 'Removed from favorites', data.isFavorite ? 'success' : 'info');
        } catch (e) {
            Toast.show('Could not reach the server. Please try again.', 'error');
        }
    },
    paint(btn, isFavorite) {
        if (!btn) return;
        btn.classList.toggle('active', isFavorite);
        const icon = btn.querySelector('i');
        if (icon) icon.className = isFavorite ? 'fas fa-heart text-danger' : 'far fa-heart';
        btn.style.color = isFavorite ? '#dc3545' : '#6c757d';
        btn.title = isFavorite ? 'Remove from favorites' : 'Save to favorites';
    }
};

function isAuthenticated() {
    return document.body.dataset.authenticated === 'true';
}


function initQtySelector() {
    document.querySelectorAll('.qty-option').forEach(opt => {
        opt.addEventListener('click', function () {
            const group = this.closest('.qty-selector');
            group?.querySelectorAll('.qty-option').forEach(o => o.classList.remove('selected'));
            this.classList.add('selected');
            const input = group?.nextElementSibling;
            if (input?.type === 'hidden') input.value = this.dataset.qty;
        });
    });
}


const AIAssistant = {
    panel: null,
    input: null,
    sendBtn: null,
    messages: null,
    sending: false,
    init() {
        this.panel = document.querySelector('.ai-chat-panel');
        this.input = document.querySelector('.ai-chat-input input');
        this.sendBtn = document.querySelector('.ai-chat-input button');
        this.messages = document.querySelector('.ai-chat-messages');
        const toggle = document.querySelector('.ai-toggle-btn');

        if (!this.panel || !window.MarketLinkRoutes?.assistantAsk) return;

        toggle?.addEventListener('click', () => {
            this.panel?.classList.toggle('open');
            if (this.panel?.classList.contains('open') && this.messages?.children.length === 0) {
                const label = this.panel.querySelector('.ai-chat-header h6')?.textContent || 'Assistant';
                this.addMessage('bot', `👋 Hi! I'm your ${label}. Ask me anything about your MarketLink account.`);
            }
        });

        this.input?.addEventListener('keypress', e => { if (e.key === 'Enter') this.sendMessage(); });
        this.sendBtn?.addEventListener('click', () => this.sendMessage());
    },
    addMessage(type, text) {
        const msg = document.createElement('div');
        msg.className = `ai-msg ${type}`;
        msg.textContent = text;
        this.messages?.appendChild(msg);
        if (this.messages) this.messages.scrollTop = this.messages.scrollHeight;
        return msg;
    },
    setBusy(busy) {
        this.sending = busy;
        if (this.input) this.input.disabled = busy;
        if (this.sendBtn) this.sendBtn.disabled = busy;
    },
    async sendMessage() {

        if (this.sending) return;

        const text = this.input?.value.trim();
        if (!text) return;

        this.addMessage('user', text);
        if (this.input) this.input.value = '';
        this.setBusy(true);
        const typingMsg = this.addMessage('bot typing', '…thinking…');

        try {
            const response = await fetch(window.MarketLinkRoutes.assistantAsk, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
                body: JSON.stringify({ message: text })
            });

            typingMsg.remove();

            if (response.status === 401 || response.status === 403 || response.redirected) {
                this.addMessage('bot', 'Please sign in to use the assistant.');
                return;
            }

            if (!response.ok) {
                this.addMessage('bot', 'The assistant is temporarily unavailable. Please try again shortly.');
                return;
            }

            const data = await response.json();
            this.addMessage('bot', data.message || (data.success ? 'Sorry, I have no answer for that.' : 'Something went wrong. Please try again.'));
        } catch (e) {
            typingMsg.remove();
            this.addMessage('bot', 'Network error reaching the assistant. Please check your connection and try again.');
        } finally {
            this.setBusy(false);
        }
    }
};


function initProductGallery() {
    const thumbs = document.querySelectorAll('.product-thumb');
    const mainImg = document.querySelector('#product-main-image');
    thumbs.forEach(thumb => {
        thumb.addEventListener('click', function () {
            if (mainImg) mainImg.src = this.dataset.full;
            thumbs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
        });
    });
}


function animateCounters() {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    document.querySelectorAll('[data-count]').forEach(el => {
        const goal = parseInt(el.dataset.count, 10);
        if (reduceMotion) { el.textContent = goal.toLocaleString(); return; }

        const runFor = 1400;
        const began = performance.now();

        const tick = now => {
            const t = Math.min((now - began) / runFor, 1);
            const eased = 1 - Math.pow(1 - t, 3);   // slows down near the end
            el.textContent = Math.round(goal * eased).toLocaleString();
            if (t < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    });
}


function initScrollAnimations() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-up');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });
    document.querySelectorAll('.animate-on-scroll').forEach(el => observer.observe(el));
}


function initMarketMap(mapId, locations) {
    if (!document.getElementById(mapId) || typeof L === 'undefined') return;
    const map = L.map(mapId).setView([39.5, -98.35], 4);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© <a href="https://www.openstreetmap.org/">OpenStreetMap</a> contributors'
    }).addTo(map);

    const greenIcon = L.icon({
        iconUrl: 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="32" height="40" viewBox="0 0 32 40"%3E%3Cpath fill="%232f6f4e" d="M16 0C7.2 0 0 7.2 0 16c0 12 16 24 16 24s16-12 16-24C32 7.2 24.8 0 16 0z"/%3E%3Ccircle cx="16" cy="16" r="6" fill="white"/%3E%3C/svg%3E',
        iconSize: [32, 40],
        iconAnchor: [16, 40],
        popupAnchor: [0, -40]
    });

    locations?.forEach(loc => {
        const popup = document.createElement('div');

        const name = document.createElement('strong');
        name.textContent = loc.name ?? 'Market';
        popup.appendChild(name);

        const address = document.createElement('div');
        address.textContent = loc.address ?? '';
        popup.appendChild(address);

        if (loc.url) {
            try {
                const linkUrl = new URL(loc.url, window.location.origin);
                if (linkUrl.protocol === 'http:' || linkUrl.protocol === 'https:') {
                    const br = document.createElement('br');
                    popup.appendChild(br);
                    const link = document.createElement('a');
                    link.href = linkUrl.href;
                    link.target = '_blank';
                    link.rel = 'noopener noreferrer';
                    link.textContent = 'View details';
                    popup.appendChild(link);
                }
            } catch {

            }
        }

        L.marker([loc.lat, loc.lng], { icon: greenIcon })
            .addTo(map)
            .bindPopup(popup);
    });
    return map;
}


function openDirections(latitude, longitude) {
    const destination = `${latitude},${longitude}`;
    const open = (origin) => {
        const url = origin
            ? `https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route=${origin};${destination}`
            : `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=16/${latitude}/${longitude}`;
        window.open(url, '_blank', 'noopener');
    };
    if (!navigator.geolocation) { open(null); return; }
    navigator.geolocation.getCurrentPosition(
        position => open(`${position.coords.latitude},${position.coords.longitude}`),
        () => open(null),
        { enableHighAccuracy: true, timeout: 8000 }
    );
}


function initSalesChart(canvasId, labels, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx || typeof Chart === 'undefined') return;
    const chartCtx = ctx.getContext('2d');
    const gradient = chartCtx.createLinearGradient(0, 0, 0, ctx.height || 220);
    gradient.addColorStop(0, 'rgba(92,111,43,.35)');
    gradient.addColorStop(1, 'rgba(92,111,43,0)');
    return new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [{
                label: 'Revenue',
                data,
                borderColor: '#5C6F2B',
                backgroundColor: gradient,
                borderWidth: 2.5,
                fill: true,
                tension: 0.4,
                pointBackgroundColor: '#fff',
                pointBorderColor: '#5C6F2B',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (item) => '$' + Number(item.raw).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(92,111,43,.08)' },
                    ticks: { callback: (val) => '$' + Number(val).toLocaleString() }
                },
                x: { grid: { display: false } }
            }
        }
    });
}

function initDonutChart(canvasId, labels, data, colors) {
    const ctx = document.getElementById(canvasId);
    if (!ctx || typeof Chart === 'undefined') return;
    return new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels,
            datasets: [{
                data,
                backgroundColor: colors || ['#5C6F2B', '#DE802B', '#D8E983', '#9CAB84', '#D8C9A7'],
                borderColor: '#fff',
                borderWidth: 2,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'bottom', labels: { padding: 16, usePointStyle: true } },
                tooltip: {
                    callbacks: {
                        label: (item) => `${item.label}: ${item.raw}`
                    }
                }
            },
            cutout: '65%'
        }
    });
}


function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

async function postJson(url, data) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
        body: JSON.stringify(data)
    });
    return res.json();
}


const Notifications = {
    async refresh(url) {
        try {
            const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!res.ok) return;
            const data = await res.json();
            document.querySelectorAll('.notif-badge').forEach(el => {
                el.textContent = data.count;
                el.style.display = data.count > 0 ? 'flex' : 'none';
            });
        } catch {  }
    },
    startPolling(interval = 30000) {
        const url = document.body.dataset.notificationsUrl;
        if (!url) return;
        this.refresh(url);
        setInterval(() => this.refresh(url), interval);
    }
};


function initPasswordToggles() {
    document.querySelectorAll('.password-toggle').forEach(button => {
        button.addEventListener('click', () => {
            const field = button.closest('.password-field');
            const input = field?.querySelector('.password-input');
            const icon = button.querySelector('i');
            if (!input) return;
            const visible = input.type === 'text';
            input.type = visible ? 'password' : 'text';
            button.setAttribute('aria-label', visible ? 'Show password' : 'Hide password');
            icon?.classList.toggle('fa-eye', visible);
            icon?.classList.toggle('fa-eye-slash', !visible);
        });
    });
}

function initAjaxProductSearch() {
    const inputs = Array.from(document.querySelectorAll('input[type="search"], input[name="q"], input[placeholder*="Search" i], input[placeholder*="search" i]'))
        .filter((input, index, list) => list.indexOf(input) === index && !input.dataset.ajaxSearchReady);

    inputs.forEach(input => {
        input.dataset.ajaxSearchReady = 'true';
        let timer;
        let controller;
        const wrapper = input.closest('.input-group')?.parentElement || input.parentElement;
        wrapper.style.position = 'relative';

        const dropdown = document.createElement('div');
        dropdown.className = 'ajax-search-results';
        dropdown.hidden = true;
        wrapper.appendChild(dropdown);

        const hide = () => {
            dropdown.hidden = true;
        };

        input.addEventListener('input', () => {
            clearTimeout(timer);
            const q = input.value.trim();
            if (q.length < 2) {
                hide();
                return;
            }

            timer = setTimeout(async () => {
                controller?.abort();
                controller = new AbortController();
                try {
                    const response = await fetch(`/Products/Autocomplete?q=${encodeURIComponent(q)}`, {
                        headers: { 'Accept': 'application/json' },
                        signal: controller.signal
                    });
                    if (!response.ok) throw new Error('search failed');
                    const results = await response.json();
                    dropdown.replaceChildren();

                    if (!results.length) {
                        const empty = document.createElement('div');
                        empty.className = 'ajax-search-empty';
                        empty.textContent = 'No matching produce found';
                        dropdown.appendChild(empty);
                    } else {
                        results.slice(0, 8).forEach(result => {
                            const link = document.createElement('a');
                            link.className = 'ajax-search-item';
                            link.href = `/Products/${encodeURIComponent(result.slug ?? '')}`;

                            const image = document.createElement('img');
                            image.src = typeof result.imageUrl === 'string' && /^(https?:\/\/|\/)/i.test(result.imageUrl)
                                ? result.imageUrl
                                : '/images/placeholder.svg';
                            image.alt = '';
                            image.loading = 'lazy';

                            const content = document.createElement('span');
                            const name = document.createElement('strong');
                            name.textContent = result.name ?? 'Produce';
                            const meta = document.createElement('small');
                            meta.textContent = `${result.categoryName ?? 'Produce'} · ${result.farmerName ?? 'Local farmer'} · $${Number(result.pricePerKg ?? 0).toFixed(2)}/kg`;
                            content.append(name, meta);
                            link.append(image, content);
                            dropdown.appendChild(link);
                        });
                    }
                    dropdown.hidden = false;
                } catch (error) {
                    if (error.name !== 'AbortError') hide();
                }
            }, 250);
        });

        input.addEventListener('keydown', event => {
            if (event.key === 'Escape') hide();
        });

        document.addEventListener('click', event => {
            if (!wrapper.contains(event.target)) hide();
        });
    });
}

function initPickupDateSelectors() {
    document.querySelectorAll('select[id^="pickup-slot-"]').forEach(select => {
        const timeInputId = select.dataset.timeInput;
        const timeInput = document.getElementById(timeInputId);
        if (!timeInput) return;

        const updateDate = () => {
            const option = select.selectedOptions[0];
            if (!option?.dataset.day || !option.dataset.start) return;

            const targetDay = Number(option.dataset.day);
            const [hours, minutes] = option.dataset.start.split(':').map(Number);
            const now = new Date();
            const candidate = new Date(now);
            let daysAhead = (targetDay - now.getDay() + 7) % 7;
            candidate.setHours(hours, minutes, 0, 0);
            if (daysAhead === 0 && candidate <= now) daysAhead = 7;
            candidate.setDate(now.getDate() + daysAhead);

            const pad = value => String(value).padStart(2, '0');
            timeInput.value = `${candidate.getFullYear()}-${pad(candidate.getMonth() + 1)}-${pad(candidate.getDate())}T${pad(candidate.getHours())}:${pad(candidate.getMinutes())}`;
            timeInput.min = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;
        };

        select.addEventListener('change', updateDate);
        updateDate();
    });
}

document.addEventListener('DOMContentLoaded', () => {
    DarkMode.init();
    Toast.init();
    Sidebar.init();
    AIAssistant.init();
    initQtySelector();
    initPasswordToggles();
    initAjaxProductSearch();
    initPickupDateSelectors();
    initProductGallery();
    initScrollAnimations();
    // Navigation search is already handled by initAjaxProductSearch().


    document.querySelectorAll('.alert.auto-dismiss').forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });


    const countersSection = document.querySelector('[data-counters]');
    if (countersSection) {
        const obs = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting) { animateCounters(); obs.disconnect(); }
        });
        obs.observe(countersSection);
    }


    if (document.body.dataset.authenticated === 'true') {
        Notifications.startPolling();
    }
});


window.ML = { Cart, Favorites, Toast, DarkMode, initSalesChart, initDonutChart, initMarketMap, openDirections };
