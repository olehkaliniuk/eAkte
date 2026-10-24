// Initialisieren.
async function openModalDokument(objectType, mode, objectId) {
    if (isModalOpen) return;

    // Edit modus öffnet das Modal nicht, sondern klickt den Bearbeiten-Button.
    if (mode === "edit") {
        const realBtn = document.querySelector('.btn-edit');
        if (realBtn) {
            realBtn.click()
        } else {
            await showAlert("Bearbeiten-Button nicht gefunden.", "error");
        }
        return; 
    }

    currentObjectType = objectType;
    currentMode = mode;

    const resolvedObjectId = objectId ?? new URLSearchParams(window.location.search).get("ObjectId");
    try {
        const params = new URLSearchParams({ ObjectId: resolvedObjectId, mode });
        const response = await fetch(`/bescheidbearbeitung/get-modal-data-dokument?${params}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden des Modals.", "error");
            return;
        }
        const html = await response.text();
        document.getElementById('modalDokumentContainer').innerHTML = html;
        initDokDropZone();
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        return;
    }

    const modal = document.getElementById('modalDokumentContainer').querySelector('#fileModalDokument');
    if (!modal) {
        await showAlert("Modal konnte nicht geöffnet werden.", "error");
        return;
    }
    modal.style.display = 'flex';
    isModalOpen = true;
    initDrag(modal);
}

function closeModalDokument() {
    const modal = document.getElementById('modalDokumentContainer').querySelector('#fileModalDokument');
    if (modal) modal.style.display = 'none';
    isModalOpen = false;
}
