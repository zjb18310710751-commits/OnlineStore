(function () {
    'use strict';

    function initConfirmations() {
        var dialog = document.getElementById('ui-confirm-dialog');
        var requestSubmit = window.HTMLFormElement && window.HTMLFormElement.prototype.requestSubmit;
        if (!dialog || typeof dialog.showModal !== 'function' || typeof requestSubmit !== 'function') return;
        if (dialog.dataset.confirmReady === 'true') return;

        var message = dialog.querySelector('[data-confirm-message]');
        var cancelButton = dialog.querySelector('[data-confirm-cancel]');
        var closeButton = dialog.querySelector('[data-confirm-close]');
        var acceptButton = dialog.querySelector('[data-confirm-accept]');
        if (!message || !cancelButton || !closeButton || !acceptButton) return;

        var pending = null;
        var returnFocus = null;
        var confirmed = new WeakSet();
        var submitting = new WeakSet();

        function restoreFocus() {
            if (returnFocus && returnFocus.isConnected && typeof returnFocus.focus === 'function') {
                returnFocus.focus({ preventScroll: true });
            }
            returnFocus = null;
        }

        function dismiss() {
            pending = null;
            acceptButton.disabled = false;
            if (dialog.open) dialog.close('cancel');
            else restoreFocus();
        }

        function submitConfirmed(state) {
            if (!state || !state.form.isConnected || submitting.has(state.form)) return;
            confirmed.add(state.form);
            try {
                if (state.submitter && state.submitter.isConnected && state.submitter.form === state.form) {
                    requestSubmit.call(state.form, state.submitter);
                } else {
                    requestSubmit.call(state.form);
                }
            } finally {
                confirmed.delete(state.form);
            }
        }

        function interceptSubmit(event) {
            var form = event.currentTarget;
            if (confirmed.has(form)) {
                confirmed.delete(form);
                submitting.add(form);
                Promise.resolve().then(function () {
                    if (event.defaultPrevented) submitting.delete(form);
                });
                return;
            }
            event.preventDefault();
            if (submitting.has(form) || pending || dialog.open) return;

            var state = { form: form, submitter: event.submitter || null };
            pending = state;
            returnFocus = state.submitter || document.activeElement;
            message.textContent = form.dataset.confirm;
            acceptButton.disabled = false;
            try {
                dialog.showModal();
                cancelButton.focus({ preventScroll: true });
            } catch (_) {
                pending = null;
                if (window.confirm(form.dataset.confirm)) submitConfirmed(state);
                restoreFocus();
            }
        }

        cancelButton.addEventListener('click', dismiss);
        closeButton.addEventListener('click', dismiss);
        dialog.addEventListener('cancel', function (event) {
            event.preventDefault();
            dismiss();
        });
        dialog.addEventListener('close', restoreFocus);
        acceptButton.addEventListener('click', function () {
            if (!pending || acceptButton.disabled) return;
            var state = pending;
            pending = null;
            acceptButton.disabled = true;
            dialog.close('confirm');
            submitConfirmed(state);
        });

        document.querySelectorAll('form[data-confirm]').forEach(function (form) {
            form.addEventListener('submit', interceptSubmit);
            form.removeAttribute('onsubmit');
            form.dataset.confirmReady = 'true';
        });
        dialog.dataset.confirmReady = 'true';
        window.addEventListener('pageshow', function (event) {
            if (event.persisted) {
                confirmed = new WeakSet();
                submitting = new WeakSet();
                dismiss();
            }
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initConfirmations);
    else initConfirmations();
})();
