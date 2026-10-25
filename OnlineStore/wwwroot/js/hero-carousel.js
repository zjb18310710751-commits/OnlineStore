(function () {
    'use strict';

    function initCarousel(root) {
        if (root.dataset.carouselReady === 'true') return;

        var slides = Array.prototype.slice.call(root.querySelectorAll('[data-hero-slide]'));
        var controls = root.querySelector('[data-hero-controls]');
        if (slides.length < 2 || !controls) return;

        var previous = root.querySelector('[data-hero-prev]');
        var next = root.querySelector('[data-hero-next]');
        var dots = Array.prototype.slice.call(root.querySelectorAll('[data-hero-dot]'));
        var pauseButton = root.querySelector('[data-hero-pause]');
        var pauseIcon = root.querySelector('[data-hero-pause-icon]');
        var pauseText = root.querySelector('[data-hero-pause-text]');
        var counter = root.querySelector('[data-hero-current]');
        var status = root.querySelector('[data-hero-status]');
        var motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
        var hoverCapability = window.matchMedia('(hover: hover) and (pointer: fine)');
        var configuredInterval = Number(root.dataset.interval);
        var interval = Number.isFinite(configuredInterval) && configuredInterval > 0 ? configuredInterval : 5000;
        var activeIndex = Math.max(0, slides.findIndex(function (slide) { return slide.classList.contains('is-active'); }));
        var persistentlyPaused = motionPreference.matches;
        var hovered = hoverCapability.matches && root.matches(':hover');
        var timer = null;
        var pointerPauseAction = null;

        if (status) {
            status.setAttribute('aria-live', 'polite');
            status.setAttribute('aria-atomic', 'true');
        }

        function cancelTimer() {
            if (timer !== null) {
                window.clearTimeout(timer);
                timer = null;
            }
        }

        function schedule() {
            cancelTimer();
            if (persistentlyPaused || hovered || document.hidden) return;
            timer = window.setTimeout(function () {
                timer = null;
                showSlide(activeIndex + 1, false);
            }, interval);
        }

        function updatePauseControl() {
            var label = persistentlyPaused ? root.dataset.playLabel : root.dataset.pauseLabel;
            if (pauseButton) {
                if (label) pauseButton.setAttribute('aria-label', label);
            }
            if (pauseText && label) pauseText.textContent = label;
            if (pauseIcon) {
                pauseIcon.classList.toggle('bi-pause-fill', !persistentlyPaused);
                pauseIcon.classList.toggle('bi-play-fill', persistentlyPaused);
            }
        }

        function setPaused(paused) {
            persistentlyPaused = paused;
            updatePauseControl();
            schedule();
        }

        function showSlide(index, manuallyRequested) {
            activeIndex = (index + slides.length) % slides.length;
            slides.forEach(function (slide, slideIndex) {
                var active = slideIndex === activeIndex;
                slide.classList.toggle('is-active', active);
                slide.setAttribute('aria-hidden', String(!active));
                slide.toggleAttribute('inert', !active);
            });
            dots.forEach(function (dot) {
                var active = Number(dot.dataset.slideIndex) === activeIndex;
                dot.classList.toggle('is-active', active);
                if (active) {
                    dot.setAttribute('aria-current', 'true');
                } else {
                    dot.removeAttribute('aria-current');
                }
            });
            if (counter) counter.textContent = String(activeIndex + 1).padStart(2, '0');
            if (manuallyRequested && status) status.textContent = slides[activeIndex].dataset.slideLabel || '';
            schedule();
        }

        if (previous) previous.addEventListener('click', function () { showSlide(activeIndex - 1, true); });
        if (next) next.addEventListener('click', function () { showSlide(activeIndex + 1, true); });
        dots.forEach(function (dot) {
            dot.addEventListener('click', function () {
                var index = Number(dot.dataset.slideIndex);
                if (Number.isInteger(index) && index >= 0 && index < slides.length) showSlide(index, true);
            });
        });
        if (pauseButton) {
            pauseButton.addEventListener('pointerdown', function (event) {
                if (event.button === 0) pointerPauseAction = !persistentlyPaused;
            });
            pauseButton.addEventListener('pointercancel', function () { pointerPauseAction = null; });
            pauseButton.addEventListener('click', function (event) {
                var paused = event.detail > 0 && pointerPauseAction !== null ? pointerPauseAction : !persistentlyPaused;
                pointerPauseAction = null;
                setPaused(paused);
            });
        }

        root.addEventListener('mouseenter', function () {
            if (!hoverCapability.matches) return;
            hovered = true;
            cancelTimer();
        });
        root.addEventListener('mouseleave', function () {
            hovered = false;
            schedule();
        });
        root.addEventListener('focusin', function (event) {
            var focusIsInSlide = slides.some(function (slide) { return slide.contains(event.target); });
            if (!event.relatedTarget || !root.contains(event.relatedTarget) || focusIsInSlide) setPaused(true);
        });
        document.addEventListener('visibilitychange', schedule);

        function handleMotionChange(event) {
            if (event.matches) setPaused(true);
        }
        if (typeof motionPreference.addEventListener === 'function') {
            motionPreference.addEventListener('change', handleMotionChange);
        } else if (typeof motionPreference.addListener === 'function') {
            motionPreference.addListener(handleMotionChange);
        }

        root.dataset.carouselReady = 'true';
        updatePauseControl();
        showSlide(activeIndex, false);
        controls.hidden = false;
    }

    function init() {
        document.querySelectorAll('[data-hero-carousel]').forEach(initCarousel);
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
