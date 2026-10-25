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

    const list = section.querySelector('[data-review-list]');
    const tools = section.querySelector('[data-review-tools]');
    const sort = section.querySelector('[data-review-sort]');
    const status = section.querySelector('[data-review-status]');
    const filters = [...section.querySelectorAll('[data-review-filter]')];
    const rows = [...section.querySelectorAll('[data-review-row]')].map((element, index) => ({
        element,
        index,
        rating: Number(element.dataset.rating),
        hasPhoto: element.dataset.hasPhoto === 'true'
    }));
    let activeFilter = 'all';
    const updateReviews = () => {
        let visibleCount = 0;
        const sorted = [...rows].sort((a, b) => {
            if (sort?.value === 'highest') return b.rating - a.rating || a.index - b.index;
            if (sort?.value === 'lowest') return a.rating - b.rating || a.index - b.index;
            return a.index - b.index;
        });
        sorted.forEach(row => {
            row.element.hidden = activeFilter === 'photos' && !row.hasPhoto;
            if (!row.element.hidden) visibleCount += 1;
            list.append(row.element);
        });
        if (status) status.textContent = status.dataset.countTemplate.replace('{0}', String(visibleCount));
    };
    if (list && tools) {
        tools.hidden = false;
        filters.forEach(button => button.addEventListener('click', () => {
            activeFilter = button.dataset.reviewFilter;
            filters.forEach(filter => filter.setAttribute('aria-pressed', String(filter === button)));
            updateReviews();
        }));
        sort?.addEventListener('change', updateReviews);
    }

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
    let previewUrl = null;
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
        fileInput.focus();
    });
    window.addEventListener('pagehide', clearPreview);

    const dialog = section.querySelector('[data-review-dialog]');
    const dialogImage = section.querySelector('[data-review-dialog-image]');
    let photoTrigger = null;
    if (dialog && typeof dialog.showModal === 'function') {
        section.querySelectorAll('[data-review-photo]').forEach(link => {
            link.addEventListener('click', event => {
                if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
                event.preventDefault();
                photoTrigger = link;
                dialogImage.src = link.href;
                dialogImage.alt = link.querySelector('img').alt;
                dialog.showModal();
            });
        });
        section.querySelector('[data-review-dialog-close]')?.addEventListener('click', () => dialog.close());
        dialog.addEventListener('click', event => {
            if (event.target !== dialog) return;
            const bounds = dialog.getBoundingClientRect();
            if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) dialog.close();
        });
        dialog.addEventListener('close', () => {
            dialogImage.removeAttribute('src');
            photoTrigger?.focus({ preventScroll: true });
        });
    }
})();
