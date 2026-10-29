(() => {
    const language = (document.documentElement?.lang || navigator.language || 'en').toLowerCase();
    const isChinese = language.startsWith('zh');
    const messages = isChinese
        ? {
            required: '请填写此字段。',
            select: '请在列表中选择一项。',
            invalid: '请输入有效内容。',
            pattern: '请按要求的格式填写。',
            range: '请输入指定范围内的数值。',
            length: '请检查输入内容的长度。'
        }
        : {
            required: 'Please fill out this field.',
            select: 'Please select an item in the list.',
            invalid: 'Please enter a valid value.',
            pattern: 'Please match the requested format.',
            range: 'Please enter a value within the allowed range.',
            length: 'Please check the length of this value.'
        };

    function validationMessage(control) {
        const validity = control.validity;
        if (!validity) return '';
        if (validity.valueMissing) return control.tagName === 'SELECT' ? messages.select : messages.required;
        if (validity.typeMismatch || validity.badInput) return messages.invalid;
        if (validity.patternMismatch) return messages.pattern;
        if (validity.rangeUnderflow || validity.rangeOverflow || validity.stepMismatch) return messages.range;
        if (validity.tooShort || validity.tooLong) return messages.length;
        return '';
    }

    function enhance(control) {
        if (control.dataset.validationLanguageReady === 'true') return;
        control.dataset.validationLanguageReady = 'true';

        control.addEventListener('invalid', () => {
            const message = validationMessage(control);
            if (message) control.setCustomValidity(message);
        });
        const clear = () => control.setCustomValidity('');
        control.addEventListener('input', clear);
        control.addEventListener('change', clear);
    }

    function init() {
        document.querySelectorAll('input, select, textarea').forEach(enhance);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
