const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');
const assert = require('node:assert/strict');

function setup(language, tagName, validity) {
    const listeners = {};
    const control = {
        tagName,
        dataset: {},
        validity,
        customMessage: '',
        setCustomValidity(message) { this.customMessage = message; },
        addEventListener(name, callback) { listeners[name] = callback; }
    };
    const document = {
        readyState: 'complete',
        documentElement: { lang: language },
        querySelectorAll: () => [control],
        addEventListener() {}
    };
    const source = fs.readFileSync(
        path.resolve(__dirname, '../../OnlineStore/wwwroot/js/validation-messages.js'),
        'utf8');
    vm.runInNewContext(source, { document, navigator: { language } });
    return { control, listeners };
}

test('uses an English required message in English mode', () => {
    const view = setup('en-GB', 'INPUT', { valueMissing: true });
    view.listeners.invalid();
    assert.equal(view.control.customMessage, 'Please fill out this field.');
});

test('uses an English select message for a missing English selection', () => {
    const view = setup('en-GB', 'SELECT', { valueMissing: true });
    view.listeners.invalid();
    assert.equal(view.control.customMessage, 'Please select an item in the list.');
});

test('uses Chinese validation messages in Chinese mode', () => {
    const view = setup('zh-CN', 'SELECT', { valueMissing: true });
    view.listeners.invalid();
    assert.equal(view.control.customMessage, '请在列表中选择一项。');
});

test('clears the custom message after the user changes the field', () => {
    const view = setup('en-GB', 'INPUT', { valueMissing: true });
    view.listeners.invalid();
    view.listeners.input();
    assert.equal(view.control.customMessage, '');
});
