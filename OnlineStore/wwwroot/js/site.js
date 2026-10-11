// 站点脚本：自动隐藏可关闭的提示信息
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert-dismissible').forEach(function (el) {
        setTimeout(function () {
            var alert = bootstrap.Alert.getOrCreateInstance(el);
            alert.close();
        }, 5000);
    });
});

// 商品详情页数量步进器：点击 +/- 调整数量，并受输入框 min/max 约束
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

// 平滑切换主图：预加载目标图，再淡出→换图→淡入，避免白屏闪烁
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

// 详情页多图画廊：点击缩略图切换主图
function switchImage(btn, url) {
    setMainImage(url);
    document.querySelectorAll('.gallery-thumb').forEach(function (t) {
        t.classList.remove('active');
    });
    btn.classList.add('active');
}

// 发送验证码按钮：通用组件（注册/找回密码共用），通过 data-* 属性配置
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-send-code-url]').forEach(initSendCodeButton);
});

function initSendCodeButton(btn) {
    var emailInput = document.querySelector(btn.getAttribute('data-send-code-email'));
    var statusEl = document.querySelector(btn.getAttribute('data-send-code-status'));
    if (!emailInput) return;

    var url = btn.getAttribute('data-send-code-url');
    var purpose = btn.getAttribute('data-send-code-purpose') || 'register';
    var successMsg = btn.getAttribute('data-send-code-success') || '';
    var failedMsg = btn.getAttribute('data-send-code-failed') || '';
    var invalidMsg = btn.getAttribute('data-send-code-invalid') || '';
    var label = btn.getAttribute('data-send-code-label') || btn.textContent;
    var cooldown = 60;

    function setStatus(msg) {
        if (statusEl) statusEl.textContent = msg;
    }

    btn.addEventListener('click', function () {
        var email = emailInput.value.trim();
        if (!email) {
            setStatus(invalidMsg);
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
            body: JSON.stringify({ email: email, purpose: purpose })
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.ok) {
                setStatus(successMsg);
                startCountdown();
            } else {
                setStatus(data.error || failedMsg);
                btn.disabled = false;
            }
        })
        .catch(function () {
            setStatus(failedMsg);
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

