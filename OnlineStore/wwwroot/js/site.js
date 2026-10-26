function stepQty(btn, delta) {
    var input = btn.closest('.input-group').querySelector('input[type="number"]');
    if (!input) {
        return;
    }
    var min = input.min ? parseInt(input.min, 10) : 1;
    var max = input.max ? parseInt(input.max, 10) : Number.MAX_SAFE_INTEGER;
    var next = (parseInt(input.value, 10) || 0) + delta;
    input.value = Math.min(Math.max(next, min), max);
}

function setMainImage(url) {
    var main = document.getElementById('mainImage');
    if (!main) {
        return;
    }
    if (main.getAttribute('data-current') === url) {
        return;
    }
    main.setAttribute('data-current', url);

    var preload = new Image();
    preload.onload = function () {
        main.style.opacity = '0';
        setTimeout(function () {
            main.src = url;
            main.style.opacity = '1';
        }, 130);
    };
    preload.src = url;
}

function switchImage(btn, url) {
    setMainImage(url);
    document.querySelectorAll('.gallery-thumb').forEach(function (t) {
        t.classList.remove('active');
    });
    btn.classList.add('active');
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-send-code-url]').forEach(initSendCodeButton);
    initPasswordToggles();
    initCustomSelects();
});

function initCustomSelects() {
    var selectId = 0;

    document.querySelectorAll('select:not([multiple]):not([size])').forEach(function (select) {
        if (select.dataset.customSelectReady) return;
        select.dataset.customSelectReady = 'true';

        selectId += 1;
        var listboxId = 'sf-select-' + selectId;
        var wrapper = document.createElement('div');
        wrapper.className = 'sf-select';
        if (select.classList.contains('form-select')) {
            wrapper.classList.add('sf-select--block');
        }
        if (select.classList.contains('sf-filter-input')) {
            wrapper.classList.add('sf-select--block', 'sf-select--filter');
        }
        if (select.closest('.sf-sort-form') || select.closest('.sf-review-sort')) {
            wrapper.classList.add('sf-select--sort');
        }

        select.parentNode.insertBefore(wrapper, select);
        wrapper.appendChild(select);
        select.classList.add('sf-select__native');
        select.tabIndex = -1;
        select.setAttribute('aria-hidden', 'true');

        var trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.id = listboxId + '-trigger';
        trigger.className = 'sf-select__trigger';
        trigger.setAttribute('role', 'combobox');
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-controls', listboxId);
        trigger.setAttribute('aria-expanded', 'false');
        trigger.disabled = select.disabled;

        var value = document.createElement('span');
        value.className = 'sf-select__value';
        var chevron = document.createElement('span');
        chevron.className = 'sf-select__chevron';
        chevron.setAttribute('aria-hidden', 'true');
        trigger.append(value, chevron);

        var menu = document.createElement('div');
        menu.className = 'sf-select__menu';
        menu.id = listboxId;
        menu.setAttribute('role', 'listbox');
        menu.hidden = true;

        var options = Array.prototype.slice.call(select.options);
        var optionButtons = options.map(function (option, index) {
            var item = document.createElement('button');
            item.type = 'button';
            item.className = 'sf-select__option';
            item.id = listboxId + '-option-' + index;
            item.setAttribute('role', 'option');
            item.setAttribute('aria-selected', 'false');
            item.tabIndex = -1;
            item.textContent = option.text;
            item.disabled = option.disabled;
            item.dataset.optionIndex = String(index);
            item.addEventListener('mouseenter', function () {
                highlight(index);
            });
            item.addEventListener('mousedown', function (event) {
                event.preventDefault();
            });
            item.addEventListener('click', function () {
                choose(index);
            });
            menu.appendChild(item);
            return item;
        });

        wrapper.append(trigger, menu);

        var linkedLabels = select.id
            ? Array.prototype.slice.call(document.querySelectorAll('label[for]'))
                .filter(function (label) { return label.htmlFor === select.id; })
            : [];
        linkedLabels.forEach(function (label, index) {
            if (!label.id) label.id = listboxId + '-label-' + index;
            label.htmlFor = trigger.id;
        });
        if (linkedLabels.length > 0) {
            trigger.setAttribute('aria-labelledby', linkedLabels.map(function (label) { return label.id; }).join(' '));
        }

        function syncSelection() {
            var index = select.selectedIndex;
            var selected = index >= 0 ? options[index] : null;
            value.textContent = selected ? selected.text : '';
            if (linkedLabels.length === 0) {
                trigger.setAttribute('aria-label', selected ? selected.text : '');
            }
            optionButtons.forEach(function (item, optionIndex) {
                item.setAttribute('aria-selected', String(optionIndex === index));
                item.classList.toggle('is-selected', optionIndex === index);
            });
            trigger.disabled = select.disabled;
            trigger.setAttribute('aria-required', select.required ? 'true' : 'false');
            trigger.setAttribute('aria-invalid', select.matches(':invalid') ? 'true' : 'false');
            if (select.getAttribute('aria-describedby')) {
                trigger.setAttribute('aria-describedby', select.getAttribute('aria-describedby'));
            } else {
                trigger.removeAttribute('aria-describedby');
            }
        }

        function highlight(index) {
            optionButtons.forEach(function (item, optionIndex) {
                item.classList.toggle('is-highlighted', optionIndex === index);
            });
            if (index >= 0) {
                trigger.setAttribute('aria-activedescendant', optionButtons[index].id);
                optionButtons[index].scrollIntoView({ block: 'nearest' });
            } else {
                trigger.removeAttribute('aria-activedescendant');
            }
        }

        function openMenu() {
            if (select.disabled || optionButtons.length === 0) return;
            menu.hidden = false;
            wrapper.classList.add('is-open');
            trigger.setAttribute('aria-expanded', 'true');
            highlight(Math.max(0, select.selectedIndex));
            requestAnimationFrame(function () {
                var bounds = menu.getBoundingClientRect();
                menu.classList.toggle('sf-select__menu--above', bounds.bottom > window.innerHeight - 12);
            });
        }

        function closeMenu(returnFocus) {
            menu.hidden = true;
            wrapper.classList.remove('is-open');
            trigger.setAttribute('aria-expanded', 'false');
            trigger.removeAttribute('aria-activedescendant');
            optionButtons.forEach(function (item) {
                item.classList.remove('is-highlighted');
            });
            if (returnFocus) trigger.focus();
        }

        function highlightedIndex() {
            return optionButtons.findIndex(function (item) {
                return item.classList.contains('is-highlighted');
            });
        }

        function choose(index) {
            if (index < 0 || index >= options.length || options[index].disabled) return;
            var changed = select.selectedIndex !== index;
            select.selectedIndex = index;
            syncSelection();
            closeMenu(true);
            if (changed) select.dispatchEvent(new Event('change', { bubbles: true }));
        }

        trigger.addEventListener('click', function () {
            if (menu.hidden) {
                openMenu();
            } else {
                closeMenu(false);
            }
        });

        trigger.addEventListener('keydown', function (event) {
            var index = highlightedIndex();
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                if (menu.hidden) {
                    openMenu();
                    return;
                }
                var direction = event.key === 'ArrowDown' ? 1 : -1;
                var next = index < 0 ? Math.max(0, select.selectedIndex) : index;
                for (var step = 0; step < optionButtons.length; step += 1) {
                    next = (next + direction + optionButtons.length) % optionButtons.length;
                    if (!optionButtons[next].disabled) break;
                }
                highlight(next);
                return;
            }

            if (event.key === 'Home' || event.key === 'End') {
                event.preventDefault();
                if (menu.hidden) openMenu();
                var target = event.key === 'Home' ? 0 : optionButtons.length - 1;
                highlight(target);
                return;
            }

            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                if (menu.hidden) {
                    openMenu();
                } else {
                    choose(index);
                }
                return;
            }

            if (event.key === 'Escape' && !menu.hidden) {
                event.preventDefault();
                closeMenu(true);
            }
        });

        trigger.addEventListener('keydown', function (event) {
            if (event.key === 'Tab' && !menu.hidden) closeMenu(false);
        });

        document.addEventListener('pointerdown', function (event) {
            if (!wrapper.contains(event.target)) closeMenu(false);
        });
        document.addEventListener('focusin', function (event) {
            if (!wrapper.contains(event.target)) closeMenu(false);
        });
        window.addEventListener('resize', function () {
            if (!menu.hidden) closeMenu(false);
        });

        select.addEventListener('change', syncSelection);
        new MutationObserver(syncSelection).observe(select, {
            attributes: true,
            attributeFilter: ['disabled', 'required', 'aria-describedby', 'class']
        });
        select.form?.addEventListener('reset', function () {
            window.setTimeout(syncSelection, 0);
        });

        syncSelection();
    });
}

function initPasswordToggles() {
    document.querySelectorAll('.password-toggle').forEach(function (btn) {
        if (btn.dataset.passwordToggleInit) {
            return;
        }
        btn.dataset.passwordToggleInit = 'true';

        var input = document.getElementById(btn.getAttribute('data-password-target'));
        if (!input) {
            return;
        }

        var showLabel = btn.getAttribute('data-show-label') || 'Show password';
        var hideLabel = btn.getAttribute('data-hide-label') || 'Hide password';

        function renderToggle() {
            var visible = input.type === 'text';
            var icon = btn.querySelector('i');
            if (icon) {
                icon.className = 'bi ' + (visible ? 'bi-eye-slash' : 'bi-eye');
            }
            btn.setAttribute('aria-pressed', visible ? 'true' : 'false');
            btn.setAttribute('aria-label', visible ? hideLabel : showLabel);
        }

        btn.addEventListener('click', function () {
            input.type = input.type === 'password' ? 'text' : 'password';
            input.focus();
            renderToggle();
        });

        renderToggle();
    });
}

function initSendCodeButton(btn) {
    var targetSelector = btn.getAttribute('data-send-code-target') || btn.getAttribute('data-send-code-email');
    var targetInput = document.querySelector(targetSelector);
    var statusEl = document.querySelector(btn.getAttribute('data-send-code-status'));
    if (!targetInput) return;

    var url = btn.getAttribute('data-send-code-url');
    var purpose = btn.getAttribute('data-send-code-purpose') || 'register';
    var channel = btn.getAttribute('data-send-code-channel') || 'email';
    var successMsg = btn.getAttribute('data-send-code-success') || '';
    var failedMsg = btn.getAttribute('data-send-code-failed') || '';
    var invalidMsg = btn.getAttribute('data-send-code-invalid') || '';
    var label = btn.getAttribute('data-send-code-label') || btn.textContent;
    var demoMsg = btn.getAttribute('data-send-code-demo') || '';
    var cooldown = 60;

    function setStatus(msg, tone) {
        if (!statusEl) return;
        statusEl.classList.add('ui-inline-feedback');
        statusEl.dataset.tone = tone || 'info';
        statusEl.setAttribute('role', tone === 'error' ? 'alert' : 'status');
        statusEl.setAttribute('aria-atomic', 'true');
        statusEl.textContent = msg;
    }

    btn.addEventListener('click', function () {
        var target = targetInput.value.trim();
        if (!target) {
            setStatus(invalidMsg, 'error');
            return;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        btn.disabled = true;

        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token ? token.value : ''
            },
            body: JSON.stringify({ target: target, channel: channel, purpose: purpose })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.ok) {
                if (data.code) {
                    setStatus(demoMsg.replace('{0}', data.code));
                } else {
                    setStatus(successMsg, 'success');
                }
                startCountdown();
            } else {
                setStatus(data.error || failedMsg, 'error');
                btn.disabled = false;
            }
        })
        .catch(function () {
            setStatus(failedMsg, 'error');
            btn.disabled = false;
        });
    });

    function startCountdown() {
        var left = cooldown;
        btn.disabled = true;
        btn.textContent = left + 's';
        var timer = setInterval(function () {
            left--;
            if (left <= 0) {
                clearInterval(timer);
                btn.disabled = false;
                btn.textContent = label;
            } else {
                btn.textContent = left + 's';
            }
        }, 1000);
    }
}

