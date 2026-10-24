// Löscht ein oder mehrere Dokumente/Elemente aus dem Baum oder der Tabelle.
document.addEventListener("click", async function (e) {
    if (!e.target.closest(".delete-item")) return;
    if (!currentObjectId && selectedIds.size === 0) return;

    const isMulti = selectedIds.size > 1;
    const ids = isMulti ? Array.from(selectedIds) : [currentObjectId];

    // Bei Mehrfachauswahl garantiert resolveVisibleItemsMulti() bereits, dass
    // ausschließlich Dokumente (und keine gesperrten) ausgewählt sind.
    let nodeType;
    if (isMulti) {
        nodeType = "Dokument";
    } else if (currentSource === "tree") {
        nodeType = document.querySelector(".js-tree-node.tree-selected")?.dataset.nodeType;
    } else {
        nodeType = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`)?.dataset.nodeType;
    }

    const confirmed = await showConfirm(
        isMulti ? `${ids.length} Dokumente wirklich löschen?` : "Element wirklich löschen?",
        "Ja, löschen", "Abbrechen"
    );
    if (!confirmed) return;

    let url = "";
    if (nodeType === "Dokument") {
        url = "/bescheidbearbeitung/delete-dokument";
    } else if (!isMulti && nodeType === "UnterOrdner") {
        url = "/bescheidbearbeitung/delete-ordner";
    } else if (!isMulti && ["Kategorie", "Hauptgruppe", "Nebengruppe"].includes(nodeType)) {
        url = "/bescheidbearbeitung/delete-ka-ha-ne";
    } else {
        return;
    }

    const isKaHaNe = url === "/bescheidbearbeitung/delete-ka-ha-ne";
    const isDokument = url === "/bescheidbearbeitung/delete-dokument";

    try {
        let response;

        if (isKaHaNe) {
            response = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ objectId: currentObjectId, parentId: currentParentId, objectType: nodeType })
            });
        } else if (isDokument) {
            // Einheitliches Format: Server behandelt 1 oder N Ids identisch.
            response = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ objectIds: ids })
            });
        } else {
            const params = new URLSearchParams({ objectId: currentObjectId });
            response = await fetch(url, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: params.toString()
            });
        }

        if (response.ok) {
            const contextMenu = document.getElementById("contextMenu");
            if (contextMenu) contextMenu.style.display = "none";
            await showAlert(
                isMulti ? "Dokumente wurden erfolgreich gelöscht." : "Element wurde erfolgreich gelöscht.",
                "success"
            );
            if (nodeType === "Kategorie") {
                window.location.href = appUrl("/bescheidbearbeitung");
            } else {
                window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${currentParentId}`);
            }
        } else if (response.status === 409) {
            let message = "Element(e) konnten nicht gelöscht werden (Ordner nicht leer oder Dokument gesperrt).";
            try {
                const data = await response.json();
                message = data?.message || message;
            } catch { /* kein JSON-Body vorhanden */ }
            await showAlert(message, "warning");
        } else {
            await showAlert("Fehler beim Löschen.", "error");
        }
    } catch (err) {
        await showAlert("Fehler beim Löschen.", "error");
    }
});

// Stellt ein oder mehrere gelöschte Dokumente wieder her.
async function restoreDokument(objectId, event) {
    if (event) event.stopPropagation();

    const isMulti = selectedIds.size > 1 && selectedIds.has(objectId);
    const ids = isMulti ? Array.from(selectedIds) : [objectId];

    const confirmed = await showConfirm(
        isMulti ? `${ids.length} Dokumente wirklich wiederherstellen?` : "Dokument wirklich wiederherstellen?",
        "Ja, wiederherstellen", "Abbrechen"
    );
    if (!confirmed) return;

    try {
        const response = await fetch("/bescheidbearbeitung/restore-dokument", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ objectIds: ids })
        });

        if (response.ok) {
            await showAlert(
                isMulti ? "Dokumente wurden erfolgreich wiederhergestellt." : "Dokument wurde erfolgreich wiederhergestellt.",
                "success"
            );
            // Weiterleitung auf das (bei Mehrfachauswahl: zuletzt) wiederhergestellte Dokument.
            const targetId = ids[ids.length - 1];
            window.location.href = appUrl(`/bescheidbearbeitung?ObjectId=${targetId}`);
        } else {
            await showAlert("Fehler beim Wiederherstellen.", "error");
        }
    } catch (err) {
        await showAlert("Fehler beim Wiederherstellen.", "error");
    }
}

// Löscht ein oder mehrere gelöschte Dokumente endgültig (nicht wiederherstellbar).
async function purgeDokument(objectId, event) {
    if (event) event.stopPropagation();

    const isMulti = selectedIds.size > 1 && selectedIds.has(objectId);
    const ids = isMulti ? Array.from(selectedIds) : [objectId];

    const confirmed = await showConfirm(
        isMulti
            ? `${ids.length} Dokumente endgültig löschen? Dies kann nicht rückgängig gemacht werden.`
            : "Dokument endgültig löschen? Dies kann nicht rückgängig gemacht werden.",
        "Ja, endgültig löschen", "Abbrechen"
    );
    if (!confirmed) return;

    try {
        const response = await fetch("/bescheidbearbeitung/purge-dokument", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ objectIds: ids })
        });

        if (response.ok) {
            await showAlert(
                isMulti ? "Dokumente wurden endgültig gelöscht." : "Dokument wurde endgültig gelöscht.",
                "success"
            );
            window.location.reload();
        } else {
            await showAlert("Fehler beim endgültigen Löschen.", "error");
        }
    } catch (err) {
        await showAlert("Fehler beim endgültigen Löschen.", "error");
    }
}

// Benennt ein Element (Dokument / UnterOrdner) um.
document.addEventListener("click", function (e) {
    if (!e.target.closest(".rename-item")) return;
    if (!currentObjectId) {
        return;
    }
    let node;
    // Bestimmt die Quelle (Baum oder Tabelle).
    if (currentSource === "tree") {
        node = document.querySelector(".js-tree-node.tree-selected");
    } else {
        node = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    }
    const nodeType = node?.dataset.nodeType;
    // Erlaubt das Umbenennen nur für bestimmte Typen.
    if (nodeType !== "UnterOrdner" && nodeType !== "Dokument") {
        return;
    }
    let link;
    // Holt das Element mit dem Text.
    if (currentSource === "tree") {
        link = document.querySelector(".tree-selected");
    }
    else if (currentSource === "table") {
        link = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"] td:nth-child(2) .node-name`);
    }
    const oldName = link.textContent.trim();
    // Schließt das Kontextmenü.
    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none";
    // Erstellt ein Input-Feld für die Bearbeitung.
    const input = document.createElement("input");
    input.type = "text";
    input.className = "rename-input";
    input.value = oldName;
    // Blendet den Text aus und fügt das Input-Feld ein.
    link.style.display = "none";
    link.after(input);
    input.focus();
    input.select();

    // Verhindert doppeltes Ausführen von restoreLink.
    let isResolved = false;

    // Stellt den ursprünglichen Text wieder her (bei Abbruch).
    function restoreLink(name) {
        if (isResolved) return;
        isResolved = true;
        input.removeEventListener("blur", onBlur);
        link.textContent = name;
        link.style.display = "";
        input.remove();
    }

    // Bei Fokusverlust wird automatisch gespeichert.
    function onBlur() {
        restoreLink(oldName);
    }
    input.addEventListener("blur", onBlur);

    // Speichert den neuen Namen.
    function saveRename(newName) {

        // Verhindert, dass 'blur' während des Speicherns nochmal restoreLink auslöst.
        input.removeEventListener("blur", onBlur);

        // Wenn leer – wird der alte Name wiederhergestellt.
        if (!newName) {
            restoreLink(oldName);
            return;
        }

        let url = "";

        // Bestimmt den passenden Endpoint.
        if (nodeType === "UnterOrdner") {
            url = "/bescheidbearbeitung/rename-ordner";
        }

        const params = new URLSearchParams({
            objectId: currentObjectId,
            name: newName
        });

        fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: params.toString()
        })
            .then(async function (response) {
                if (!response.ok) {
                    const bodyText = await response.text().catch(() => "");
                    console.error("Rename fehlgeschlagen:", response.status, bodyText);
                    throw new Error(`Fehler beim Umbenennen (Status ${response.status}).`);
                }

                // Aktualisiert das aktuelle Element (wo bearbeitet wurde).
                restoreLink(newName);

                // Aktualisiert alle Stellen in der UI.
                updateNodeUI(currentObjectId, newName);

                await showAlert("Erfolgreich umbenannt.", "success");
            })
            .catch(async function (err) {
                console.error(err);
                await showAlert("Fehler beim Umbenennen.", "error");
                restoreLink(oldName);
            });
    }

    // Behandelt Tastatureingaben.
    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            saveRename(this.value);
        }
        if (e.key === "Escape") {
            restoreLink(oldName);
        }
    });
});

// Aktualisieren der UI nach einer Umbenennung. Aktualisiert alle Stellen, an denen das Element angezeigt wird.
function updateNodeUI(objectId, newName) {
    // 1. Aktualisiert den Baum.
    document.querySelectorAll(`.js-tree-node[data-object-id="${objectId}"]`)
        .forEach(function (el) {
            el.textContent = newName;
        });

    // 2. Aktualisiert die Tabelle.
    document.querySelectorAll(`.js-table-node[data-object-id="${objectId}"]`)
        .forEach(function (el) {
            const nameEl = el.querySelector(".node-name");
            if (nameEl) nameEl.textContent = newName;
        });

    // 3. Aktualisiert den Header, falls das aktuelle Element geöffnet ist.
    const header = document.querySelector(".header-text-name");
    if (header && header.dataset.objectId === String(objectId)) {
        header.textContent = newName;
    }

    // 4. Aktualisiert den Unten-Header, falls das aktuelle Element geöffnet ist.
    const tableHeaderText = document.querySelector(".table-header-text");
    const header2 = document.querySelector(".table-header-text span");

    if (tableHeaderText && tableHeaderText.dataset.objectId === String(objectId)) {
        if (header2) header2.textContent = newName;
    }

    // 3. Aktualisiert die Breadcrumb-Navigation (Pfad).
    const breadcrumb = document.querySelector(`#pathContent span[data-objectid="${objectId}"]`);
    if (breadcrumb) breadcrumb.textContent = newName;
}


