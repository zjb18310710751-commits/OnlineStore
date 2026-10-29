const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assert = require('node:assert/strict');

function setup() {
    const handlers = {};
    const timers = new Map();
    let nextTimer = 0;
    let calls = 0;
    let reloads = 0;
    let response = async () => ({
        ok: true,
        json: async () => ({
            success: true,
            totalCount: 1,
            orders: [{
                id: 8,
                status: 'shipped',
                statusLabel: 'Shipped',
                receiptNote: '',
                isReceiptException: false,
                stateKey: 'shipped|'
            }]
        })
    });
    const label = { textContent: 'Shipped' };
    const note = { textContent: '', classList: { toggle() {} } };
    const actions = ['shipped,delivered,exception', 'delivered', 'exception,completed'].map(statuses => ({
        dataset: { orderActionsStatus: statuses }, hidden: !statuses.includes('shipped')
    }));
    const row = {
        dataset: { orderId: '8', orderStatus: 'shipped' },
        attributes: { 'data-admin-order-state-key': 'shipped|' },
        getAttribute(name) {
            return this.attributes[name] ?? null;
        },
        querySelector: selector => selector === '[data-order-status-label]' ? label : note,
        querySelectorAll: () => actions
    };
    const table = {
        dataset: { orderUpdatesUrl: '/Admin/Orders/OrderUpdates?ids=8', adminOrderTotal: '1' },
        querySelectorAll: () => [row]
    };
    const document = {
        hidden: false,
        openEditor: false,
        querySelector(selector) {
            if (selector === '[data-admin-order-updates]') return table;
            if (selector === '.modal.show, input:focus, select:focus, textarea:focus') {
                return this.openEditor ? {} : null;
            }
            return null;
        },
        addEventListener: (name, handler) => { handlers[name] = handler; }
    };
    const window = {
        location: { reload: () => { reloads += 1; } },
        setInterval: handler => { timers.set(++nextTimer, handler); return nextTimer; },
        clearInterval: id => timers.delete(id),
        addEventListener: (name, handler) => { handlers[name] = handler; }
    };
    const file = path.resolve(__dirname, '../../OnlineStore/wwwroot/js/admin-order-updates.js');
    const source = fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : '';
    vm.runInNewContext(source, {
        document, window, AbortController,
        fetch: async (url, options) => { calls++; assert.equal(url, table.dataset.orderUpdatesUrl); return response(options); }
    });
    const flush = () => new Promise(resolve => setImmediate(resolve));
    return { row, label, note, actions, document, timers, handlers, flush,
        calls: () => calls,
        reloads: () => reloads,
        respond: callback => { response = callback; },
        tick: async () => { for (const callback of timers.values()) callback(); await flush(); }
    };
}

test('delivery shows receipt exception action and updates status without refreshing or opening a modal', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, totalCount: 1, orders: [
        { id: 8, status: 'delivered', statusLabel: 'Delivered', receiptNote: '', isReceiptException: false, stateKey: 'delivered|' }
    ] }) }));
    await view.tick();
    assert.equal(view.label.textContent, 'Delivered');
    assert.equal(view.actions[1].hidden, false);
    assert.equal(view.actions[0].hidden, false);
    assert.equal(view.actions[2].hidden, true);
    assert.equal(view.reloads(), 1);
    assert.equal(view.row.attributes['data-admin-order-state-key'], 'shipped|');
});

test('later completion hides receipt exception and exposes refund without replacing open form input', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, totalCount: 1, orders: [
        { id: 8, status: 'completed', statusLabel: 'Completed', receiptNote: 'Automatically confirmed', isReceiptException: false, stateKey: 'completed|' }
    ] }) }));
    await view.tick();
    assert.equal(view.label.textContent, 'Completed');
    assert.equal(view.actions[1].hidden, true);
    assert.equal(view.actions[2].hidden, false);
    assert.equal(view.note.textContent, 'Automatically confirmed');
    assert.equal(view.reloads(), 1);
});

test('hidden tabs pause requests and resuming the tab checks immediately', async () => {
    const view = setup();
    await view.flush();
    assert.equal(view.calls(), 1);
    view.document.hidden = true;
    await view.tick();
    assert.equal(view.calls(), 1);
    view.document.hidden = false;
    view.handlers.visibilitychange();
    await view.flush();
    assert.equal(view.calls(), 2);
});

test('a slow response is not overlapped and a network failure preserves current controls until retry succeeds', async () => {
    const view = setup();
    await view.flush();
    let reject;
    view.respond(() => new Promise((_, fail) => { reject = fail; }));
    await view.tick();
    await view.tick();
    assert.equal(view.calls(), 2);
    reject(new Error('offline'));
    await view.flush();
    assert.equal(view.label.textContent, 'Shipped');
    assert.equal(view.actions[1].hidden, true);
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, totalCount: 1, orders: [
        { id: 8, status: 'delivered', statusLabel: 'Delivered', stateKey: 'delivered|' }
    ] }) }));
    await view.tick();
    assert.equal(view.label.textContent, 'Delivered');
});

test('leaving the page cancels the timer and aborts pending work', async () => {
    const view = setup();
    await view.flush();
    let signal;
    view.respond(options => { signal = options.signal; return new Promise(() => {}); });
    await view.tick();
    view.handlers.pagehide();
    assert.equal(signal.aborted, true);
    assert.equal(view.timers.size, 0);
});

test('expired admin sessions stop polling instead of requesting the login page indefinitely', async () => {
    const view = setup();
    await view.flush();
    assert.equal(view.calls(), 1);
    view.respond(async () => ({ redirected: true, ok: true }));
    await view.tick();
    const calls = view.calls();
    await view.tick();
    assert.equal(view.calls(), calls);
});
