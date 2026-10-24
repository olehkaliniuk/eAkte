// Protocol mapping for Office file extensions to their respective protocols.
const officeProtocolMap = {
    ".doc": "ms-word", ".docx": "ms-word", ".dotx": "ms-word",
    ".xls": "ms-excel", ".xlsx": "ms-excel", ".xltx": "ms-excel",
    ".ppt": "ms-powerpoint", ".pptx": "ms-powerpoint", ".potx": "ms-powerpoint"
};



// Anzeigen.
// Dokument herunterladen / öffnen.
async function handleDocumentDownload(versionId, fileName) {
    const ext = "." + fileName.split(".").pop().toLowerCase();
    const protocol = officeProtocolMap[ext];

    if (protocol) {
        // Office-Datei: sofort read-only im nativen Client öffnen statt Download-Dialog.
        try {
            const res = await fetch("/bescheidbearbeitung/start-view", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: `versionId=${versionId}`
            });
            const data = await res.json();
            if (!data.success) {
                await showAlert("Fehler beim Öffnen.", "error");
                return;
            }
            window.location.href = `${protocol}:ofv|u|${data.editUrl}`;
        } catch {
            await showAlert("Fehler beim Öffnen.", "error");
        }
        return;
    }

    // Alles andere: normaler Blob-Download wie bisher.
    try {
        const res = await fetch(`/bescheidbearbeitung/download-dokument?versionId=${versionId}`);
        if (!res.ok) {
            await showAlert("Fehler beim Herunterladen.", "error");
            return;
        }
        const blob = await res.blob();
        const disposition = res.headers.get("Content-Disposition");
        let filename = "download";
        if (disposition) {
            const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (match) filename = match[1].replace(/['"]/g, "");
        }
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    } catch {
        await showAlert("Fehler beim Herunterladen.", "error");
    }
}

// Variante 1: Klick auf den Download-Button.
$(document).on("click", ".btn-download", function (e) {
    e.stopPropagation(); // verhindert, dass der Klick an übergeordnete Handler durchgereicht wird.
    const versionId = $(this).data("object-id");
    const fileName = $(this).data("filename");
    handleDocumentDownload(versionId, fileName);
});

// Variante 2: Doppelklick auf die Zeile.
$(document).on("dblclick", ".js-table-node", function () {
    const nodeType = $(this).data("node-type");
    if (nodeType !== "Dokument") {
        return; // bei UnterOrdner/Ordner löst der Doppelklick keinen Download aus.
    }

    const versionId = $(this).data("version-id");
    const fileName = $(this).data("filename");

    if (!versionId || !fileName) {
        return; // Zeile enthält keine gültigen Download-Daten.
    }

    handleDocumentDownload(versionId, fileName);
});

// Variante 3: Klick auf "Anzeigen" im Kontextmenü. Wir simulieren einen Doppelklick auf die Zeile.
document.querySelector(".view-item").addEventListener("click", function () {
    const row = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    if (row) {
        const dblClickEvent = new MouseEvent("dblclick", {
            bubbles: true,
            cancelable: true,
            view: window
        });
        row.dispatchEvent(dblClickEvent);
    }

    // Kontextmenü schließen.
    document.getElementById("contextMenu").style.display = "none";
});



// Kontextmenü.
// Klick auf Eigenschaften im Kontextmenü. Wir simulieren einen Klick auf das Info-Icon in der Zeile.
document.querySelector(".properties-item").addEventListener("click", function () {
    const row = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    if (row) {
        const infoIcon = row.querySelector(".btn-info-table");
        if (infoIcon) {
            infoIcon.click();
        }
    }

    // Kontextmenü schließen.
    document.getElementById("contextMenu").style.display = "none";
});

// Klick auf Bearbeiten im Kontextmenü. Wir simulieren einen Klick auf das Edit-Icon in der Zeile.
document.querySelector(".edit-item").addEventListener("click", function () {
    const row = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    if (row) {
        // Nicht klicken, wenn das Dokument gerade bearbeitet wird (von mir oder einem anderen Benutzer).
        const lockedBy = row.dataset.lockedBy || "";
        const isLocked = pendingEdits.has(currentObjectId) || (lockedBy && lockedBy !== window.currentUId);

        const editIcon = row.querySelector(".btn-edit");
        if (editIcon && !isLocked) {
            editIcon.click();
        }
    }

    // Kontextmenü schließen.
    document.getElementById("contextMenu").style.display = "none";
});



// Bearbeitung.
// Dokument in Word/Excel bearbeiten. (edit button)
$(document).on("click", ".btn-edit", async function () {
    const versionId = $(this).data("version-id");
    const $btn = $(this);
    const $row = $btn.closest(".js-table-node");
    const objectId = $row.data("object-id");
    try {
        const res = await fetch("/bescheidbearbeitung/start-edit", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: `versionId=${versionId}`
        });
        const data = await res.json();
        if (!data.success) {
            await showAlert("Fehler beim Starten der Bearbeitung.", "error");
            return;
        }

        // Office-Protokoll anhand der Dateiendung bestimmen.
        const protocolMap = {
            ".doc": "ms-word",
            ".docx": "ms-word",
            ".dotx": "ms-word",
            ".xls": "ms-excel",
            ".xlsx": "ms-excel",
            ".xltx": "ms-excel",
            ".ppt": "ms-powerpoint",
            ".pptx": "ms-powerpoint",
            ".potx": "ms-powerpoint"
        };

        const ext = (data.contentType || "").toLowerCase();
        const protocol = protocolMap[ext];

        if (!protocol) {
            await showAlert("Dieser Dateityp kann nicht direkt geöffnet werden.", "error");
            return;
        }

        // Token merken, damit das Kontextmenü die richtigen Aktionen anzeigen kann.
        pendingEdits.set(objectId, { token: data.token, versionId, $row });
        // Icons ausblenden, Schlüssel-Icon zeigen.
        $row.find(".btn-edit").hide();
        $row.find(".edit-pending-key").show();

        window.location.href = `${protocol}:ofe|u|${data.editUrl}`;

        // Polling starten, damit die Seite automatisch aktualisiert wird, sobald gespeichert wurde.
        startPollingEditStatus(data.token, objectId, $row);
    } catch {
        await showAlert("Fehler beim Starten der Bearbeitung.", "error");
    }
});

// Übernehmen (neue Version).
$(document).on("click", ".edit-confirm-item", async function () {
    const pending = pendingEdits.get(currentObjectId);
    if (!pending) return;
    try {
        const res = await fetch("/bescheidbearbeitung/confirm-edit", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: `token=${pending.token}`
        });
        const data = await res.json();
        if (data.success) {
            await showAlert("Änderungen übernommen.", "success");
            location.reload();
        } else {
            await showAlert(data.message ?? "Fehler beim Übernehmen.", "error");
        }
    } catch {
        await showAlert("Fehler beim Übernehmen.", "error");
    }
});

// Übernehmen (gleiche Version).
$(document).on("click", ".edit-confirm-same-item", async function () {
    const pending = pendingEdits.get(currentObjectId);
    if (!pending) return;
    try {
        const res = await fetch("/bescheidbearbeitung/confirm-edit-same-version", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: `token=${pending.token}`
        });
        const data = await res.json();
        if (data.success) {
            await showAlert("Änderungen übernommen.", "success");
            location.reload();
        } else {
            await showAlert(data.message ?? "Fehler beim Übernehmen.", "error");
        }
    } catch {
        await showAlert("Fehler beim Übernehmen.", "error");
    }
});

// Bearbeitung überspringen.
$(document).on("click", ".edit-skip-item", async function () {
    const pending = pendingEdits.get(currentObjectId);
    if (!pending) return;
    const objectId = currentObjectId;

    // Menü sofort schließen.
    document.getElementById("contextMenu").style.display = "none";
    document.querySelectorAll(".js-tree-node").forEach(function (el) {
        el.classList.remove("tree-selected");
    });
    document.querySelectorAll(".js-table-node").forEach(function (el) {
        el.classList.remove("table-selected");
    });

    try {
        await fetch("/bescheidbearbeitung/skip-edit", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: `token=${pending.token}`
        });
    } finally {
        resetEditRow(objectId);
    }
});



// Hilfsfunktionen.
// Seite aktualisieren, sobald die Bearbeitung in Word abgeschlossen ist.
function startPollingEditStatus(editToken, objectId, $row) {
    const poll = setInterval(async () => {
        try {
            const res = await fetch(`/bescheidbearbeitung/edit-session-status?token=${editToken}`);
            const data = await res.json();

            if (data.completed) {
                clearInterval(poll);
                pendingEdits.delete(objectId);
                location.reload();
            } else if (data.failed) {
                clearInterval(poll);
                pendingEdits.delete(objectId);
                $row.find(".edit-pending-key").hide();
                $row.find(".btn-edit").show();
                showAlert(data.message || "Fehler beim Speichern des Dokuments.", "error");
            }
        } catch (err) {
            console.error("Polling-Fehler:", err);
        }
    }, 2000);
}

// Setzt die Zeile nach Abschluss der Bearbeitung zurück.
function resetEditRow(objectId) {
    const pending = pendingEdits.get(objectId);
    if (!pending) return;
    pending.$row.find(".edit-pending-key").hide();
    pending.$row.find(".btn-download, .btn-edit").show();
    pendingEdits.delete(objectId);
}


