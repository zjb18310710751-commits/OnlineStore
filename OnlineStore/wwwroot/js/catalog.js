(function () {
    'use strict';

    function initCatalog() {
        var catalog = document.querySelector('[data-catalog]');
        if (!catalog) return;

        var layout = catalog.querySelector('[data-catalog-layout]');
        var filters = catalog.querySelector('#catalogFilters');
        var toggle = catalog.querySelector('[data-filter-toggle]');
        var toggleLabel = catalog.querySelector('[data-filter-label]');

        if (layout && filters && toggle && toggleLabel) {
            function setFiltersVisible(visible) {
                filters.hidden = !visible;
                layout.classList.toggle('filters-hidden', !visible);
                toggle.setAttribute('aria-expanded', String(visible));
                toggleLabel.textContent = visible ? toggle.dataset.hideLabel : toggle.dataset.showLabel;
            }

            setFiltersVisible(!window.matchMedia('(max-width: 991.98px)').matches);
            toggle.hidden = false;
            toggle.addEventListener('click', function () {
                setFiltersVisible(filters.hidden);
            });
        }

        var sortForm = catalog.querySelector('[data-sort-form]');
        var sortSelect = catalog.querySelector('[data-sort-select]');
        var sortApply = catalog.querySelector('[data-sort-apply]');
        if (sortForm && sortSelect) {
            sortSelect.addEventListener('change', function () {
                if (typeof sortForm.requestSubmit === 'function') {
                    sortForm.requestSubmit();
                } else {
                    sortForm.submit();
                }
            });
            if (sortApply) sortApply.hidden = true;
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCatalog);
    } else {
        initCatalog();
    }
})();
