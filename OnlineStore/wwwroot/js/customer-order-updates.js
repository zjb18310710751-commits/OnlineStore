(() => {
    const container = document.querySelector('[data-customer-order-updates]');
    if (!container) return;

    const rows = new Map(Array.from(container.querySelectorAll('[data-order-id]'), row => [
        row.dataset.orderId,
        row
    ]));

    let inFlight = false;
    let stopped = false;
    let request = null;
    let timer = null;
    let reloadPending = false;

    function stop() {
        stopped = true;
        window.clearInterval(timer);
        request?.abort();
    }

    async function updateOrders() {
        if (document.hidden || inFlight || stopped) return;
        inFlight = true;
        request = new AbortController();
        try {
            const response = await fetch(container.dataset.orderUpdatesUrl, {
                cache: 'no-store',
                credentials: 'same-origin',
                signal: request.signal
            });
            if (response.redirected || response.status === 401 || response.status === 403) {
                stop();
                return;
            }
            if (!response.ok) return;

            const data = await response.json();
            if (stopped || !data.success || !Array.isArray(data.orders)) return;

            const hasChanged = data.orders.length !== rows.size || data.orders.some(state => {
                const row = rows.get(String(state.id));
                return row
                    && typeof state.stateKey === 'string'
                    && row.getAttribute('data-order-state-key') !== state.stateKey;
            });
            if (hasChanged) reloadPending = true;
            const hasOpenEditor = Boolean(document.querySelector(
                '.modal.show, input:focus, select:focus, textarea:focus'));
            if (reloadPending && !hasOpenEditor) {
                stopped = true;
                window.clearInterval(timer);
                window.location.reload();
            }
        } catch {
        } finally {
            inFlight = false;
            request = null;
        }
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
