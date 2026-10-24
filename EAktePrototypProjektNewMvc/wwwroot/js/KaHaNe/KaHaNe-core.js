async function openModalKaHaNe(objectType, mode, objectId) {
    if (isModalOpen) return;
    currentObjectType = objectType;
    currentMode = mode;
    const resolvedObjectId = objectId ?? new URLSearchParams(window.location.search).get("ObjectId");
    try {
        const params = new URLSearchParams({ objectType, mode });

        if (mode === 'edit') {
            params.set('ObjectId', resolvedObjectId);
        } else if (objectType !== 'kategorie') {
            params.set('ParentObjectId', resolvedObjectId);
        }

        const response = await fetch(`/bescheidbearbeitung/get-modal-data-ka-ha-ne?${params}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden des Modals.", "error");
            return;
        }
        const html = await response.text();
        document.getElementById('modalKaHaNeContainer').innerHTML = html;
        initDokDropZone();
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        return;
    }
    const modal = document.getElementById('modalKaHaNeContainer').querySelector('#fileModalKaHaNe');
    if (!modal) {
        await showAlert("Modal konnte nicht geöffnet werden.", "error");
        return;
    }
    modal.style.display = 'flex';
    isModalOpen = true;
    initDrag(modal);
    validateTabKahane();
}

function closeModalKaHaNe() {
    const modal = document.getElementById('modalKaHaNeContainer').querySelector('#fileModalKaHaNe');
    if (modal) modal.style.display = 'none';
    isModalOpen = false;
}