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
            orders: [{ id: 8, status: 'shipped', statusLabel: 'Shipped', stateKey: 'shipped|' }]
        })
    });
    const row = {
        dataset: { orderId: '8' },
        attributes: { 'data-order-state-key': 'shipped|' },
        getAttribute(name) {
            return this.attributes[name] ?? null;
        }
    };
    const container = {
        dataset: { orderUpdatesUrl: '/Orders/OrderUpdates' },
        querySelectorAll: () => [row]
    };
    const document = {
        hidden: false,
        openEditor: false,
        activeElement: null,
        querySelector(selector) {
            if (selector === '[data-customer-order-updates]') return container;
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
    const file = path.resolve(__dirname, '../../OnlineStore/wwwroot/js/customer-order-updates.js');
    vm.runInNewContext(fs.readFileSync(file, 'utf8'), {
        document,
        window,
        AbortController,
        fetch: async (url, options) => {
            calls += 1;
            assert.equal(url, container.dataset.orderUpdatesUrl);
            return response(options);
        }
    });

    const flush = () => new Promise(resolve => setImmediate(resolve));
    return {
        document,
        handlers,
        timers,
        flush,
        calls: () => calls,
        reloads: () => reloads,
        respond: callback => { response = callback; },
        tick: async () => {
            for (const callback of timers.values()) callback();
            await flush();
        }
    };
}

test('keeps the current order page while its state is unchanged', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, orders: [
            { id: 8, status: 'shipped', statusLabel: 'Shipped', stateKey: 'shipped|' }
        ] })
    }));
    await view.tick();

    assert.equal(view.reloads(), 0);
});

test('reloads once when the order becomes delivered', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, orders: [
            { id: 8, status: 'delivered', statusLabel: 'Delivered', stateKey: 'delivered|' }
        ] })
    }));
    await view.tick();
    const calls = view.calls();
    await view.tick();

    assert.equal(view.reloads(), 1);
    assert.equal(view.calls(), calls);
});

test('reloads when an after-sales request changes without an order status change', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, orders: [
            { id: 8, status: 'shipped', statusLabel: 'Shipped', stateKey: 'shipped|12:已拒绝' }
        ] })
    }));
    await view.tick();

    assert.equal(view.reloads(), 1);
});

test('defers a reload while the customer is editing a form or modal', async () => {
    const view = setup();
    await view.flush();
    view.document.openEditor = true;
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, orders: [
            { id: 8, status: 'delivered', statusLabel: 'Delivered', stateKey: 'delivered|' }
        ] })
    }));
    await view.tick();
    assert.equal(view.reloads(), 0);

    view.document.openEditor = false;
    await view.tick();
    assert.equal(view.reloads(), 1);
});

test('hidden tabs pause polling and resume with an immediate check', async () => {
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

test('expired sessions stop polling instead of requesting the login page repeatedly', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ redirected: true, ok: true }));
    await view.tick();
    const calls = view.calls();
    await view.tick();

    assert.equal(view.calls(), calls);
});
