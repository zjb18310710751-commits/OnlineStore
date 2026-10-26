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
        required: false,
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
        focus: () => {},
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

function setup() {
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

    return { container, label, select, changes: () => changes };
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
