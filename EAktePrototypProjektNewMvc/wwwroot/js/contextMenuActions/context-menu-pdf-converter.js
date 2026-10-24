// Menüpunkt "PDF-Konverter" — click.
document.querySelector(".pdf-converter")?.addEventListener("click", handlePdfConverterClick);

// Handler für den Klick auf den Menüpunkt "PDF-Konverter".
function handlePdfConverterClick() {
    closeContextMenu();
    clearSelection();
    openPdfConverterFromMenu(currentObjectId);
}

// Öffnet den PDF-Konverter in einem Iframe für das übergebene Objekt.
async function openPdfConverterFromMenu(objectId) {
    if (!objectId) {
        alert('ObjectId not found');
        return;
    }

    let token;
    try {
        const response = await fetch(
            `/bescheidbearbeitung/create-converter-session?objectId=${encodeURIComponent(objectId)}&mode=${encodeURIComponent('table')}`,
            { method: 'POST', credentials: 'same-origin' }
        );

        if (!response.ok) {
            if (response.status === 401) {
                alert('UserId not found — Sitzung möglicherweise abgelaufen');
            } else {
                alert('PDF-Konverter-Session konnte nicht erstellt werden');
            }
            return;
        }

        const data = await response.json();
        token = data.token;
    } catch (err) {
        alert('Fehler beim Erstellen der Konverter-Session');
        return;
    }


    const baseUrl = window.location.hostname === "localhost"
        ? "https://localhost:7006/testKonverter/Index"
        : "https://dreif-gmbh.de/testKonverter/Index";

    const targetUrl = `${baseUrl}?Token=${encodeURIComponent(token)}`;

    const iframe = document.getElementById('pdfConverterFrame');
    iframe.src = targetUrl;
    document.getElementById('pdfConverterContainer').style.display = 'block';
}


