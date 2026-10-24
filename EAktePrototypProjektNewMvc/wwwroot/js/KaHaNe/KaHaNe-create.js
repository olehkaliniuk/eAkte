// Speichert Kategorie / Hauptgruppe / Nebengruppe (Create oder Edit).
async function saveKaHaNe() {
    const bezeichnungInput = document.getElementById('inputBezeichnung');
    const bezeichnung = bezeichnungInput.value.trim();
    const kuerzel = document.getElementById('inputKuerzel').value.trim();
    const beschreibung = document.getElementById('inputBeschreibung').value.trim();

    const objectType = document.getElementById('modalObjectType').value.trim();
    const mode = document.getElementById('modalMode').value.trim();
    const objectId = document.getElementById('modalObjectId').value.trim();
    const parentObjectId = document.getElementById('modalParentObjectId').value.trim();

    const iconAction = document.getElementById('inputIconAction').value.trim();
    const iconBase64 = document.getElementById('inputIconBase64').value.trim();

    if (!bezeichnung) {
        showNameErrorKaHaNe('Bezeichnung ist ein Pflichtfeld.');
        return;
    }

    // Ausgewählte Abteilungen einsammeln (nur relevant für objectType === 'kategorie').
    const abteilungen = Array.from(
        document.querySelectorAll('input[name="Abteilungen"]:checked')
    ).map(checkbox => checkbox.value);

    // Ausgewählte Referate einsammeln (nur relevant für objectType === 'hauptgruppe').
    const referate = Array.from(
        document.querySelectorAll('input[name="Referate"]:checked')
    ).map(checkbox => parseInt(checkbox.value, 10));


    const payload = {
        objectType,
        objectId,
        parentObjectId,
        bezeichnung,
        kuerzel,
        beschreibung,
        iconAction,
        iconBase64,
        abteilungen,
        referate
    };

    const url = mode === 'edit'
        ? '/bescheidbearbeitung/update-ka-ha-ne'
        : '/bescheidbearbeitung/create-ka-ha-ne';

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

        closeModalKaHaNe();
        await showAlert(mode === 'edit' ? "Eintrag wurde aktualisiert." : "Eintrag wurde erstellt.", "success");

        if (mode === 'edit') {
            window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${objectId}`);
        } else if (objectType === 'kategorie') {
            // Kategorie liegt auf oberster Ebene - kein ObjectId im URL nötig.
            window.location.href = appUrl('/bescheidbearbeitung');
        } else if (parentObjectId) {
            // Hauptgruppe/Nebengruppe erstellt - zurück zum übergeordneten Objekt.
            window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${parentObjectId}`);
        } else {
            window.location.reload();
        }
    } catch {
        await showAlert("Netzwerkfehler.", "error");
    }
}

// Reagiert live auf Eingaben im Bezeichnung-Feld.
function handleNameInput() {
    validateTabKahane();
}

// Prüft alle Pflichtfelder im Tab und zeigt Status an.
function validateTabKahane() {
    const bezeichnung = document.getElementById('inputBezeichnung').value.trim();

    if (bezeichnung) {
        showNameSuccessKaHaNe('Alle Pflichtfelder ausgefüllt.');
    } else {
        clearNameStatusKaHaNe();
    }
}

// Zeigt Fehlerzustand für das Bezeichnung-Feld an (KaHaNe-Modal).
function showNameErrorKaHaNe(message) {
    const nameInput = document.getElementById('inputBezeichnung');
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');
    if (!nameInput || !errorBox || !errorIcon || !errorText) return;

    nameInput.classList.add('input-error');
    errorBox.classList.remove('text-success-modal');
    errorBox.classList.add('text-error-modal', 'visible');
    errorIcon.classList.remove('fa-circle-check', 'icon-success');
    errorIcon.classList.add('fa-circle-exclamation', 'icon-error');
    errorText.textContent = message;
}

// Zeigt positive Rückmeldung, wenn die Bezeichnung ausgefüllt ist (KaHaNe-Modal).
function showNameSuccessKaHaNe(message) {
    const nameInput = document.getElementById('inputBezeichnung');
    const errorBox = document.getElementById('nameErrorBox');
    const errorIcon = document.getElementById('nameErrorIcon');
    const errorText = document.getElementById('nameErrorText');
    if (!nameInput || !errorBox || !errorIcon || !errorText) return;

    nameInput.classList.remove('input-error');
    errorBox.classList.remove('text-error-modal');
    errorBox.classList.add('text-success-modal', 'visible');
    errorIcon.classList.remove('fa-circle-exclamation', 'icon-error');
    errorIcon.classList.add('fa-circle-check', 'icon-success');
    errorText.textContent = message;
}

// Versteckt jeglichen Status (KaHaNe-Modal).
function clearNameStatusKaHaNe() {
    const nameInput = document.getElementById('inputBezeichnung');
    const errorBox = document.getElementById('nameErrorBox');
    if (!nameInput || !errorBox) return;

    nameInput.classList.remove('input-error');
    errorBox.classList.remove('visible', 'text-error-modal', 'text-success-modal');
}

// Verarbeitet Icon-Upload (Vorschau + Base64 für Speichern).
function handleIconUpload(event) {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
        const result = reader.result;
        const base64 = result.split(',')[1];

        const previewImg = document.getElementById('iconPreviewImg');
        const placeholder = document.getElementById('iconPlaceholder');
        const removeBtn = document.getElementById('btnRemoveIcon');

        previewImg.src = result;
        previewImg.style.display = '';
        if (placeholder) placeholder.style.display = 'none';
        removeBtn.style.display = '';

        document.getElementById('inputIconAction').value = 'new';
        document.getElementById('inputIconBase64').value = base64;
    };
    reader.onerror = () => {
        showAlert("Icon konnte nicht gelesen werden.", "error");
    };
    reader.readAsDataURL(file);
}

// Entfernt das aktuelle Icon (Vorschau + Markierung zum Löschen).
function removeIcon() {
    const previewImg = document.getElementById('iconPreviewImg');
    const placeholder = document.getElementById('iconPlaceholder');
    const removeBtn = document.getElementById('btnRemoveIcon');
    const fileInput = document.getElementById('inputIcon');

    previewImg.src = '';
    previewImg.style.display = 'none';
    if (placeholder) placeholder.style.display = '';
    removeBtn.style.display = 'none';
    if (fileInput) fileInput.value = '';

    document.getElementById('inputIconAction').value = 'remove';
    document.getElementById('inputIconBase64').value = '';
}