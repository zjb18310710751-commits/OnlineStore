(() => {
    const container = document.querySelector('[data-admin-after-sales-updates]');
    if (!container || !container.dataset.afterSalesUpdatesUrl) return;

    const rows = new Map(Array.from(container.querySelectorAll('[data-after-sales-id]'), row => [
        row.dataset.afterSalesId,
        row
    ]));

    let inFlight = false;
    let stopped = false;
    let reloadPending = false;
    let request = null;
    let timer = null;

    function stop() {
        stopped = true;
        window.clearInterval(timer);
        request?.abort();
    }

    async function updateAfterSales() {
        if (document.hidden || inFlight || stopped) return;
        inFlight = true;
        request = new AbortController();
        try {
            const response = await fetch(container.dataset.afterSalesUpdatesUrl, {
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
            if (stopped || !data.success || !Array.isArray(data.requests)) return;
            reloadPending = Number(data.totalCount) !== Number(container.dataset.afterSalesTotal)
                || Number(data.openCount) !== Number(container.dataset.afterSalesOpen)
                || data.requests.length !== rows.size
                || data.requests.some(state => {
                const row = rows.get(String(state.id));
                return row
                    && typeof state.stateKey === 'string'
                    && row.getAttribute('data-after-sales-state-key') !== state.stateKey;
                });
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
        timer = window.setInterval(updateAfterSales, 1000);
        updateAfterSales();
    }

    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) updateAfterSales();
    });
    window.addEventListener('pagehide', stop);
    window.addEventListener('pageshow', event => {
        if (event.persisted && stopped) start();
    });
    start();
})();
