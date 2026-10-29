const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assert = require('node:assert/strict');

function element(tagName = 'div') {
    const listeners = {};
    const attributes = {};
    const classes = new Set();
    const children = [];
    const node = {
        tagName,
        children,
        dataset: {},
        id: '',
        textContent: '',
        disabled: false,
        focused: false,
        required: false,
        validationMessage: 'Please select an item in the list.',
        tabIndex: 0,
        selectedIndex: 1,
        form: null,
        classList: {
            add: (...names) => names.forEach(name => classes.add(name)),
            remove: (...names) => names.forEach(name => classes.delete(name)),
            toggle: (name, force) => force ? classes.add(name) : classes.delete(name),
            contains: name => classes.has(name)
        },
        append: (...nodes) => children.push(...nodes),
        appendChild: node => { children.push(node); return node; },
        addEventListener: (name, callback) => {
            (listeners[name] ||= []).push(callback);
        },
        fire: (name, event = {}) => (listeners[name] || []).forEach(callback => callback(event)),
        dispatchEvent: event => {
            (listeners[event.type] || []).forEach(callback => callback(event));
            return true;
        },
        setAttribute: (name, value) => { attributes[name] = String(value); },
        getAttribute: name => attributes[name] ?? null,
        removeAttribute: name => { delete attributes[name]; },
        matches: () => false,
        closest: () => null,
        scrollIntoView: () => {},
        getBoundingClientRect: () => ({ top: 0, bottom: 40, left: 0, right: 200 }),
        focus() { this.focused = true; },
        contains: node => children.includes(node)
    };
    Object.defineProperty(node, 'className', {
        get: () => [...classes].join(' '),
        set: value => {
            classes.clear();
            String(value).split(/\s+/).filter(Boolean).forEach(name => classes.add(name));
        }
    });
    return node;
}

function setup(configure = () => {}) {
    const select = element('select');
    select.id = 'brand';
    select.required = true;
    select.selectedIndex = 1;
    select.options = [
        { text: 'All brands', disabled: false },
        { text: 'Geek Digital Store', disabled: false }
    ];
    select.matches = selector => selector === ':invalid' && select.required;
    let changes = 0;
    select.addEventListener('change', () => { changes += 1; });

    const label = element('label');
    label.htmlFor = 'brand';
    configure(select, label);

    const container = element();
    select.parentNode = {
        insertBefore: node => container.children.push(node),
        appendChild: node => container.children.push(node)
    };

    const document = {
        querySelectorAll: selector => {
            if (selector.startsWith('select:')) return [select];
            if (selector === 'label[for]') return [label];
            return [];
        },
        createElement: element,
        addEventListener: () => {}
    };

    const source = fs.readFileSync(
        path.resolve(__dirname, '../../OnlineStore/wwwroot/js/site.js'),
        'utf8');
    const context = {
        document,
        window: { addEventListener: () => {} },
        MutationObserver: class { observe() {} },
        Event: class { constructor(type) { this.type = type; } },
        requestAnimationFrame: callback => callback(),
        setTimeout
    };
    vm.runInNewContext(`${source}\ninitCustomSelects();`, context);

    const wrapper = container.children[0];
    const trigger = wrapper.children.find(child => child.classList.contains('sf-select__trigger'));
    const menu = wrapper.children.find(child => child.classList.contains('sf-select__menu'));
    const key = key => trigger.fire('keydown', { key, preventDefault() {} });
    return { container, label, select, trigger, menu, key, changes: () => changes };
}

test('custom select reassigns the label and mirrors required state', () => {
    const view = setup();
    const wrapper = view.container.children[0];
    const trigger = wrapper.children.find(child => child.classList.contains('sf-select__trigger'));

    assert.notEqual(view.label.htmlFor, 'brand');
    assert.equal(trigger.getAttribute('aria-required'), 'true');
    assert.equal(trigger.getAttribute('aria-label'), null);
    assert.equal(trigger.children[0].textContent, 'Geek Digital Store');
});

test('choosing an option updates the native select and emits change once', () => {
    const view = setup();
    const wrapper = view.container.children[0];
    const trigger = wrapper.children.find(child => child.classList.contains('sf-select__trigger'));
    const menu = wrapper.children.find(child => child.classList.contains('sf-select__menu'));

    trigger.fire('click');
    menu.children[0].fire('click');

    assert.equal(view.select.selectedIndex, 0);
    assert.equal(view.changes(), 1);
    assert.equal(trigger.children[0].textContent, 'All brands');
});

test('Home and End select enabled boundary options without getting stuck on disabled placeholders', () => {
    const view = setup(select => {
        select.options = [
            { text: 'Choose', disabled: true },
            { text: 'First available', disabled: false },
            { text: 'Last available', disabled: false },
            { text: 'Unavailable', disabled: true }
        ];
        select.selectedIndex = 2;
    });
    view.key('Home');
    view.key('Enter');
    assert.equal(view.select.selectedIndex, 1);
    view.key('End');
    view.key('Enter');
    assert.equal(view.select.selectedIndex, 2);
    assert.equal(view.changes(), 2);
});

test('preserves an explicit accessible field name when the value changes', () => {
    const view = setup((select, label) => {
        label.htmlFor = 'unrelated';
        select.setAttribute('aria-label', 'Sort products');
    });
    view.trigger.fire('click');
    view.menu.children[0].fire('click');
    assert.equal(view.trigger.getAttribute('aria-label'), 'Sort products');
    assert.equal(view.menu.getAttribute('aria-label'), 'Sort products');
});

test('native validation exposes the error and focuses the visible control', () => {
    const view = setup(select => {
        select.setAttribute('aria-describedby', 'brand-help');
        select.setAttribute('aria-errormessage', 'brand-error');
    });
    let prevented = false;
    view.select.fire('invalid', { preventDefault() { prevented = true; } });
    assert.equal(prevented, true);
    assert.equal(view.trigger.focused, true);
    assert.equal(view.trigger.getAttribute('aria-invalid'), 'true');
    assert.ok(view.trigger.getAttribute('aria-describedby').split(' ').includes('brand-help'));
    assert.equal(view.trigger.getAttribute('aria-errormessage'), 'brand-error');
    const feedback = view.container.children[0].children.find(child => child.classList.contains('sf-select__error'));
    assert.equal(feedback.textContent, 'Please select an item in the list.');
    assert.equal(feedback.hidden, false);
    assert.ok(view.trigger.getAttribute('aria-describedby').split(' ').includes(feedback.id));
    view.select.matches = () => false;
    view.select.fire('change');
    assert.equal(feedback.hidden, true);
    assert.equal(view.trigger.getAttribute('aria-invalid'), 'false');
});

test('uses an existing aria-labelledby on both the field and option list', () => {
    const view = setup(select => select.setAttribute('aria-labelledby', 'sort-title'));
    assert.equal(view.trigger.getAttribute('aria-labelledby'), 'sort-title');
    assert.equal(view.menu.getAttribute('aria-labelledby'), 'sort-title');
});

test('keeps the native explicit aria-label precedence over a linked HTML label', () => {
    const view = setup(select => select.setAttribute('aria-label', 'Sort products'));
    assert.equal(view.trigger.getAttribute('aria-label'), 'Sort products');
    assert.equal(view.trigger.getAttribute('aria-labelledby'), null);
});

test('Escape cancels a highlighted option while Tab closes without trapping focus', () => {
    const view = setup();
    view.key('Home');
    view.key('Escape');
    assert.equal(view.select.selectedIndex, 1);
    assert.equal(view.changes(), 0);
    assert.equal(view.menu.hidden, true);
    view.trigger.focused = false;
    view.key('ArrowDown');
    view.key('Tab');
    assert.equal(view.menu.hidden, true);
    assert.equal(view.trigger.focused, false);
});

test('opening a menu under a stationary pointer does not cancel the keyboard choice', () => {
    const view = setup(select => {
        select.options.push({ text: 'Unavailable', disabled: true });
    });
    view.key('Home');
    view.menu.children[2].fire('mouseenter');
    view.key('Enter');
    assert.equal(view.select.selectedIndex, 0);
});
