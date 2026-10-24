////////////////////////////////////////////////////////////////////////////////  Konstanten und Zustand
// Alle steuerbaren Menüeinträge. Was nicht explizit angezeigt wird, wird versteckt.
const MENU_ITEMS = [
    ".delete-item",
    ".rename-item",
    ".delete-icon-item",
    ".icon-item",
    ".no-permission",
    ".no-select-info",
    ".edit-confirm-item",
    ".edit-confirm-same-item",
    ".edit-skip-item",
    ".view-item",
    ".edit-item",
    ".properties-item",
    ".clipboard-item",
    ".copy-here-item",
    ".move-here-item",
    ".calendar",
    ".email",
    ".pdf-converter",
    ".restore-item",
    ".purge-item",
    ".vorschau-item"
];

// Dateiendungen, die mit dem Online-Editor bearbeitet werden können.
const EDITABLE_EXTENSIONS = [".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt"];
const NODE_TYPES_WITH_ICON = ["Kategorie", "Hauptgruppe", "Nebengruppe", "Akte", "Vorgang"];
const DEFAULT_NO_PERMISSION_TEXT = "Keine Berechtigung";

// Laufende Bearbeitungen: objectId - { token, versionId, $row }.
const pendingEdits = new Map();

// currentObjectId / currentParentId.
let currentSource = null;

// Mehrfachauswahl. Enthält die objectIds aller aktuell ausgewählten Elemente. Bei genau einem Eintrag = (Einzelauswahl).
let selectedIds = new Set();

// Zwischenspeicher für Kopieren/Verschieben. Enthält die objectIds, die zuletzt über ".clipboard-item" gemerkt wurden.
let clipboardIds = new Set();




///////////////////////////////////////////////////////////////////////////////   Hilfsfunktionen
// Liefert das Menü-Element.
function getMenu() {
    return document.getElementById("contextMenu");
}

// Liefert die Tabellenzeile für ein Objekt, falls vorhanden. (Baumknoten ist immer vorhanden.)
function findTableRow(objectId) {
    return document.querySelector(`.js-table-node[data-object-id="${objectId}"]`);
}

// Liefert irgendeine Repräsentation des Knotens (Baum bevorzugt, sonst Tabelle).
function getNodeElement(objectId) {
    return document.querySelector(`.js-tree-node[data-object-id="${objectId}"]`) || findTableRow(objectId);
}

// Liefert den Knotentyp.
function getNodeType(objectId) {
    const el = getNodeElement(objectId);
    return el ? el.dataset.nodeType : undefined;
}

// Entfernt nur die CSS-Klassen, ohne den Auswahl-Zustand (selectedIds) anzufassen.
function clearSelectionStyles() {
    document.querySelectorAll(".js-tree-node.tree-selected")
        .forEach(el => el.classList.remove("tree-selected"));

    document.querySelectorAll(".js-table-node.table-selected")
        .forEach(el => el.classList.remove("table-selected"));
}

// Leert die Auswahl komplett (Zustand + Darstellung).
function clearSelection() {
    selectedIds.clear();
    clearSelectionStyles();
}

// Zeichnet die Markierung für alle aktuell ausgewählten Elemente neu, sowohl im Baum als auch in der Tabelle (ein Objekt kann in beiden vorkommen).
function applyHighlighting() {
    // Zuerst alle Markierungen entfernen, dann die ausgewählten Elemente markieren.
    clearSelectionStyles();

    selectedIds.forEach(id => {
        document.querySelectorAll(`.js-tree-node[data-object-id="${id}"]`)
            .forEach(el => el.classList.add("tree-selected"));
        document.querySelectorAll(`.js-table-node[data-object-id="${id}"]`)
            .forEach(el => el.classList.add("table-selected"));
    });
}

// Setzt die Auswahl auf genau ein Element (1 Element). 
function selectSingle(objectId) {
    selectedIds.clear();
    selectedIds.add(objectId);
    applyHighlighting();
}

// Fügt ein Element der Auswahl hinzu bzw. entfernt es (Strg/Cmd-Klick). (Multiselct).
function toggleInSelection(objectId) {
    if (selectedIds.has(objectId)) {
        selectedIds.delete(objectId);
    } else {
        selectedIds.add(objectId);
    }
    applyHighlighting();
}

// Zeigt genau die übergebenen Einträge an und blendet alle übrigen aus.
function showMenuItems(visibleSelectors) {
    const menu = getMenu();
    MENU_ITEMS.forEach(selector => {
        const el = menu.querySelector(selector);
        if (el) el.style.display = visibleSelectors.includes(selector) ? "flex" : "none";
    });
}

// Positioniert das Menü an den übergebenen Koordinaten und zeigt es an.
function positionAndShowMenu(x, y) {
    const menu = getMenu();
    menu.style.top = y + "px";
    menu.style.left = x + "px";
    menu.style.display = "block";
}

// Liefert den Dateinamen des Knotens.
function getFilename(node, objectId) {
    if (node.dataset.filename) return node.dataset.filename;
    const row = findTableRow(objectId);
    return row ? (row.dataset.filename || "") : "";
}

// Prüft, ob die Datei bearbeitet werden kann (Office-Dateien).
function isEditableFile(filename) {
    const dotIndex = filename.lastIndexOf(".");
    if (dotIndex === -1) return false;
    return EDITABLE_EXTENSIONS.includes(filename.substring(dotIndex).toLowerCase());
}

// Lock-Information steht am Baumknoten nicht immer zur Verfügung.
function getLockedBy(node, objectId) {
    if (node.dataset.lockedBy) return node.dataset.lockedBy;
    const row = findTableRow(objectId);
    return row ? (row.dataset.lockedBy || "") : "";
}

// Prüft, ob das Dokument gelöscht ist (data-deleted="1" bzw. "True").
function isDeletedNode(node, objectId) {
    const raw = node.dataset.deleted ?? findTableRow(objectId)?.dataset.deleted ?? "";
    const value = raw.toString().toLowerCase();
    return value === "1" || value === "true";
}

// Setzt den Text für den Fall, dass keine Berechtigung besteht.
function setNoPermissionText(text) {
    const el = getMenu().querySelector(".no-permission-text");
    if (el) el.textContent = text;
}

// "Hierher kopieren/verschieben" ist nur erlaubt, wenn der Zwischenspeicher gefüllt ist UND das Zielobjekt nicht selbst eines der gemerkten Elemente ist.
function canPasteHere(targetObjectId) {
    return clipboardIds.size > 0 && !clipboardIds.has(targetObjectId);
}

// Liefert die Zwischenablage-Einträge für das Menü: ".clipboard-item" ist immer verfügbar, "hierher kopieren/verschieben" nur, wenn canPasteHere(targetObjectId) zutrifft.
function getClipboardMenuItems(targetObjectId) {
    const items = [".clipboard-item"];
    if (canPasteHere(targetObjectId)) {
        items.push(".copy-here-item", ".move-here-item");
    }
    return items;
}




/////////////////////////////////////////////////////////////////////////////   Sichtbarkeitsregeln des Menüs
// Liefert die sichtbaren Einträge für den jeweiligen Zustand des Knotens (Einzelauswahl).
function resolveVisibleItems(node, objectId) {
    const nodeType = node.dataset.nodeType;
    const isDokument = nodeType === "Dokument";
    const isUnterOrdner = nodeType === "UnterOrdner";
    const isOrdner = nodeType === "Ordner";


    // FALL 0: Gelöschtes Dokument — nur "Wiederherstellen".
    if (isDokument && isDeletedNode(node, objectId)) {
        return [".restore-item", ".purge-item"];
    }

    // FALL 1: Dokument wird von mir selbst bearbeitet (Token liegt lokal vor).
    if (pendingEdits.has(objectId)) {
        return [".edit-confirm-item", ".edit-confirm-same-item", ".edit-skip-item"];
    }

    // FALL 2: Dokument ist von einem anderen Benutzer gesperrt.
    const lockedBy = getLockedBy(node, objectId);
    const isAdmin = window.userRoles.includes("Administrator");
    const isAdminModeActive = isAdmin && window.adminMode;

    if (isDokument && lockedBy && lockedBy !== window.currentUId) {

        // Administrator im Admin-Modus darf die Bearbeitungssitzung des anderen Benutzers übernehmen.
        if (isAdminModeActive) {
            return [".edit-confirm-item", ".edit-confirm-same-item", ".edit-skip-item"];
        }

        setNoPermissionText(`Wird gerade von ${lockedBy} bearbeitet`);
        return [".no-permission"];
    }

    // FALL 3: Unterordner — löschen und umbenennen, keine Icon-Verwaltung.
    // "Hierher kopieren/verschieben" nur, wenn zuvor etwas in den Zwischenspeicher gelegt wurde.
    if (isUnterOrdner) {
        const items = [".delete-item", ".rename-item"];
        if (canPasteHere(objectId)) items.push(".copy-here-item", ".move-here-item");
        return items;
    }

    // FALL 4: Administrator im Admin-Modus — Icon ändern oder löschen.
    const canManageIcon = NODE_TYPES_WITH_ICON.includes(nodeType);
    if (canManageIcon && isAdmin && window.adminMode) {
        return [".delete-item", ".delete-icon-item", ".icon-item"];
    }

    // FALL 5: Dokument — Standardaktionen.
    if (isDokument) {
        const filename = getFilename(node, objectId);
        const items = [
            ".pdf-converter",
            ".delete-item",
            ".view-item",
            ".properties-item",
            ...getClipboardMenuItems(objectId),
            ".calendar",
            ".email"
        ];

        // Vorschau nur bei Word/PDF.
        if (isWordOrPdf(filename)) items.push(".vorschau-item");

        // "Bearbeiten" nur bei Office-Dateien.
        if (isEditableFile(filename)) items.push(".edit-item");
        return items;
    }

    // FALL 6: Ordner if Copy/Move is allowed, show those options. Otherwise, no permission.
    if (isOrdner) {
        const items = [];
        if (canPasteHere(objectId)) items.push(".copy-here-item", ".move-here-item")
        else items.push(".no-permission");
        return items;
    }

    // FALL 7: Keine Berechtigung oder unbekannter Knotentyp.
    return [".no-permission"];
}

// Sichtbarkeitsregeln bei Mehrfachauswahl (mehr als ein Element markiert). Erlaubt nur Kopieren/Löschen, und nur wenn ALLE ausgewählten Elemente Dokumente sind. Sind ALLE ausgewählten Dokumente gelöscht, wird nur "Wiederherstellen" angeboten.
function resolveVisibleItemsMulti(targetObjectId) {
    const ids = Array.from(selectedIds);

    const allDocuments = ids.every(id => getNodeType(id) === "Dokument");
    if (!allDocuments) {
        setNoPermissionText("Mehrfachauswahl ist nur für Dokumente möglich");
        return [".no-permission"];
    }

    // Gelöschte Dokumente: entweder alle gelöscht (nur Wiederherstellen) oder gemischt (blockieren).
    const deletedFlags = ids.map(id => {
        const el = getNodeElement(id);
        return !!el && isDeletedNode(el, id);
    });
    const allDeleted = deletedFlags.every(Boolean);
    const anyDeleted = deletedFlags.some(Boolean);

    if (allDeleted) {
        return [".restore-item", ".purge-item"];
    }
    if (anyDeleted) {
        setNoPermissionText("Bitte nur gelöschte oder nur aktive Dokumente auswählen");
        return [".no-permission"];
    }

    // Ist eines der ausgewählten Dokumente gesperrt oder wird gerade bearbeitet,
    // wird die Mehrfachaktion komplett blockiert.
    const hasBlocked = ids.some(id => {
        if (pendingEdits.has(id)) return true;
        const el = getNodeElement(id);
        const lockedBy = el ? getLockedBy(el, id) : "";
        return !!(lockedBy && lockedBy !== window.currentUId);
    });
    if (hasBlocked) {
        setNoPermissionText("Eines der ausgewählten Dokumente wird gerade bearbeitet");
        return [".no-permission"];
    }

    return [".delete-item", ...getClipboardMenuItems(targetObjectId)];
}



/////////////////////////////////////////////////////////////////////////////   Klick-Handler Wiederherstellen/Endgültig löschen
// Klick auf "Wiederherstellen" im Menü. Nur bei Einzelauswahl möglich.
document.addEventListener("click", async function (e) {
    if (!e.target.closest(".restore-item")) return;
    if (!currentObjectId && selectedIds.size === 0) return;

    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none";

    await restoreDokument(currentObjectId);
});

// Klick auf "Endgültig löschen" im Menü. Nur bei Einzelauswahl möglich.
document.addEventListener("click", async function (e) {
    if (!e.target.closest(".purge-item")) return;
    if (!currentObjectId && selectedIds.size === 0) return;

    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none";

    await purgeDokument(currentObjectId);
});




///////////////////////////////////////////////////////////////////////////////   Kontextmenü-Lgik Open/Close
// Öffnet das Kontextmenü für den übergebenen Knoten an den angegebenen Koordinaten.
function openContextMenu(node, x, y, source) {
    currentObjectId = node.dataset.objectId;
    currentParentId =
        node.dataset.parentObjectId ||
        node.closest(".tree-node")?.dataset.parentObjectId;

    // Rechtsklick auf ein Element, das bereits Teil einer Mehrfachauswahl ist, behält diese Auswahl bei. Andernfalls wird auf dieses eine Element reduziert.
    if (!(selectedIds.size > 1 && selectedIds.has(currentObjectId))) {
        selectSingle(currentObjectId);
    }

    if (selectedIds.size > 1) {
        showMenuItems(resolveVisibleItemsMulti(currentObjectId));
    } else {
        showMenuItems(resolveVisibleItems(node, currentObjectId));
    }
    positionAndShowMenu(x, y);
}

// Schließt das Kontextmenü und alle Untermenüs. Wird z. B. beim Klick außerhalb des Menüs aufgerufen.
function closeContextMenu() {
    document.querySelectorAll(".context-menu-item.has-submenu.open")
        .forEach(item => item.classList.remove("open"));
    getMenu().style.display = "none";
}

// Öffnen des Menüs Baumansicht (links).
document.addEventListener("contextmenu", function (e) {
    const node = e.target.closest(".js-tree-node");
    if (!node) return;

    e.preventDefault();
    currentSource = "tree";
    openContextMenu(node, e.pageX, e.pageY, "tree");
});

// Tabellenzeilen.
document.addEventListener("contextmenu", async function (e) {
    const row = e.target.closest(".js-table-node");
    if (!row) return;

    e.preventDefault();
    currentSource = "table";

    const objectId = row.dataset.objectId;

    // Die Wiederherstellen-Logik der Edit-Session ergibt nur bei Einzelauswahl Sinn.
    if (!(selectedIds.size > 1 && selectedIds.has(objectId))) {
        await restoreEditSession(objectId, row);
    }

    // Zeile ohne Auswahlmöglichkeit: nur Hinweis, keine Aktionen.
    if (row.classList.contains("no-select")) {
        selectSingle(objectId);
        showMenuItems([".no-select-info"]);
        positionAndShowMenu(e.pageX, e.pageY);
        return;
    }

    // Menülogik basiert primär auf dem Baumknoten, Zeile dient als Fallback.
    const treeNode = document.querySelector(`.js-tree-node[data-object-id="${objectId}"]`) || row;
    openContextMenu(treeNode, e.pageX, e.pageY, "table");
});

// Nach einem Reload liegt der Token lokal nicht mehr vor — vom Server nachladen.
async function restoreEditSession(objectId, row) {
    const lockedByMe =
        window.currentUId &&
        row.dataset.lockedBy === window.currentUId;

    const isAdminModeActive =
        window.userRoles.includes("Administrator") &&
        window.adminMode;

    if (pendingEdits.has(objectId) || (!lockedByMe && !isAdminModeActive)) {
        return;
    }

    try {
        const res = await fetch(
            `/bescheidbearbeitung/my-edit-session?objectId=${objectId}`
        );

        const data = await res.json();

        if (data.found) {
            pendingEdits.set(objectId, {
                token: data.token,
                versionId: data.versionId,
                $row: $(row)
            });
        }
    } catch (err) {
        console.error("Konnte Edit-Sitzung nicht nachladen:", err);
    }
}

// Linksklick steuert die Auswahl.
// - normaler Klick auf ein Element: Auswahl auf dieses eine Element setzen
// - Strg/Cmd-Klick auf ein Element: Element der Auswahl hinzufügen/entfernen
// - Klick außerhalb (kein Element, kein Menü): Menü schließen und Auswahl leeren
document.addEventListener("click", function (e) {
    if (e.target.closest("#contextMenu")) return;
    closeContextMenu();

    const node = e.target.closest(".js-tree-node, .js-table-node");
    const multi = e.ctrlKey || e.metaKey;

    // Обычный левый клик (или клик мимо элемента): ничего не выбираем, сбрасываем выбор.
    // Навигацию по ссылкам не блокируем.
    if (!node || !multi) {
        clearSelection();
        return;
    }

    // Ctrl/Cmd + клик: работает только если выбор уже начат правым кликом.
    if (selectedIds.size === 0) return;

    // Кнопки/поля внутри узла (например, стрелку раскрытия) не трогаем.
    const interactive = e.target.closest("button, input, .tree-toggle"); // подставьте свой класс стрелки
    if (interactive && interactive !== node) return;

    e.preventDefault();   // не открывать ссылку в новой вкладке
    e.stopPropagation();  // не запускать навигацию дерева
    toggleInSelection(node.dataset.objectId);
}, true); // capture, чтобы сработать раньше навигации дерева




////////////////////////////////////////////////////////////////////////////   Zwischenablage: Kopieren / Verschieben
// ".clipboard-item" merkt sich die aktuelle Auswahl im Zwischenspeicher. ".copy-here-item" / ".move-here-item" werden erst angezeigt, sobald der Zwischenspeicher gefüllt ist (siehe getClipboardMenuItems) und sind aktuell nur Platzhalter (console.log).
function copyToClipboard() {
    clipboardIds = new Set(selectedIds);
    closeContextMenu();
    renderClipboardModal();
    showClipboardModal();
    console.log("Zwischenspeicher:", Array.from(clipboardIds)); 
}

// Kopiert die gemerkten Elemente in das aktuell ausgewählte Zielobjekt.
async function copySelectionHere(targetObjectId) {
    const ids = Array.from(clipboardIds);
    closeContextMenu();
    hideClipboardModal();

    try {
        const res = await fetch("/bescheidbearbeitung/clipboard/copy-here", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                targetObjectId,
                sourceObjectIds: ids,
                sourceType: ids.length === 1 ? getNodeType(ids[0]) : null
            })
        });
        const data = await res.json();

        if (!data.success) {
            alert(data.message || "Kopieren fehlgeschlagen.");
            return;
        }
        // Nach erfolgreichem Kopieren die Seite neu laden, um die Änderungen anzuzeigen.
        const url = new URL(window.location.href);
        url.searchParams.set("ObjectId", targetObjectId);
        window.location.href = url.toString();
    } catch (err) {
        console.error("Kopieren fehlgeschlagen:", err);
        alert("Kopieren fehlgeschlagen.", err);
    } finally {
        clearSelection();
        clipboardIds.clear();
    }
}

// Verschiebt die gemerkten Elemente in das aktuell ausgewählte Zielobjekt.
async function moveSelectionHere(targetObjectId) {
    const ids = Array.from(clipboardIds);
    closeContextMenu();
    hideClipboardModal();

    try {
        const res = await fetch("/bescheidbearbeitung/clipboard/move-here", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                targetObjectId,
                sourceObjectIds: ids,
                sourceType: ids.length === 1 ? getNodeType(ids[0]) : null
            })
        });
        const data = await res.json();

        if (!data.success) {
            alert(data.message || "Verschieben fehlgeschlagen.");
            return;
        }
        // Nach dem Verschieben die Seite neu laden, um die Änderungen anzuzeigen.
        const url = new URL(window.location.href);
        url.searchParams.set("ObjectId", targetObjectId);
        window.location.href = url.toString();
    } catch (err) {
        console.error("Verschieben fehlgeschlagen:", err);
        alert("Verschieben fehlgeschlagen.");
    } finally {
        clearSelection();
        clipboardIds.clear();
    }
}

getMenu().querySelector(".clipboard-item")?.addEventListener("click", copyToClipboard);
getMenu().querySelector(".copy-here-item")?.addEventListener("click", () => copySelectionHere(currentObjectId));
getMenu().querySelector(".move-here-item")?.addEventListener("click", () => moveSelectionHere(currentObjectId));

// Clipboard-Modal: Zeigt die gemerkten Elemente an, falls der Benutzer wissen möchte, was sich im Zwischenspeicher befindet. Liefert eine Anzeigebezeichnung für ein Objekt im Zwischenspeicher (Dateiname/Name, sonst objectId).
function getClipboardItemLabel(objectId) {
    const node = getNodeElement(objectId);
    if (!node) return objectId;
    return node.dataset.filename || node.dataset.name || node.textContent.trim() || objectId;
}

// Baut die Liste im Modal aus clipboardIds neu auf.
function renderClipboardModal() {
    const modal = document.getElementById("clipboardModal");
    if (!modal) return;
    const list = modal.querySelector(".clipboard-modal-list");
    const count = modal.querySelector(".clipboard-modal-count");

    list.innerHTML = "";
    clipboardIds.forEach(id => {
        const li = document.createElement("li");
        li.className = "clipboard-modal-item";
        li.dataset.id = id;

        const label = document.createElement("span");
        label.className = "clipboard-modal-item-label";
        label.textContent = getClipboardItemLabel(id);
        li.appendChild(label);

        const removeBtn = document.createElement("button");
        removeBtn.type = "button";
        removeBtn.className = "clipboard-modal-item-remove";
        removeBtn.title = "Element entfernen";
        removeBtn.innerHTML = '<i class="fas fa-times"></i>';
        removeBtn.addEventListener("click", () => removeFromClipboardItem(id));
        li.appendChild(removeBtn);

        list.appendChild(li);
    });

    count.textContent = clipboardIds.size;
    count.style.display = clipboardIds.size ? "" : "none";
}

// Entfernt ein einzelnes Element aus dem Zwischenspeicher.
// Schließt das Modal automatisch, wenn dadurch keine Elemente mehr übrig sind.
function removeFromClipboardItem(objectId) {
    clipboardIds.delete(objectId);
    if (clipboardIds.size === 0) {
        hideClipboardModal();
        return;
    }
    renderClipboardModal();
}

function showClipboardModal() {
    document.getElementById("clipboardModal")?.classList.add("visible");
}

function hideClipboardModal() {
    document.getElementById("clipboardModal")?.classList.remove("visible");
}

// Entfernt alle aktuell ausgewählten Elemente aus der Auswahl
// und schließt anschließend die Zwischenablage.
function clearClipboardSelection() {
    clipboardIds.clear();
    clearSelection();
    hideClipboardModal();
}

document.querySelector(".clipboard-clear-btn")?.addEventListener("click", clearClipboardSelection);

// Gemeinsame Logik für "Einfügen"-Kontextmenü an einem Ort ohne eigenes Element (Ordner-Header oder leere/teilweise leere Tabelle). Zeigt NUR die Zwischenablage-Einfügeoptionen, keine Delete/Rename-Optionen.
function openPasteOnlyMenu(objectId, x, y) {
    if (!objectId) return;

    currentObjectId = objectId;
    currentParentId = objectId;

    // Vorherige Auswahl (z. B. aus der Tabelle) darf hier nicht weiterwirken.
    clearSelection();

    if (!canPasteHere(objectId)) {
        // Zwischenspeicher leer (oder Ziel liegt selbst im Zwischenspeicher) -> kein Menü.
        return;
    }

    showMenuItems([".copy-here-item", ".move-here-item"]);
    positionAndShowMenu(x, y);
}




//////////////////////////////////////////////////////////////////////////  Click Header oder Click Table Empty
// Rechtsklick auf den Ordner-Header oberhalb der Tabelle. Existiert immer, auch wenn die Tabelle leer ist.
document.addEventListener("contextmenu", function (e) {
    const header = e.target.closest(".js-folder-node");
    if (!header) return;

    e.preventDefault();
    currentSource = "folder-header";
    openPasteOnlyMenu(header.dataset.objectId, e.pageX, e.pageY);
});

// Rechtsklick irgendwo im Tabellenbereich, aber NICHT auf einer konkreten Zeile.
document.addEventListener("contextmenu", function (e) {
    const container = e.target.closest("#dokument-table-container");
    if (!container) return;
    if (e.target.closest(".js-table-node")) return; // eigener Handler übernimmt

    e.preventDefault();
    currentSource = "table-empty";
    openPasteOnlyMenu(container.dataset.parentId, e.pageX, e.pageY);
});




