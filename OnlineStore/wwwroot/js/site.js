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

// 详情页多图画廊：点击缩略图切换主图
function switchImage(btn, url) {
    var main = document.getElementById('mainImage');
    if (main) {
        main.src = url;
    }
    document.querySelectorAll('.gallery-thumb').forEach(function (t) {
        t.classList.remove('active');
    });
    btn.classList.add('active');
}

