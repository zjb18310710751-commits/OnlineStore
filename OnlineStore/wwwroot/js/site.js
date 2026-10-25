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
});

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

