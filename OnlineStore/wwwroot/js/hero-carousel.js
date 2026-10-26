(function () {
    'use strict';

    function initCarousel(root) {
        if (root.dataset.carouselReady === 'true') return;

        var slides = Array.prototype.slice.call(root.querySelectorAll('[data-hero-slide]'));
        var controls = root.querySelector('[data-hero-controls]');
        if (slides.length < 2 || !controls) return;

        var dots = Array.prototype.slice.call(root.querySelectorAll('[data-hero-dot]'));
        var status = root.querySelector('[data-hero-status]');
        var motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
        var hoverCapability = window.matchMedia('(hover: hover) and (pointer: fine)');
        var configuredInterval = Number(root.dataset.interval);
        var interval = Number.isFinite(configuredInterval) && configuredInterval > 0 ? configuredInterval : 3000;
        var activeIndex = Math.max(0, slides.findIndex(function (slide) { return slide.classList.contains('is-active'); }));
        var motionPaused = motionPreference.matches;
        var hovered = hoverCapability.matches && root.matches(':hover');
        var focused = false;
        var timer = null;

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
            if (motionPaused || hovered || focused || document.hidden) return;
            timer = window.setTimeout(function () {
                timer = null;
                showSlide(activeIndex + 1, false);
            }, interval);
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
            if (manuallyRequested && status) status.textContent = slides[activeIndex].dataset.slideLabel || '';
            schedule();
        }

        dots.forEach(function (dot) {
            dot.addEventListener('click', function () {
                var index = Number(dot.dataset.slideIndex);
                if (Number.isInteger(index) && index >= 0 && index < slides.length) showSlide(index, true);
            });
        });

        root.addEventListener('mouseenter', function () {
            if (!hoverCapability.matches) return;
            hovered = true;
            cancelTimer();
        });
        root.addEventListener('mouseleave', function () {
            hovered = false;
            schedule();
        });
        root.addEventListener('focusin', function () {
            focused = true;
            cancelTimer();
        });
        root.addEventListener('focusout', function (event) {
            if (!root.contains(event.relatedTarget)) {
                focused = false;
                schedule();
            }
        });
        document.addEventListener('visibilitychange', schedule);

        function handleMotionChange(event) {
            motionPaused = event.matches;
            schedule();
        }
        if (typeof motionPreference.addEventListener === 'function') {
            motionPreference.addEventListener('change', handleMotionChange);
        } else if (typeof motionPreference.addListener === 'function') {
            motionPreference.addListener(handleMotionChange);
        }

        root.dataset.carouselReady = 'true';
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
