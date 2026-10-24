////////////////////////////////////////////////////////////////////////////   Vorschau per Leertaste (Word / PDF)
// Dateiendungen, für die Leertaste eine Aktion auslösen soll (Word + PDF).
const SPACE_PREVIEW_EXTENSIONS = [".pdf", ".doc", ".docx"];

// true, solange der gemeinsame Container als Vorschau (und nicht als PDF-Konverter) geöffnet ist.
let previewMode = false;

// Prüft, ob der Dateiname auf Word oder PDF endet.
function isWordOrPdf(filename) {
    const dotIndex = filename.lastIndexOf(".");
    if (dotIndex === -1) return false;
    return SPACE_PREVIEW_EXTENSIONS.includes(filename.substring(dotIndex).toLowerCase());
}

// Basis-URL des Vorschau-Endpunkts (gleiche Logik wie beim PDF-Konverter).
function getPreviewBaseUrl() {
    return window.location.hostname === "localhost"
        ? "https://localhost:7006/Dokument"
        : "https://dreif-gmbh.de/Dokument";
}

// Erstellt die Sitzung über denselben Endpunkt wie der PDF-Konverter und liefert das Token.
async function createPreviewToken(objectId) {
    try {
        const response = await fetch(
            `/bescheidbearbeitung/create-converter-session?objectId=${encodeURIComponent(objectId)}&mode=${encodeURIComponent("table")}`,
            { method: "POST", credentials: "same-origin" }
        );

        if (!response.ok) {
            if (response.status === 401) {
                alert("UserId not found — Sitzung möglicherweise abgelaufen");
            } else {
                alert("Vorschau-Session konnte nicht erstellt werden");
            }
            return null;
        }

        const data = await response.json();
        return data.token;
    } catch (err) {
        console.error("Fehler beim Erstellen der Vorschau-Session:", err);
        alert("Fehler beim Erstellen der Vorschau-Session");
        return null;
    }
}

function isPreviewOpen() {
    closeContextMenu()
    return previewMode &&
        document.getElementById("pdfConverterContainer")?.style.display === "block";

}

function closePreview() {
    previewMode = false;
    const container = document.getElementById("pdfConverterContainer");
    if (container) container.style.display = "none";
    const frame = document.getElementById("pdfConverterFrame");
    if (frame) frame.src = "about:blank";
}

// Öffnet die Vorschau für ein Dokument im vorhandenen iframe über den /Dokument-Endpunkt.
async function openDocumentPreview(objectId) {
    const token = await createPreviewToken(objectId);
    if (!token) return;

    const url =
        `${getPreviewBaseUrl()}?Token=${encodeURIComponent(token)}` +
        `&DocumentObjektId=${encodeURIComponent(objectId)}`;

    previewMode = true;
    document.getElementById("pdfConverterFrame").src = url;
    document.getElementById("pdfConverterContainer").style.display = "block";
}

// Leertaste: Vorschau für das erste ausgewählte Word/PDF-Dokument.
function previewSelectedWordOrPdf() {
    const id = Array.from(selectedIds).find(id => {
        const node = getNodeElement(id);
        return node
            && getNodeType(id) === "Dokument"
            && !isDeletedNode(node, id)
            && isWordOrPdf(getFilename(node, id));
    });

    if (id) openDocumentPreview(id);
}

document.addEventListener("keydown", function (e) {
    // Esc schließt die Vorschau.
    if (e.code === "Escape" && isPreviewOpen()) {
        closePreview();
        return;
    }

    if (e.code !== "Space") return;

    const tag = e.target.tagName;
    if (tag === "INPUT" || tag === "TEXTAREA" || e.target.isContentEditable) return;

    // Leertaste bei offener Vorschau schließt sie wieder (wie im Finder).
    if (isPreviewOpen()) {
        e.preventDefault();
        closePreview();
        return;
    }

    if (selectedIds.size === 0) return;

    e.preventDefault();
    previewSelectedWordOrPdf();
});

// Klick auf "Vorschau" im Kontextmenü — nur bei genau einem ausgewählten Element.
document.addEventListener("click", function (e) {
    if (!e.target.closest(".vorschau-item")) return;

    closeContextMenu();

    if (selectedIds.size !== 1) return;

    const id = Array.from(selectedIds)[0];
    const node = getNodeElement(id);
    if (!node
        || getNodeType(id) !== "Dokument"
        || isDeletedNode(node, id)
        || !isWordOrPdf(getFilename(node, id))) return;

    openDocumentPreview(id);
});