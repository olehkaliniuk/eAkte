// Speichert ein Dokument, entweder durch Erstellen oder Aktualisieren, basierend auf dem Modus (create/edit).
async function saveDokument() {
    const parentObjectId = document.getElementById('modalDokumentParentObjectId').value.trim();
    const mode = document.getElementById('modalDokumentMode')?.value.trim();
    const fileInput = document.getElementById('dokFileInput');
    const file = fileInput?.files?.[0];
    if (!file) {
        showFileError('Bitte wählen Sie eine Datei aus.');
        return;
    }

    const formData = new FormData();
    formData.append('parentObjectId', parentObjectId);
    formData.append('file', file);

    // Diese Felder gelten für Create UND Edit.
    const description = document.getElementById('dokDescription')?.value.trim() ?? '';
    const bemerkungen = document.getElementById('dokBemerkungen')?.value.trim() ?? '';
    formData.append('description', description);
    formData.append('bemerkungen', bemerkungen);

    // Nur beim Erstellen.
    if (mode !== 'edit') {
        const subject = document.getElementById('dokSubject')?.value.trim() ?? '';
        const gegenstand = document.getElementById('dokGegenstand')?.value.trim() ?? '';
        formData.append('subject', subject);
        formData.append('gegenstand', gegenstand);
    }

    const url = mode === 'edit'
        ? '/bescheidbearbeitung/update-dokument'
        : '/bescheidbearbeitung/create-dokument';
    try {
        const response = await fetch(url, { method: 'POST', body: formData });
        if (!response.ok) {
            await showAlert("Fehler beim Speichern.", "error");
            return;
        }
        const result = await response.json();
        if (result.success) {
            closeModalDokument();
            await showAlert(mode === 'edit' ? "Dokument wurde aktualisiert." : "Dokument wurde erstellt.", "success");
            const objectId = history.state?.objectId;
            if (objectId) window.location.href = `/bescheidbearbeitung?ObjectId=${objectId}`;
        } else {
            await showAlert(result.message ?? "Fehler beim Speichern.", "error");
        }
    } catch {
        await showAlert("Netzwerkfehler.", "error");
    }
}


// Drag Drop Table.
// Vermeiden, dass die Datei im Browser geöffnet wird, wenn sie auf die Seite gezogen wird.
window.addEventListener('dragover', e => e.preventDefault());
window.addEventListener('drop', e => e.preventDefault());


(function () {
    const UPLOAD_URL = '/bescheidbearbeitung/create-dokument';

    // Drag & Drop Events für die Drop-Zone.
    document.addEventListener('dragenter', e => {
        const zone = e.target.closest('#dokument-drop-zone');
        if (zone) zone.classList.add('drag-over');
    });
    document.addEventListener('dragover', e => {
        const zone = e.target.closest('#dokument-drop-zone');
        if (zone) zone.classList.add('drag-over');
    });
    document.addEventListener('dragleave', e => {
        const zone = e.target.closest('#dokument-drop-zone');
        if (zone) zone.classList.remove('drag-over');
    });
    document.addEventListener('drop', async e => {
        const zone = e.target.closest('#dokument-drop-zone');
        if (!zone) return;
        zone.classList.remove('drag-over');
        const files = e.dataTransfer.files;
        if (!files || files.length === 0) return;
        const container = document.getElementById('dokument-table-container');
        const parentObjectId = container?.dataset.parentId;
        for (const file of Array.from(files)) {
            await uploadFile(file, parentObjectId);
        }
    });

    // Überprüfen der Datei-Endung und des MIME-Typs, um sicherzustellen,
    // dass es sich um einen erlaubten Dateityp handelt
    // (.docx, .xlsx, .pptx, .txt, .jpeg/.jpg, .msg, .eml).
    const ALLOWED_EXT = [
        '.docx', '.xlsx', '.pptx',
        '.txt',
        '.jpeg', '.jpg',
        '.msg', '.eml',
        '.xml'
    ];
    const ALLOWED_MIME = [
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document', // .docx
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',       // .xlsx
        'application/vnd.openxmlformats-officedocument.presentationml.presentation', // .pptx
        'text/plain',                                                              // .txt
        'image/jpeg',                                                              // .jpeg / .jpg
        'application/vnd.ms-outlook',                                              // .msg
        'application/x-msg',                                                       // .msg (Alternative)
        'message/rfc822',                                                          // .eml
        'text/xml',                                                                // .xml
        'application/xml',                                                         // .xml (Alternative)
        ''
    ];

    // MIME-Types, die typischerweise beim Drag&Drop einer E-Mail direkt aus
    // Outlook (Desktop) auftreten. Outlook liefert dabei oft KEINE Dateiendung
    // und/oder einen leeren MIME-Type - das behandeln wir als Sonderfall.
    const EMAIL_LIKE_MIME = [
        'message/rfc822',
        'application/vnd.ms-outlook',
        'application/x-msg',
        ''
    ];

    function isAllowedFile(file) {
        const name = file.name.toLowerCase();
        const extOk = ALLOWED_EXT.some(ext => name.endsWith(ext));

        if (!extOk) {
            // Sonderfall: Datei ohne (bekannte) Endung, aber mit
            // E-Mail-artigem MIME-Type -> vermutlich Outlook-Drag.
            const hasNoExtension = !name.includes('.');
            const looksLikeEmailMime = EMAIL_LIKE_MIME.includes(file.type);
            return hasNoExtension && looksLikeEmailMime;
        }

        const mimeOk = ALLOWED_MIME.includes(file.type);
        return mimeOk;
    }

    // Falls die Datei aus Outlook ohne Endung kommt (z.B. nur der Betreff
    // als Dateiname), wird sie vor dem Upload als .eml umbenannt, damit
    // Server/DB/Windows sie später korrekt als E-Mail erkennen und öffnen
    // können.
    function normalizeFile(file) {
        const name = file.name.toLowerCase();
        const hasKnownExt = ALLOWED_EXT.some(ext => name.endsWith(ext));
        if (hasKnownExt) return file;

        if (!name.includes('.') && EMAIL_LIKE_MIME.includes(file.type)) {
            const newName = `${file.name}.eml`;
            return new File([file], newName, { type: file.type || 'message/rfc822' });
        }

        return file;
    }

    // Upload-Funktion, die die Datei an den Server sendet und die Antwort verarbeitet.
    async function uploadFile(file, parentObjectId) {
        file = normalizeFile(file);

        if (!isAllowedFile(file)) {
            await showAlert(
                `Datei "${file.name}" wurde abgelehnt: Erlaubt sind nur .docx, .xlsx, .pptx, .txt, .jpeg/.jpg, .msg und .eml Dateien.`,
                "error"
            );
            return;
        }

        const formData = new FormData();
        formData.append('parentObjectId', parentObjectId);
        formData.append('file', file);
        try {
            const response = await fetch(UPLOAD_URL, { method: 'POST', body: formData });
            if (!response.ok) {
                await showAlert("Fehler beim Speichern.", "error");
                return;
            }
            const result = await response.json();
            if (result.success) {
                await showAlert("Dokument wurde erstellt.", "success");
                const objectId = history.state?.objectId;
                if (objectId) {
                    window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${objectId}`);
                } else {
                    window.location.reload();
                }
            } else {
                await showAlert(result.message ?? "Fehler beim Speichern.", "error");
            }
        } catch {
            await showAlert("Netzwerkfehler.", "error");
        }
    }
})();