const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assert = require('node:assert/strict');

function setup(language = 'en') {
    const handlers = {};
    const timers = new Map();
    let nextTimer = 0;
    let calls = 0;
    let reloads = 0;
    let now = 0;
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
        querySelectorAll: () => [row],
        before(node) { this.feedback = node; }
    };
    const document = {
        documentElement: { lang: language },
        hidden: false,
        openEditor: false,
        activeElement: null,
        querySelector(selector) {
            if (selector === '[data-customer-order-updates]') return container;
            if (selector.includes('.modal.show')) {
                return this.openEditor ? {} : null;
            }
            return null;
        },
        createElement(tagName) {
            return {
                tagName, children: [], attributes: {}, dataset: {}, hidden: false,
                textContent: '', listeners: {},
                setAttribute(name, value) { this.attributes[name] = value; },
                getAttribute(name) { return this.attributes[name] ?? null; },
                append(...children) { this.children.push(...children); },
                contains(node) { return this === node || this.children.includes(node); },
                addEventListener(name, handler) { this.listeners[name] = handler; }
            };
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
        Date: { now: () => now },
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
        container,
        handlers,
        timers,
        flush,
        calls: () => calls,
        reloads: () => reloads,
        respond: callback => { response = callback; },
        tick: async () => {
            now += 1000;
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

test('does not discard keyboard focus on an order action when fresh data arrives', async () => {
    const view = setup();
    await view.flush();
    view.document.activeElement = { matches: selector => selector.includes('button') };
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, orders: [] }) }));
    await view.tick();
    assert.equal(view.reloads(), 0);
    assert.equal(view.container.feedback.hidden, false);
    assert.match(view.container.feedback.children[0].textContent, /updated/i);
    view.container.feedback.children[1].listeners.click();
    assert.equal(view.reloads(), 1);
});

test('503 shows a retryable status and successful polling clears it', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: false, status: 503 }));
    await view.tick();
    const feedback = view.container.feedback;
    assert.ok(feedback, 'An unavailable service must produce visible feedback');
    assert.equal(feedback.hidden, false);
    assert.equal(feedback.getAttribute('role'), 'status');
    assert.equal(feedback.getAttribute('aria-live'), 'polite');
    assert.match(feedback.children[0].textContent, /temporarily unavailable/i);
    assert.equal(feedback.children[1].hidden, false);
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, orders: [
        { id: 8, stateKey: 'shipped|' }
    ] }) }));
    feedback.children[1].listeners.click();
    await view.flush();
    assert.equal(feedback.hidden, true);
    assert.equal(view.reloads(), 0);
});

test('network failure exposes Chinese recovery guidance and does not steal focus', async () => {
    const view = setup('zh-CN');
    await view.flush();
    const focused = { matches: () => true };
    view.document.activeElement = focused;
    view.respond(async () => { throw new TypeError('Failed to fetch'); });
    await view.tick();
    assert.ok(view.container.feedback, 'Network failure must not remain silent');
    assert.match(view.container.feedback.children[0].textContent, /重试/);
    assert.equal(view.document.activeElement, focused);
    assert.equal(view.reloads(), 0);
});

test('session expiry offers a reload action and stops polling', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: false, status: 401 }));
    await view.tick();
    const calls = view.calls();
    await view.tick();
    assert.equal(view.calls(), calls);
    assert.ok(view.container.feedback, 'Expired sessions need an actionable explanation');
    assert.match(view.container.feedback.children[0].textContent, /session/i);
    view.container.feedback.children[1].listeners.click();
    assert.equal(view.reloads(), 1);
});

test('respects a service Retry-After delay without stopping future updates', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: false, status: 503, headers: { get: () => '5' } }));
    await view.tick();
    const calls = view.calls();
    for (let second = 0; second < 4; second += 1) await view.tick();
    assert.equal(view.calls(), calls);
    await view.tick();
    assert.equal(view.calls(), calls + 1);
});

test('successful retry keeps its focused action visible until the user moves away', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: false, status: 503 }));
    await view.tick();
    const feedback = view.container.feedback;
    assert.ok(feedback, 'A retry action must be rendered');
    view.document.activeElement = feedback.children[1];
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, orders: [
        { id: 8, stateKey: 'shipped|' }
    ] }) }));
    feedback.children[1].listeners.click();
    await view.flush();
    assert.equal(feedback.hidden, false);
    assert.equal(feedback.children[1].hidden, false);
    assert.match(feedback.children[0].textContent, /restored/i);
    view.document.activeElement = null;
    await view.tick();
    assert.equal(feedback.hidden, true);
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
