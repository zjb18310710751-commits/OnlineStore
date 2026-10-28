(function () {
    'use strict';
    document.querySelectorAll('[data-carousel-editor]').forEach(function (form) {
        var fileInput = form.querySelector('[data-image-input]');
        var image = form.querySelector('[data-preview-image]');
        var placeholder = form.querySelector('[data-preview-placeholder]');
        var language = form.querySelector('[data-preview-language]');
        var heading = form.querySelector('[data-preview-heading]');
        var description = form.querySelector('[data-preview-description]');
        var objectUrl = null;

        function value(selector) {
            var input = form.querySelector(selector);
            return input ? input.value.trim() : '';
        }
        function updateCopy() {
            var english = language && language.value === 'en';
            var headline = (english ? value('[data-copy-heading-en]') : '') || value('[data-copy-heading]');
            var copy = (english ? value('[data-copy-description-en]') : '') || value('[data-copy-description]');
            heading.textContent = headline || heading.dataset.placeholder;
            description.textContent = copy;
            description.hidden = !copy;
        }
        function revokePreview() {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = null;
        }
        function updateImage() {
            revokePreview();
            var file = fileInput.files && fileInput.files[0];
            var supportedImage = file && /\.(jpe?g|png|webp|gif)$/i.test(file.name) && (!file.type || /^image\//i.test(file.type));
            var source = image.dataset.originalSrc || '';
            if (supportedImage && file.size <= 5 * 1024 * 1024) {
                objectUrl = URL.createObjectURL(file);
                source = objectUrl;
            }
            if (source) image.src = source;
            else image.removeAttribute('src');
            image.hidden = !source;
            placeholder.hidden = !!source;
        }
        form.querySelectorAll('[data-copy-heading], [data-copy-heading-en], [data-copy-description], [data-copy-description-en]').forEach(function (input) {
            input.addEventListener('input', updateCopy);
        });
        if (language) language.addEventListener('change', updateCopy);
        if (fileInput && image) fileInput.addEventListener('change', updateImage);
        window.addEventListener('pagehide', revokePreview);
        updateCopy();
    });
})();
