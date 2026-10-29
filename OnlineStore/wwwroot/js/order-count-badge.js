(() => {
    const link = document.querySelector('[data-active-item-count-url]');
    const badge = document.querySelector('[data-active-item-count-badge]');
    if (!link || !badge || !link.dataset.activeItemCountUrl) return;

    const isChinese = (document.documentElement?.lang || '').toLowerCase().startsWith('zh');
    let inFlight = false;
    let stopped = false;
    let request = null;
    let timer = null;

    function stop() {
        stopped = true;
        window.clearInterval(timer);
        request?.abort();
    }

    function render(count) {
        const normalized = Number.isFinite(count) && count > 0 ? Math.floor(count) : 0;
        const changed = badge.textContent !== String(normalized) || badge.hidden !== (normalized === 0);
        badge.textContent = String(normalized);
        badge.hidden = normalized === 0;
        if (changed) {
            badge.setAttribute(
                'aria-label',
                isChinese
                    ? `${normalized} 件进行中商品`
                    : `${normalized} active ${normalized === 1 ? 'item' : 'items'}`);
        }
    }

    async function updateCount() {
        if (document.hidden || inFlight || stopped) return;
        inFlight = true;
        request = new AbortController();
        try {
            const response = await fetch(link.dataset.activeItemCountUrl, {
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
            if (!stopped && data.success) render(Number(data.count));
        } catch {
        } finally {
            inFlight = false;
            request = null;
        }
    }

    function start() {
        stopped = false;
        timer = window.setInterval(updateCount, 3000);
        updateCount();
    }

    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) updateCount();
    });
    window.addEventListener('pagehide', stop);
    window.addEventListener('pageshow', event => {
        if (event.persisted && stopped) start();
    });
    start();
})();
