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
            openCount: 1,
            requests: [{ id: 12, status: '待审核', stateKey: '待审核|||' }]
        })
    });
    const row = {
        dataset: { afterSalesId: '12' },
        attributes: { 'data-after-sales-state-key': '待审核|||' },
        getAttribute(name) {
            return this.attributes[name] ?? null;
        }
    };
    const container = {
        dataset: {
            afterSalesUpdatesUrl: '/Admin/Orders/AfterSalesUpdates?ids=12',
            afterSalesTotal: '1',
            afterSalesOpen: '1'
        },
        querySelectorAll: () => [row]
    };
    const document = {
        hidden: false,
        openEditor: false,
        querySelector(selector) {
            if (selector === '[data-admin-after-sales-updates]') return container;
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
    const file = path.resolve(__dirname, '../../OnlineStore/wwwroot/js/admin-after-sales-updates.js');
    vm.runInNewContext(fs.readFileSync(file, 'utf8'), {
        document,
        window,
        AbortController,
        fetch: async (url, options) => {
            calls += 1;
            assert.equal(url, container.dataset.afterSalesUpdatesUrl);
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

test('refreshes the admin after-sales list when return tracking is submitted', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, totalCount: 1, openCount: 1, requests: [
            { id: 12, status: '退回中', stateKey: '退回中|顺丰速运|RETURN01|2026-10-28T23:00:00Z|||' }
        ] })
    }));
    await view.tick();

    assert.equal(view.reloads(), 1);
});

test('defers the admin reload while a tracking modal or form is open', async () => {
    const view = setup();
    await view.flush();
    view.document.openEditor = true;
    view.respond(async () => ({
        ok: true,
        json: async () => ({ success: true, totalCount: 1, openCount: 1, requests: [
            { id: 12, status: '已收货', stateKey: '已收货|顺丰速运|RETURN01|2026-10-28T23:00:00Z|2026-10-29T08:00:00Z||' }
        ] })
    }));
    await view.tick();
    assert.equal(view.reloads(), 0);

    view.document.openEditor = false;
    await view.tick();
    assert.equal(view.reloads(), 1);
});

test('admin aftersales polling stops when the session expires', async () => {
    const view = setup();
    await view.flush();
    view.respond(async () => ({ redirected: true, ok: true }));
    await view.tick();
    const calls = view.calls();
    await view.tick();

    assert.equal(view.calls(), calls);
});
