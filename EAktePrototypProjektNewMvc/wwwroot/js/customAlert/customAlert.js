//Custom Alert / Confirm.
const _alertCfg = {
    success: { icon: 'fa-circle-check', label: 'Erfolgreich', css: 'success' },
    error: { icon: 'fa-circle-xmark', label: 'Fehler', css: 'error' },
    warning: { icon: 'fa-triangle-exclamation', label: 'Hinweis', css: 'warning' },
    confirm: { icon: 'fa-circle-question', label: 'Bestätigen', css: 'confirm' }
};

let _toastTimer = null;

// Zeigt eine benutzerdefinierte Toast-Nachricht an (success / warning). Rückgabewert: Promise, der mit true aufgelöst wird, wenn die Nachricht ausgeblendet wurde.
function showToast(message, duration = 500, type = 'success') {
    return new Promise(resolve => {
        const toast = document.getElementById('customToast');
        document.getElementById('customToastMessage').textContent = message;

        // Typ setzen (Farbe) und passendes Icon einsetzen.
        toast.classList.remove('success', 'warning');
        toast.classList.add(type);
        const icon = toast.querySelector('.toast-icon');
        if (icon) {
            const iconName = type === 'warning' ? 'fa-triangle-exclamation' : 'fa-circle-check';
            icon.className = `fa-solid ${iconName} toast-icon`;
        }

        clearTimeout(_toastTimer);
        toast.classList.add('show');

        _toastTimer = setTimeout(() => {
            toast.classList.remove('show');
            resolve(true);
        }, duration);
    });
}

// Zeigt eine benutzerdefinierte Alert-Box an.
function showAlert(message, type = 'success') {
    // Erfolg und Hinweis werden als selbst verschwindender Toast angezeigt (ohne Bestätigung).
    if (type === 'success') return showToast(message, 500, 'success');
    if (type === 'warning') return showToast(message, 2500, 'warning');

    return new Promise(resolve => {
        const cfg = _alertCfg[type] ?? _alertCfg.error;
        document.getElementById('customAlertIcon').className = `alert-icon ${cfg.css}`;
        document.getElementById('customAlertIcon').innerHTML = `<i class="fa-solid ${cfg.icon}"></i>`;
        document.getElementById('customAlertTitle').textContent = cfg.label;
        document.getElementById('customAlertMessage').textContent = message;
        document.getElementById('customAlertStripe').className = cfg.css;
        const cancelBtn = document.getElementById('customAlertCancel');
        const okBtn = document.getElementById('customAlertOk');
        cancelBtn.style.display = 'none';
        okBtn.className = 'alert-btn primary';
        okBtn.textContent = 'OK';
        document.getElementById('customAlertOverlay').classList.add('show');
        const close = (val) => {
            document.getElementById('customAlertOverlay').classList.remove('show');
            okBtn.replaceWith(okBtn.cloneNode(true));
            resolve(val);
        };
        document.getElementById('customAlertOk').addEventListener('click', () => close(true), { once: true });
    });
}

// Zeigt eine benutzerdefinierte Confirm-Box an. Rückgabewert: Promise, der mit true aufgelöst wird, wenn "OK" geklickt wurde, sonst false.
function showConfirm(message, confirmLabel = 'Ja, löschen', cancelLabel = 'Abbrechen') {
    return new Promise(resolve => {
        const cfg = _alertCfg.confirm;

        document.getElementById('customAlertIcon').className = `alert-icon ${cfg.css}`;
        document.getElementById('customAlertIcon').innerHTML = `<i class="fa-solid ${cfg.icon}"></i>`;
        document.getElementById('customAlertTitle').textContent = cfg.label;
        document.getElementById('customAlertMessage').textContent = message;
        document.getElementById('customAlertStripe').className = 'confirm';

        const cancelBtn = document.getElementById('customAlertCancel');
        const okBtn = document.getElementById('customAlertOk');

        cancelBtn.style.display = 'inline-flex';
        cancelBtn.textContent = cancelLabel;
        okBtn.className = 'alert-btn danger';
        okBtn.textContent = confirmLabel;

        document.getElementById('customAlertOverlay').classList.add('show');

        const close = (val) => {
            document.getElementById('customAlertOverlay').classList.remove('show');
            resolve(val);
        };
        // cloneNode trick — sicheres Entfernen alter Listener.
        const newOk = okBtn.cloneNode(true);
        const newCancel = cancelBtn.cloneNode(true);
        okBtn.replaceWith(newOk);
        cancelBtn.replaceWith(newCancel);

        document.getElementById('customAlertOk').addEventListener('click', () => close(true), { once: true });
        document.getElementById('customAlertCancel').addEventListener('click', () => close(false), { once: true });
    });
}