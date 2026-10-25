(function () {
    'use strict';

    function initOverview() {
        var root = document.querySelector('[data-admin-overview]');
        var source = document.getElementById('overview-revenue-data');
        if (!root || !source) return;
        var config;
        try { config = JSON.parse(source.textContent); } catch (_) { return; }
        var points = Array.isArray(config.points) ? config.points : [];
        var chartBox = root.querySelector('[data-revenue-chart]');
        var fallback = root.querySelector('[data-revenue-fallback]');
        var fallbackValues = root.querySelector('[data-revenue-values]');
        var accessibleValues = root.querySelector('[data-revenue-accessible-values]');
        var details = root.querySelector('[data-revenue-data-details]');
        var empty = root.querySelector('[data-revenue-empty]');
        var periods = root.querySelector('[data-revenue-periods]');
        var buttons = Array.prototype.slice.call(periods.querySelectorAll('[data-revenue-days]'));
        var total = root.querySelector('[data-revenue-total]');
        var periodLabel = root.querySelector('[data-revenue-period]');
        var numberFormat = new Intl.NumberFormat(config.locale || 'en', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        var axisFormat = new Intl.NumberFormat(config.locale || 'en', { maximumFractionDigits: 2, notation: 'compact' });
        var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var chart = null;

        function money(value) { return '¥' + numberFormat.format(value); }
        function writeDailyValues(target, selected) {
            target.replaceChildren();
            selected.forEach(function (point) {
                var item = document.createElement('div');
                var date = document.createElement('dt');
                var value = document.createElement('dd');
                date.textContent = point.label;
                value.textContent = money(Number(point.revenue));
                item.append(date, value);
                target.appendChild(item);
            });
        }
        function showPeriod(days) {
            var selected = points.slice(-days);
            var sum = selected.reduce(function (result, point) { return result + Number(point.revenue); }, 0);
            total.textContent = money(sum);
            periodLabel.textContent = config.periods[String(days)];
            buttons.forEach(function (button) { button.setAttribute('aria-pressed', String(Number(button.dataset.revenueDays) === days)); });
            empty.hidden = sum !== 0;
            writeDailyValues(accessibleValues, selected);
            if (chart) {
                chart.data.labels = selected.map(function (point) { return point.label; });
                chart.data.datasets[0].data = selected.map(function (point) { return Number(point.revenue); });
                chart.update(reducedMotion ? 'none' : undefined);
            } else {
                writeDailyValues(fallbackValues, selected);
            }
        }
        if (typeof window.Chart === 'function') {
            try {
                chartBox.hidden = false;
                chart = new window.Chart(document.getElementById('overviewRevenueChart'), {
                    type: 'line',
                    data: { labels: [], datasets: [{ data: [], borderColor: '#202020', backgroundColor: 'rgba(21, 21, 21, .035)', fill: true, tension: .25, pointRadius: 0, pointHoverRadius: 4, pointHoverBackgroundColor: '#202020', borderWidth: 2 }] },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        animation: reducedMotion ? false : { duration: 300 },
                        interaction: { mode: 'index', intersect: false },
                        plugins: {
                            legend: { display: false },
                            tooltip: { displayColors: false, padding: 10, callbacks: { label: function (context) { return money(Number(context.parsed.y)); } } }
                        },
                        scales: {
                            x: { border: { display: false }, grid: { display: false }, ticks: { color: '#777777', maxTicksLimit: 6, maxRotation: 0, font: { size: 10 }, padding: 8 } },
                            y: { beginAtZero: true, border: { display: false }, grid: { color: '#eeeeee', drawTicks: false }, ticks: { color: '#777777', maxTicksLimit: 5, padding: 10, font: { size: 10 }, callback: function (value) { return '¥' + axisFormat.format(value); } } }
                        }
                    }
                });
                fallback.hidden = true;
                details.hidden = false;
            } catch (_) {
                if (chart) chart.destroy();
                chart = null;
                chartBox.hidden = true;
                fallback.hidden = false;
            }
        }
        periods.hidden = false;
        buttons.forEach(function (button) { button.addEventListener('click', function () { showPeriod(Number(button.dataset.revenueDays)); }); });
        showPeriod(30);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initOverview);
    else initOverview();
})();
