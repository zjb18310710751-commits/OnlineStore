(function () {
    'use strict';

    function initNavigation(root) {
        if (root.dataset.navigationReady === 'true') return;
        var records = Array.prototype.slice.call(root.querySelectorAll('[data-category-item]')).map(function (item) {
            var trigger = item.querySelector('[data-category-trigger]');
            var panelId = trigger ? trigger.getAttribute('aria-controls') : null;
            return {
                item: item,
                trigger: trigger,
                fallback: item.querySelector('[data-category-fallback]'),
                panel: panelId ? document.getElementById(panelId) : null,
                hoveredNodes: new Set(),
                pointerAction: null
            };
        }).filter(function (record) { return record.trigger && record.panel && root.contains(record.panel); });
        if (!records.length) return;

        var desktop = window.matchMedia('(min-width: 992px)');
        var hoverPointer = window.matchMedia('(hover: hover) and (pointer: fine)');
        var opened = null;
        var closeTimer = null;
        var suppressFocusOpen = false;

        function contains(record, node) {
            return !!node && (record.item.contains(node) || record.panel.contains(node));
        }

        function updatePanelSpace() {
            if (!opened) return;
            var nav = root.closest('.sf-nav');
            if (nav) root.style.setProperty('--sf-category-panel-space', Math.max(100, window.innerHeight - nav.getBoundingClientRect().bottom - 12) + 'px');
        }

        function clearCloseTimer() {
            if (closeTimer !== null) {
                window.clearTimeout(closeTimer);
                closeTimer = null;
            }
        }

        function setOpen(record, open) {
            record.trigger.setAttribute('aria-expanded', String(open));
            record.item.classList.toggle('is-open', open);
            record.panel.hidden = !open;
            record.panel.toggleAttribute('inert', !open);
            if (!open) record.hoveredNodes.clear();
        }

        function close(returnFocus) {
            clearCloseTimer();
            if (!opened) return;
            var previous = opened;
            opened = null;
            setOpen(previous, false);
            if (returnFocus) {
                suppressFocusOpen = true;
                previous.trigger.focus({ preventScroll: true });
                suppressFocusOpen = false;
            }
        }

        function open(record) {
            clearCloseTimer();
            if (opened === record) return;
            if (opened) setOpen(opened, false);
            opened = record;
            updatePanelSpace();
            setOpen(record, true);
        }

        function scheduleClose(record) {
            clearCloseTimer();
            closeTimer = window.setTimeout(function () {
                closeTimer = null;
                if (opened === record && record.hoveredNodes.size === 0 && !contains(record, document.activeElement)) close(false);
            }, 160);
        }

        records.forEach(function (record) {
            setOpen(record, false);
            record.trigger.addEventListener('pointerdown', function (event) {
                if (event.button === 0) {
                    record.pointerAction = event.pointerType === 'mouse' && hoverPointer.matches ? true : opened !== record;
                }
            });
            record.trigger.addEventListener('pointercancel', function () { record.pointerAction = null; });
            record.trigger.addEventListener('click', function (event) {
                var shouldOpen = event.detail > 0 && record.pointerAction !== null ? record.pointerAction : opened !== record;
                record.pointerAction = null;
                if (shouldOpen) open(record); else close(false);
            });
            [record.item, record.panel].forEach(function (surface) {
                surface.addEventListener('pointerenter', function (event) {
                    if (!hoverPointer.matches || event.pointerType !== 'mouse') return;
                    record.hoveredNodes.add(surface);
                    clearCloseTimer();
                    if (opened && opened !== record && opened.panel.contains(document.activeElement)) return;
                    open(record);
                });
                surface.addEventListener('pointerleave', function (event) {
                    if (!hoverPointer.matches || event.pointerType !== 'mouse') return;
                    record.hoveredNodes.delete(surface);
                    if (contains(record, event.relatedTarget)) return;
                    scheduleClose(record);
                });
                surface.addEventListener('focusin', function () {
                    clearCloseTimer();
                    if (!suppressFocusOpen) open(record);
                });
                surface.addEventListener('focusout', function (event) {
                    if (contains(record, event.relatedTarget)) return;
                    scheduleClose(record);
                });
            });
            record.trigger.addEventListener('keydown', function (event) {
                if (event.key === 'Tab' && !event.shiftKey && opened === record) {
                    var firstLink = record.panel.querySelector('a[href]');
                    if (firstLink) {
                        event.preventDefault();
                        firstLink.focus();
                    }
                    return;
                }
                if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
                event.preventDefault();
                open(record);
                var links = record.panel.querySelectorAll('a[href]');
                var target = event.key === 'ArrowDown' ? links[0] : links[links.length - 1];
                if (target) target.focus();
            });
            record.panel.addEventListener('keydown', function (event) {
                if (event.key === 'Tab' && event.shiftKey && event.target === record.panel.querySelector('a[href]')) {
                    event.preventDefault();
                    record.trigger.focus();
                }
                if (event.key === 'Tab' && !event.shiftKey) {
                    var links = record.panel.querySelectorAll('a[href]');
                    if (event.target === links[links.length - 1]) {
                        var nextRecord = records[records.indexOf(record) + 1];
                        var nav = root.closest('.sf-nav');
                        var destination = nextRecord ? nextRecord.trigger : nav && nav.querySelector('.sf-nav-tools button, .sf-nav-tools input, .sf-nav-tools a[href]');
                        if (destination) {
                            event.preventDefault();
                            close(false);
                            destination.focus();
                        }
                    }
                }
            });
            if (record.fallback) record.fallback.hidden = true;
            record.trigger.hidden = false;
        });

        root.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && opened) {
                event.preventDefault();
                event.stopPropagation();
                close(true);
            }
        });
        document.addEventListener('pointerdown', function (event) {
            if (opened && !root.contains(event.target)) close(false);
        });

        function handleViewportChange() {
            var focusedPanel = opened && opened.panel.contains(document.activeElement);
            var previousTrigger = opened ? opened.trigger : null;
            records.forEach(function (record) { record.hoveredNodes.clear(); record.pointerAction = null; });
            close(false);
            if (focusedPanel && previousTrigger) {
                suppressFocusOpen = true;
                previousTrigger.focus({ preventScroll: true });
                suppressFocusOpen = false;
            }
        }
        if (typeof desktop.addEventListener === 'function') desktop.addEventListener('change', handleViewportChange);
        else if (typeof desktop.addListener === 'function') desktop.addListener(handleViewportChange);
        window.addEventListener('resize', updatePanelSpace, { passive: true });
        window.addEventListener('scroll', updatePanelSpace, { passive: true });

        root.dataset.navigationReady = 'true';
    }

    function init() { document.querySelectorAll('[data-category-navigation]').forEach(initNavigation); }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
