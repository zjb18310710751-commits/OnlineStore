(function () {
    'use strict';

    function init() {
        document.querySelectorAll('[data-notice]').forEach(function (notice) {
            var closeButton = notice.querySelector('[data-notice-close]');
            var progress = notice.querySelector('[data-notice-progress]');
            var remaining = Number(notice.dataset.duration) || 0;
            var timer = null;
            var started = 0;
            var hovering = notice.matches(':hover');
            var focused = false;
            var dismissed = false;

            function dismiss() {
                if (dismissed) return;
                dismissed = true;
                window.clearTimeout(timer);
                document.removeEventListener('visibilitychange', syncTimer);
                var restoreFocus = notice.contains(document.activeElement);
                notice.remove();
                if (restoreFocus) {
                    var destination = document.querySelector('[data-notice-close]') || document.querySelector('main[tabindex="-1"]');
                    if (destination) destination.focus({ preventScroll: true });
                }
            }

            function syncTimer() {
                if (dismissed || !Number(notice.dataset.duration)) return;
                if (timer !== null) {
                    window.clearTimeout(timer);
                    remaining = Math.max(0, remaining - (performance.now() - started));
                    timer = null;
                }
                var paused = hovering || focused || document.hidden;
                notice.classList.toggle('is-paused', paused);
                if (!paused) {
                    started = performance.now();
                    timer = window.setTimeout(dismiss, remaining);
                }
            }

            if (closeButton) {
                closeButton.hidden = false;
                closeButton.addEventListener('click', dismiss);
            }
            if (remaining > 0) {
                notice.style.setProperty('--notice-duration', remaining + 'ms');
                notice.addEventListener('pointerenter', function () { hovering = true; syncTimer(); });
                notice.addEventListener('pointerleave', function () { hovering = false; syncTimer(); });
                notice.addEventListener('focusin', function () { focused = true; syncTimer(); });
                notice.addEventListener('focusout', function (event) {
                    if (notice.contains(event.relatedTarget)) return;
                    focused = false;
                    syncTimer();
                });
                document.addEventListener('visibilitychange', syncTimer);
                if (progress) progress.hidden = false;
                syncTimer();
            }
        });

        document.querySelectorAll('[data-notification-stack]').forEach(function (stack) {
            stack.classList.add('is-enhanced');
        });

        document.querySelectorAll('[data-valmsg-summary="true"]').forEach(function (summary) {
            summary.setAttribute('role', 'alert');
            summary.setAttribute('aria-atomic', 'true');
        });
        document.querySelectorAll('[data-valmsg-for]').forEach(function (message) {
            message.setAttribute('aria-live', 'polite');
            message.setAttribute('aria-atomic', 'true');
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
