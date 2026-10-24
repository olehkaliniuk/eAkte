// Modal für Ordner öffnen.
async function openModalOrdner(objectType, mode, objectId) {
    if (isModalOpen) return;
    currentObjectType = objectType;
    currentMode = mode;
    const resolvedObjectId = objectId ?? new URLSearchParams(window.location.search).get("ObjectId");
    // HTML-Inhalt vom Server laden.
    try {
        const params = new URLSearchParams({ ObjectId: resolvedObjectId, mode });
        const response = await fetch(`/bescheidbearbeitung/get-modal-data-ordner?${params}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden des Modals.", "error");
            return;
        }
        const html = await response.text();
        document.getElementById('modalOrdnerContainer').innerHTML = html;
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        return;
    }
    // Modal aus dem Container holen (kam per fetch rein).
    const modal = document.getElementById('modalOrdnerContainer').querySelector('#fileModalOrdner');
    if (!modal) {
        console.error('Modal-Element nicht gefunden (#fileModalOrdner) im Container');
        return;
    }
    // Modal sichtbar machen.
    modal.style.display = 'flex';
    isModalOpen = true;
    // Drag-Funktion initialisieren.
    initDrag(modal);
    initColorPicker();
    handleNameInput();
}

// Modal schließen.
function closeModalOrdner() {
    const modal = document.getElementById('modalOrdnerContainer').querySelector('#fileModalOrdner');
    if (modal) modal.style.display = 'none';
    isModalOpen = false;
}
