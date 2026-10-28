(() => {
    const section = document.getElementById('product-reviews');
    if (!section) return;

    const compose = section.querySelector('#write-review');
    const openCompose = () => {
        if (!compose) return;
        compose.open = true;
    };
    section.querySelectorAll('a[href="#write-review"]').forEach(link => {
        link.addEventListener('click', () => {
            openCompose();
            compose?.querySelector('summary')?.focus({ preventScroll: true });
        });
    });
    if (location.hash === '#write-review') openCompose();
    window.addEventListener('hashchange', () => {
        if (location.hash === '#write-review') openCompose();
    });

    const comment = section.querySelector('[data-review-comment]');
    const characterCount = section.querySelector('[data-review-character-count]');
    const updateCharacterCount = () => {
        if (comment && characterCount) characterCount.textContent = `${comment.value.length} / ${comment.maxLength}`;
    };
    comment?.addEventListener('input', updateCharacterCount);
    updateCharacterCount();

    const fileInput = section.querySelector('[data-review-image]');
    const preview = section.querySelector('[data-review-image-preview]');
    const fileError = section.querySelector('[data-review-image-error]');
    const fileName = section.querySelector('[data-review-file-name]');
    let previewUrl = null;
    const renderFileName = () => {
        if (!fileInput || !fileName) return;
        const file = fileInput.files[0];
        fileName.textContent = file ? file.name : fileInput.dataset.noFile;
    };
    const clearPreview = () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        previewUrl = null;
        if (preview) {
            preview.hidden = true;
            preview.querySelector('img').removeAttribute('src');
            preview.querySelector('img').hidden = false;
        }
    };
    fileInput?.addEventListener('change', () => {
        clearPreview();
        fileInput.setCustomValidity('');
        fileInput.removeAttribute('aria-invalid');
        fileError.hidden = true;
        fileError.textContent = '';
        const file = fileInput.files[0];
        renderFileName();
        if (!file) return;
        const validExtension = /\.(jpe?g|png|webp|gif)$/i.test(file.name);
        const validType = !file.type || ['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type);
        const error = !validExtension || !validType
            ? fileInput.dataset.typeError
            : file.size > 5 * 1024 * 1024 ? fileInput.dataset.sizeError : '';
        if (error) {
            fileError.textContent = error;
            fileError.hidden = false;
            fileInput.setCustomValidity(error);
            fileInput.setAttribute('aria-invalid', 'true');
            preview.querySelector('img').hidden = true;
            preview.hidden = false;
            return;
        }
        previewUrl = URL.createObjectURL(file);
        preview.querySelector('img').src = previewUrl;
        preview.hidden = false;
    });
    section.querySelector('[data-review-image-remove]')?.addEventListener('click', () => {
        fileInput.value = '';
        fileInput.setCustomValidity('');
        fileInput.removeAttribute('aria-invalid');
        fileError.hidden = true;
        fileError.textContent = '';
        clearPreview();
        renderFileName();
        fileInput.focus();
    });
    window.addEventListener('pagehide', clearPreview);

    const archiveTrigger = section.querySelector('[data-review-archive-open]');
    const archiveDialog = section.querySelector('[data-review-archive-dialog]');
    const archiveBody = archiveDialog?.querySelector('[data-review-archive-body]');
    if (archiveTrigger && archiveDialog && archiveBody && typeof archiveDialog.showModal === 'function') {
        const archivePath = new URL(archiveTrigger.href, location.href).pathname;
        let archiveRequest = null;

        const loadArchive = async (url, focusHeading = false) => {
            archiveRequest?.abort();
            archiveRequest = new AbortController();
            archiveBody.setAttribute('aria-busy', 'true');
            archiveBody.classList.add('is-loading');
            try {
                const response = await fetch(url, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    signal: archiveRequest.signal
                });
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                archiveBody.innerHTML = await response.text();
                archiveBody.classList.remove('is-loading');
                archiveBody.removeAttribute('aria-busy');
                if (focusHeading) archiveBody.querySelector('h1')?.focus({ preventScroll: true });
            } catch (error) {
                if (error.name === 'AbortError') return;
                archiveBody.classList.remove('is-loading');
                archiveBody.removeAttribute('aria-busy');
                archiveBody.innerHTML = `<div class="sf-review-archive-error" role="alert">${archiveBody.dataset.loadError}</div>`;
            }
        };

        archiveTrigger.addEventListener('click', event => {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            archiveDialog.showModal();
            loadArchive(archiveTrigger.href);
        });

        archiveDialog.querySelector('[data-review-archive-close]')?.addEventListener('click', () => archiveDialog.close());
        archiveDialog.addEventListener('click', event => {
            const link = event.target.closest('a');
            if (link && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey
                && link.target !== '_blank' && new URL(link.href, location.href).pathname === archivePath) {
                event.preventDefault();
                loadArchive(link.href, true);
                return;
            }
            if (event.target === archiveDialog) archiveDialog.close();
        });

        archiveDialog.addEventListener('submit', event => {
            const form = event.target;
            if (!(form instanceof HTMLFormElement) || !form.matches('[data-review-archive-sort]')) return;
            event.preventDefault();
            const url = new URL(form.action, location.href);
            url.search = new URLSearchParams(new FormData(form)).toString();
            loadArchive(url, true);
        });
        archiveDialog.addEventListener('change', event => {
            if (event.target.matches('[data-review-archive-sort-select]')) event.target.form?.requestSubmit();
        });
        archiveDialog.addEventListener('close', () => {
            archiveRequest?.abort();
            archiveTrigger.focus({ preventScroll: true });
        });
    }
})();
