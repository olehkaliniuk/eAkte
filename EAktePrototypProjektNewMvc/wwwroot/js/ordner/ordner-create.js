// Speichert einen Ordner, entweder durch Erstellen oder Aktualisieren, basierend auf dem Modus (create/edit).
async function saveOrdner() {
    const nameInput = document.getElementById('inputName');
    const name = nameInput.value.trim();
    const bemerkung = document.getElementById('inputAbteilung').value.trim();
    const colorHex = document.getElementById('inputFarbe').value.trim();
    const parentObjectId = document.getElementById('modalOrdnerParentObjectId').value.trim();
    const mode = document.getElementById('modalOrdnerMode')?.value.trim();

    if (!name) {
        showNameError('Name ist ein Pflichtfeld.');
        return;
    }

    const payload = { name, bemerkungen: bemerkung, colorHex, objektId: parentObjectId, parentObjectId };
    const url = mode === 'edit'
        ? '/bescheidbearbeitung/update-ordner'
        : '/bescheidbearbeitung/create-ordner';

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!response.ok) {
            await showAlert("Fehler beim Speichern.", "error");
            return;
        }
        const result = await response.json();
        if (result.success) {
            closeModalOrdner();
            await showAlert(mode === 'edit' ? "Ordner wurde aktualisiert." : "Ordner wurde erstellt.", "success");
            const objectId = history.state?.objectId;
            if (objectId) window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${objectId}`);
        } else {
            await showAlert("Fehler beim Speichern.", "error");
        }
    } catch {
        await showAlert("Netzwerkfehler.", "error");
    }
}

// Reagiert live auf Eingaben im Name-Feld.
function handleNameInput() {

    const name = document.getElementById('inputName').value.trim();
    if (name) {
        showNameSuccess('Alle Pflichtfelder ausgefüllt.');
    } else {
        clearNameStatus();
    }
}

// Zeigt Fehlerzustand für das Name-Feld an.
function showNameError(message) {
    const nameInput = document.getElementById('inputName');
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');

    nameInput.classList.add('input-error');

    errorBox.classList.remove('text-success-modal');
    errorBox.classList.add('text-error-modal', 'visible');

    errorIcon.classList.remove('fa-circle-check', 'icon-success');
    errorIcon.classList.add('fa-circle-exclamation', 'icon-error');

    errorText.textContent = message;
}

// Zeigt positive Rückmeldung, wenn der Name ausgefüllt ist.
function showNameSuccess(message) {
    const nameInput = document.getElementById('inputName');
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');

    nameInput.classList.remove('input-error');

    errorBox.classList.remove('text-error-modal');
    errorBox.classList.add('text-success-modal', 'visible');

    errorIcon.classList.remove('fa-circle-exclamation', 'icon-error');
    errorIcon.classList.add('fa-circle-check', 'icon-success');

    errorText.textContent = message;
}

// Versteckt jeglichen Status (weder Fehler noch Erfolg).
function clearNameStatus() {
    const nameInput = document.getElementById('inputName');
    const errorBox = document.getElementById('nameErrorBox');

    nameInput.classList.remove('input-error');
    errorBox.classList.remove('visible', 'text-error-modal', 'text-success-modal');
}