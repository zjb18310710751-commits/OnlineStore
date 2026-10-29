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
    let response = async () => ({ ok: true, json: async () => ({ success: true, count: 1 }) });
    const link = { dataset: { activeItemCountUrl: '/Orders/ActiveItemCount' } };
    const badge = {
        textContent: '1',
        hidden: false,
        attributes: {},
        setAttribute(name, value) { this.attributes[name] = value; }
    };
    const document = {
        hidden: false,
        documentElement: { lang: 'en' },
        querySelector(selector) {
            if (selector === '[data-active-item-count-url]') return link;
            if (selector === '[data-active-item-count-badge]') return badge;
            return null;
        },
        addEventListener: (name, handler) => { handlers[name] = handler; }
    };
    const window = {
        setInterval: handler => { timers.set(++nextTimer, handler); return nextTimer; },
        clearInterval: id => timers.delete(id),
        addEventListener: (name, handler) => { handlers[name] = handler; }
    };
    const file = path.resolve(__dirname, '../../OnlineStore/wwwroot/js/order-count-badge.js');
    vm.runInNewContext(fs.readFileSync(file, 'utf8'), {
        document,
        window,
        AbortController,
        fetch: async (url, options) => {
            calls += 1;
            assert.equal(url, link.dataset.activeItemCountUrl);
            return response(options);
        }
    });

    const flush = () => new Promise(resolve => setImmediate(resolve));
    return {
        badge,
        document,
        handlers,
        flush,
        calls: () => calls,
        respond: callback => { response = callback; },
        tick: async () => {
            for (const callback of timers.values()) callback();
            await flush();
        }
    };
}

test('updates the active item count for a newly placed order', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, count: 2 }) }));
    await view.tick();

    assert.equal(view.badge.textContent, '2');
    assert.equal(view.badge.hidden, false);
    assert.equal(view.badge.attributes['aria-label'], '2 active items');
});

test('hides the badge when the order is fully completed', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ ok: true, json: async () => ({ success: true, count: 0 }) }));
    await view.tick();

    assert.equal(view.badge.textContent, '0');
    assert.equal(view.badge.hidden, true);
});

test('pauses count requests while the tab is hidden', async () => {
    const view = setup();
    await view.flush();
    const calls = view.calls();
    view.document.hidden = true;
    await view.tick();

    assert.equal(view.calls(), calls);
});

test('stops count polling when the session expires', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ redirected: true }));
    await view.tick();
    const calls = view.calls();
    await view.tick();

    assert.equal(view.calls(), calls);
});
