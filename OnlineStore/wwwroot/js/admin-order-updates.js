(() => {
    const table = document.querySelector('[data-admin-order-updates]');
    if (!table) return;

    const rows = new Map(Array.from(table.querySelectorAll('[data-order-id]'), row => [row.dataset.orderId, row]));
    if (!rows.size) return;
    let inFlight = false;
    let stopped = false;
    let request = null;
    let timer = null;

    async function updateOrders() {
        if (document.hidden || inFlight || stopped) return;
        inFlight = true;
        request = new AbortController();
        try {
            const response = await fetch(table.dataset.orderUpdatesUrl, {
                cache: 'no-store', credentials: 'same-origin', signal: request.signal
            });
            if (response.redirected || response.status === 401 || response.status === 403) {
                stop();
                return;
            }
            if (!response.ok) return;
            const data = await response.json();
            if (stopped || !data.success || !Array.isArray(data.orders)) return;
            for (const state of data.orders) {
                const row = rows.get(String(state.id));
                if (!row || typeof state.status !== 'string' || typeof state.statusLabel !== 'string') continue;
                const label = row.querySelector('[data-order-status-label]');
                if (label.textContent !== state.statusLabel) label.textContent = state.statusLabel;
                row.dataset.orderStatus = state.status;
                for (const actions of row.querySelectorAll('[data-order-actions-status]')) {
                    actions.hidden = !actions.dataset.orderActionsStatus.split(',').includes(state.status);
                }
                const note = row.querySelector('[data-order-note]');
                note.textContent = state.receiptNote || '';
                note.hidden = !state.receiptNote;
                note.classList.toggle('text-danger', Boolean(state.isReceiptException));
            }
        } catch {
        } finally {
            inFlight = false;
            request = null;
        }
    }

    function stop() {
        stopped = true;
        window.clearInterval(timer);
        request?.abort();
    }

    function start() {
        stopped = false;
        timer = window.setInterval(updateOrders, 1000);
        updateOrders();
    }

    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) updateOrders();
    });
    window.addEventListener('pagehide', stop);
    window.addEventListener('pageshow', event => {
        if (event.persisted && stopped) start();
    });
    start();
})();
