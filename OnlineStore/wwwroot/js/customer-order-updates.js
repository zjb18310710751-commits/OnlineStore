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
    let nextAttemptAt = 0;
    const isChinese = (document.documentElement?.lang || 'en').toLowerCase().startsWith('zh');
    const messages = isChinese ? {
        unavailable: '订单更新服务暂不可用。当前内容可能不是最新状态；将自动重试，也可手动重试。',
        failed: '暂时无法更新订单。请检查网络；将自动重试，也可手动重试。',
        expired: '登录状态已失效或无法访问订单。请重新加载页面后登录。',
        pending: '订单已有更新。为保留当前操作，暂未刷新；准备好后可刷新页面。',
        restored: '订单实时更新已恢复。',
        retry: '重试', reload: '重新加载', refresh: '立即刷新', check: '再次检查'
    } : {
        unavailable: 'Order updates are temporarily unavailable. These details may be out of date. We will retry automatically, or you can retry now.',
        failed: 'Order updates could not be loaded. Check your connection. We will retry automatically, or you can retry now.',
        expired: 'Your session has expired or order access is unavailable. Reload the page to sign in again.',
        pending: 'Your orders have updated. Refresh is waiting to keep your current action in place. Refresh when ready.',
        restored: 'Live order updates have been restored.',
        retry: 'Retry now', reload: 'Reload page', refresh: 'Refresh now', check: 'Check again'
    };
    const feedback = document.createElement('div');
    feedback.className = 'ui-inline-feedback mb-3';
    feedback.hidden = true;
    feedback.setAttribute('role', 'status');
    feedback.setAttribute('aria-live', 'polite');
    feedback.setAttribute('aria-atomic', 'true');
    const feedbackMessage = document.createElement('span');
    const feedbackAction = document.createElement('button');
    feedbackAction.type = 'button';
    feedbackAction.className = 'ui-button ui-button-secondary ms-2';
    feedback.append(feedbackMessage, feedbackAction);
    container.before(feedback);

    function showFeedback(message, action, tone = 'info') {
        feedback.hidden = false;
        feedback.dataset.tone = tone;
        // Do not repeatedly announce the same outage on every poll.
        if (feedbackMessage.textContent !== message) feedbackMessage.textContent = message;
        if (feedbackAction.textContent !== action) feedbackAction.textContent = action;
    }

    function clearFeedback() {
        if (feedback.contains(document.activeElement)) {
            showFeedback(messages.restored, messages.check);
        } else {
            feedback.hidden = true;
            feedbackMessage.textContent = '';
        }
    }

    feedbackAction.addEventListener('click', () => {
        if (reloadPending || stopped) {
            stop();
            window.location.reload();
        } else {
            nextAttemptAt = 0;
            updateOrders();
        }
    });

    function stop() {
        stopped = true;
        window.clearInterval(timer);
        request?.abort();
    }

    async function updateOrders() {
        if (document.hidden || inFlight || stopped || Date.now() < nextAttemptAt) return;
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
                showFeedback(messages.expired, messages.reload, 'error');
                return;
            }
            if (!response.ok) {
                const retryAfter = Number(response.headers?.get('Retry-After'));
                if (Number.isFinite(retryAfter) && retryAfter > 0) {
                    nextAttemptAt = Date.now() + Math.min(retryAfter, 60) * 1000;
                }
                showFeedback(response.status === 503 ? messages.unavailable : messages.failed, messages.retry, 'error');
                return;
            }

            const data = await response.json();
            if (stopped) return;
            if (!data.success || !Array.isArray(data.orders)) {
                showFeedback(messages.failed, messages.retry, 'error');
                return;
            }
            nextAttemptAt = 0;

            const hasChanged = data.orders.length !== rows.size || data.orders.some(state => {
                const row = rows.get(String(state.id));
                return row
                    && typeof state.stateKey === 'string'
                    && row.getAttribute('data-order-state-key') !== state.stateKey;
            });
            if (hasChanged) reloadPending = true;
            const hasOpenEditor = Boolean(document.querySelector(
                '.modal.show, dialog[open], input:focus, select:focus, textarea:focus'));
            const hasFocusedAction = Boolean(document.activeElement?.matches?.(
                'button, a[href], summary, [role="combobox"], [role="button"], [contenteditable="true"]'));
            if (reloadPending && !hasOpenEditor && !hasFocusedAction) {
                stop();
                window.location.reload();
            } else if (reloadPending) {
                showFeedback(messages.pending, messages.refresh);
            } else {
                clearFeedback();
            }
        } catch {
            if (!stopped) showFeedback(messages.failed, messages.retry, 'error');
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
