// Submenü E-Mail.
document.querySelectorAll(".context-menu-item.has-submenu").forEach(item => {
    item.addEventListener("click", function (e) {
        e.stopPropagation();
        // Andere offene Submenüs zuerst schließen.
        document.querySelectorAll(".context-menu-item.has-submenu.open")
            .forEach(other => { if (other !== this) other.classList.remove("open"); });
        this.classList.toggle("open");
    });
});

// Submenü-Items.
document.querySelectorAll(".submenu .context-menu-item").forEach(sub => {
    sub.addEventListener("click", async function (e) {
        e.stopPropagation();
        closeContextMenu();

        switch (this.dataset.action) {
            case "email-link":
                createEmailLink(currentObjectId);
                break;
            case "email-copy":
                await createEmailDraftFile(currentObjectId);
                break;
            case "email-pdf":
                await createEmailWithPdfAttachment(currentObjectId);
                break;
        }
    });
});

// Liefert die URL zum Dokument.
function buildDocumentUrl(objectId) {
    return `${window.location.origin}/bescheidbearbeitung?ObjectId=${objectId}`;
}

// Liefert den Titel des Dokuments.
function getDocumentTitle(objectId) {
    const row = findTableRow(objectId);
    return row?.dataset.filename || row?.dataset.name || "Dokument";
}

// Extrahiert den Dateinamen aus dem Content-Disposition-Header.
function extractFilename(disposition) {
    if (!disposition) return null;
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
    return match ? decodeURIComponent(match[1]) : null;
}

// email-link: nur Verweis, kein Serverkontakt.
function createEmailLink(objectId) {
    const documentUrl = buildDocumentUrl(objectId);
    const subject = encodeURIComponent(getDocumentTitle(objectId));
    const body = encodeURIComponent(`Link zum Dokument:\n${documentUrl}`);
    window.location.href = `mailto:?subject=${subject}&body=${body}`;
}

// email-copy: .eml mit Anhang vom Server holen und herunterladen.
async function createEmailDraftFile(objectId) {
    try {
        const res = await fetch(`/bescheidbearbeitung/create-email-draft?objectId=${objectId}`, {
            credentials: "same-origin"
        });

        if (!res.ok) {
            alert("E-Mail-Entwurf konnte nicht erstellt werden.");
            return;
        }

        const filename = extractFilename(res.headers.get("Content-Disposition")) || "Dokument.eml";
        const blob = await res.blob();
        downloadBlob(blob, filename);
    } catch (err) {
        console.error(err);
        alert("Fehler beim Erstellen des E-Mail-Entwurfs.");
    }
}


// Basis-URL des Konverters: lokal oder Produktion.
function getConverterBaseUrl() {
    return window.location.hostname === "localhost"
        ? "https://localhost:7006/testKonverter"
        : "https://dreif-gmbh.de/testKonverter";
}

// PDF: Session im Konverter erstellen, fertiges PDF abrufen, dann E-Mail-Entwurf mit PDF-Anhang vom Hauptsystem erzeugen.
async function createEmailWithPdfAttachment(objectId) {
    const token = await createConverterSession(objectId);
    if (!token) return;

    let stagedToken;
    try {
        const response = await fetch(
            `${getConverterBaseUrl()}/pdf/GetPdfStagedFileStream?Token=${encodeURIComponent(token)}`,
            { method: "GET" }
        );

        if (!response.ok) {
            alert("PDF konnte nicht im Konverter erstellt werden.");
            return;
        }

        stagedToken = await response.text();

        if (!stagedToken) {
            alert("Der Konverter hat keinen gültigen Token geliefert.");
            return;
        }
    } catch (err) {
        console.error(err);
        alert("Fehler beim Erstellen des PDF im Konverter.");
        return;
    }

    try {
        const res = await fetch(`/bescheidbearbeitung/create-email-draft-pdf`, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ token: stagedToken })
        });

        if (!res.ok) {
            alert("E-Mail-Entwurf mit PDF konnte nicht erstellt werden.");
            return;
        }

        const outFilename = extractFilename(res.headers.get("Content-Disposition")) || "Dokument.eml";
        const blob = await res.blob();
        downloadBlob(blob, outFilename);
    } catch (err) {
        console.error(err);
        alert("Fehler beim Erstellen des E-Mail-Entwurfs mit PDF.");
    }
}

// Erstellt eine Konverter-Session auf dem Server und gibt das Token zurück.
async function createConverterSession(objectId) {
    try {
        const response = await fetch(
            `/bescheidbearbeitung/create-converter-session?ObjectId=${encodeURIComponent(objectId)}&mode=${encodeURIComponent("table")}`,
            { method: "POST", credentials: "same-origin" }
        );

        if (!response.ok) {
            alert(response.status === 401
                ? "UserId not found — Sitzung möglicherweise abgelaufen"
                : "PDF-Konverter-Session konnte nicht erstellt werden");
            return null;
        }

        const data = await response.json();
        return data.token;
    } catch (err) {
        console.error(err);
        alert("Fehler beim Erstellen der Konverter-Session");
        return null;
    }
}