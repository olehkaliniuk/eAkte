// Formatiert die Dateigröße in eine lesbare Form.
function formatDokBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1048576).toFixed(2) + ' MB';
}

// Zeigt die ausgewählte Datei im Upload-Bereich an.
function showDokFile(file) {
    document.getElementById('dokEmptyState').style.display = 'none';
    document.getElementById('dokFilePreview').style.display = 'flex';
    document.getElementById('dokDropZone').classList.add('has-file');
    document.getElementById('dokFileName').textContent = file.name;
    document.getElementById('dokFileSize').textContent = formatDokBytes(file.size);
    const badge = document.getElementById('dok-badge-tab1');
    if (badge) badge.style.display = 'inline-flex';

    showFileSuccess('Datei ausgewählt.');
}

// Setzt den Datei-Upload zurück und entfernt die Vorschau.
function clearDokFile() {
    document.getElementById('dokFileInput').value = '';
    document.getElementById('dokEmptyState').style.display = '';
    document.getElementById('dokFilePreview').style.display = 'none';
    document.getElementById('dokDropZone').classList.remove('has-file');
    const badge = document.getElementById('dok-badge-tab1');
    if (badge) badge.style.display = 'none';

    clearFileStatus();
}

// Verarbeitet die Auswahl einer Datei über das Dateiauswahlfeld.
function onDokFileSelected(input) {
    if (input.files && input.files[0]) showDokFile(input.files[0]);
}

// Initialisiert die Drag-and-Drop-Funktionalität für den Datei-Upload.
function initDokDropZone() {
    const dropZone = document.getElementById('dokDropZone');
    if (!dropZone) return;
    dropZone.addEventListener('dragover', e => {
        e.preventDefault();
        dropZone.classList.add('drag-over');
    });
    dropZone.addEventListener('dragleave', () => dropZone.classList.remove('drag-over'));
    dropZone.addEventListener('drop', e => {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
        const file = e.dataTransfer.files[0];
        if (!file) return;
        showDokFile(file);
        try {
            const dt = new DataTransfer();
            dt.items.add(file);
            document.getElementById('dokFileInput').files = dt.files;
        } catch (_) { }
    });
}

// Zeigt Fehlerzustand für den Datei-Upload an.
function showFileError(message) {
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');

    errorBox.classList.remove('text-success-modal');
    errorBox.classList.add('text-error-modal', 'visible');

    errorIcon.classList.remove('fa-circle-check', 'icon-success');
    errorIcon.classList.add('fa-circle-exclamation', 'icon-error');

    errorText.textContent = message;
}

// Zeigt positive Rückmeldung, wenn eine Datei ausgewählt wurde.
function showFileSuccess(message) {
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');

    errorBox.classList.remove('text-error-modal');
    errorBox.classList.add('text-success-modal', 'visible');

    errorIcon.classList.remove('fa-circle-exclamation', 'icon-error');
    errorIcon.classList.add('fa-circle-check', 'icon-success');

    errorText.textContent = message;
}

// Versteckt jeglichen Status.
function clearFileStatus() {
    const errorBox = document.getElementById('nameErrorBox');
    errorBox.classList.remove('visible', 'text-error-modal', 'text-success-modal');
}