(function () {
    // Geist-Element wird beim Ziehen als visueller Cursor angezeigt.
    const ghost = document.createElement("div");
    ghost.id = "drag-ghost";
    document.body.appendChild(ghost);

    // Gemeinsamer Rahmen um alle gezogenen Elemente (nur bei Mehrfachauswahl).
    const sourceFrame = document.createElement("div");
    sourceFrame.id = "drag-source-frame";
    document.body.appendChild(sourceFrame);

    // Zustandsvariablen für den Drag-Vorgang.
    let active = false;
    let dragLis = [];          // Alle gezogenen Elemente (bei Einzelauswahl genau eines).
    let highlighted = null;
    let startX = 0;
    let startY = 0;
    let pendingLink = null;

    // Gibt den nächsten Tree-Container (bescheid oder mitzeichnung) für ein Element zurück.
    function getTreeContainer(el) {
        return el?.closest("#bescheidTreeContainer, #mitzeichnungTreeContainer") ?? null;
    }

    // Liefert den Baum-Eintrag (li) für eine objectId.
    function findTreeLi(objectId) {
        return document.querySelector(`a.js-tree-node[data-object-id="${objectId}"]`)?.closest("li.tree-node") ?? null;
    }

    // Zielelement wird blau hervorgehoben wenn die Maus darüber schwebt.
    function highlightTarget(el) {
        if (highlighted === el) return;
        clearTarget();
        if (el && !dragLis.includes(el)) {
            const link = el.querySelector("a.js-tree-node");
            const nodeType = link?.dataset.nodeType ?? "";

            // Cross-tree-Drag ist grundsätzlich ungültig (alle gezogenen Elemente müssen im selben Baum wie das Ziel liegen).
            const targetContainer = getTreeContainer(el);
            const sameTree = targetContainer && dragLis.every(li => getTreeContainer(li) === targetContainer);

            // Gültiges Ziel: gleicher Baum UND richtiger Knotentyp.
            const valid = sameTree && (nodeType === "Ordner" || nodeType === "UnterOrdner");
            el.classList.add(valid ? "drag-target-valid" : "drag-target-invalid");
            if (link) link.style.color = valid ? "#2563EB" : "#dc2626";
            highlighted = el;
        }
    }

    // Hervorhebung des vorherigen Zielelements wird zurückgesetzt.
    function clearTarget() {
        if (highlighted) {
            highlighted.classList.remove("drag-target-valid", "drag-target-invalid");
            const link = highlighted.querySelector("a.js-tree-node");
            if (link) link.style.color = "";
            highlighted = null;
        }
    }

    // Bestätigungsdialog zeigt Quelle und Ziel des Verschiebevorgangs an.
    async function showModal(fromLis, toLi) {
        const fromLinks = fromLis.map(li => li.querySelector("a.js-tree-node"));
        const fromIds = fromLinks.map(l => l?.dataset.objectId).filter(Boolean);
        const isMulti = fromIds.length > 1;

        // Einzelelement: Name/Typ/Id des ersten Elements.
        const fromName = fromLinks[0]?.textContent.trim() ?? "—";
        const fromType = fromLinks[0]?.dataset.nodeType ?? "—";
        const fromId = fromIds[0] ?? "—";

        const toLink = toLi?.querySelector("a.js-tree-node");
        const toName = toLink?.textContent.trim() ?? "—";
        const toId = toLink?.dataset.objectId ?? "—";

        const confirmed = await showConfirm(
            isMulti
                ? `${fromIds.length} Elemente werden nach "${toName}" verschoben.`
                : `"${fromName}" wird nach "${toName}" verschoben.`,
            "Verschieben",
            "Abbrechen"
        );

        if (!confirmed) return;

        // Modus anhand des Baums erkennen.
        const detectedMode = detectModeFromDB(fromId, fromType);
        try {
            await fetch("/bescheidbearbeitung/set-access-mode?mode=" + detectedMode);
        } catch {
            await fetch("/bescheidbearbeitung/set-access-mode?mode=" + "Bescheidbearbeitung");
        }

        try {
            // Einzel- und Mehrfachauswahl laufen über denselben Endpunkt.
            // Einzelelement: sourceType = Typ des Elements (UnterOrdner → Ordner-Zweig, Dokument → Dokument-Zweig).
            // Mehrfachauswahl: nur Dokumente → sourceType = null.
            const res = await fetch("/bescheidbearbeitung/clipboard/move-here", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    targetObjectId: toId,
                    sourceObjectIds: fromIds,
                    sourceType: isMulti ? null : fromType
                })
            });
            const json = await res.json();
            if (json.success) {
                await showAlert("Element wurde erfolgreich verschoben.", "success");
                // Nach dem Verschieben zum Ziel (Mehrfach) bzw. zum Element (Einzeln) springen.
                window.location.href = appUrl(`bescheidbearbeitung?ObjectId=${isMulti ? toId : fromId}`);
            } else {
                await showAlert(json.error ?? json.message ?? "Fehler beim Verschieben.", "error");
            }
        } catch {
            await showAlert("Netzwerkfehler.", "error");
        }
    }

    function onMouseDown(e) {
        // Nur linke Maustaste erlaubt, rechte Taste wird sofort abgewiesen.
        if (e.button !== 0) return;
        // Strg/Cmd-Klick ist für Mehrfachauswahl reserviert, kein Drag.
        if (e.ctrlKey || e.metaKey) return;
        const link = e.target.closest("a.js-tree-node");
        if (!link) return;

        // Nur bestimmte Knotentypen dürfen verschoben werden.
        const nodeType = link.dataset.nodeType ?? "";
        if (nodeType !== "Dokument" && nodeType !== "UnterOrdner") return;

        // Standardverhalten des Browsers wird verhindert damit Drag sofort reagiert.
        e.preventDefault();
        pendingLink = link;
        startX = e.clientX;
        startY = e.clientY;
    }

    function onMouseMove(e) {
        if (!pendingLink && !active) return;

        if (!active && pendingLink) {
            const dx = Math.abs(e.clientX - startX);
            const dy = Math.abs(e.clientY - startY);
            // Drag startet erst nach 5 Pixel Bewegung um normale Klicks nicht zu stören.
            if (dx < 5 && dy < 5) return;

            active = true;

            // Ist das gezogene Element Teil einer Mehrfachauswahl, wird die ganze Auswahl gezogen.
            const startId = pendingLink.dataset.objectId;
            const isMulti = selectedIds.size > 1 && selectedIds.has(startId);
            const ids = isMulti ? Array.from(selectedIds) : [startId];
            dragLis = ids.map(findTreeLi).filter(Boolean);

            pendingLink = null;
            // Einzelelement: eigene Markierung, Mehrfachauswahl: ein gemeinsamer Rahmen.
            if (dragLis.length === 1) dragLis[0].classList.add("drag-source-active");
            else updateSourceFrame();
            ghost.textContent = dragLis.length > 1
                ? `${dragLis.length} Elemente`
                : (dragLis[0]?.querySelector("a.js-tree-node")?.textContent.trim() ?? "");
            ghost.style.display = "block";
        }

        if (active) {
            moveGhost(e);
            // Rahmen folgt dem Scrollen / Layout.
            updateSourceFrame();
            // Quellelemente werden kurz versteckt damit elementFromPoint das darunterliegende Ziel erkennt.
            ghost.style.display = "none";
            dragLis.forEach(li => li.style.visibility = "hidden");
            const el = document.elementFromPoint(e.clientX, e.clientY);
            dragLis.forEach(li => li.style.visibility = "");
            ghost.style.display = "block";
            const target = el?.closest("li.tree-node");
            highlightTarget(target || null);
        }
    }

    // Geist-Element folgt der Mausbewegung.
    function moveGhost(e) {
        ghost.style.left = (e.clientX + 14) + "px";
        ghost.style.top = (e.clientY - 10) + "px";
    }

    // Rahmen wird auf die Gesamtfläche aller gezogenen Elemente gesetzt.
    function updateSourceFrame() {
        if (dragLis.length < 2) {
            sourceFrame.style.display = "none";
            return;
        }
        let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;
        dragLis.forEach(li => {
            const r = li.getBoundingClientRect();
            left = Math.min(left, r.left);
            top = Math.min(top, r.top);
            right = Math.max(right, r.right);
            bottom = Math.max(bottom, r.bottom);
        });
        sourceFrame.style.left = left + "px";
        sourceFrame.style.top = top + "px";
        sourceFrame.style.width = (right - left) + "px";
        sourceFrame.style.height = (bottom - top) + "px";
        sourceFrame.style.display = "block";
    }

    function onMouseUp(e) {
        pendingLink = null;

        if (!active) return;

        active = false;
        ghost.style.display = "none";

        const fromLis = dragLis;
        const toLi = highlighted;

        dragLis.forEach(li => li.classList.remove("drag-source-active"));
        sourceFrame.style.display = "none";
        dragLis = [];
        clearTarget();

        if (fromLis.length === 0) return;

        // Validierung vor dem Modal: Quellen und Ziel müssen gültige Typen haben.
        const toType = toLi?.querySelector("a.js-tree-node")?.dataset.nodeType ?? "";
        const validSource = fromLis.every(li => li.querySelector("a.js-tree-node")?.dataset.nodeType === "Dokument");
        const validTarget = toLi && (toType === "Ordner" || toType === "UnterOrdner");

        // Cross-tree-Drop wird blockiert — kein Modal öffnen.
        const targetContainer = toLi ? getTreeContainer(toLi) : null;
        const sameTree = targetContainer && fromLis.every(li => getTreeContainer(li) === targetContainer);

        if (!validSource || !validTarget || !sameTree) return;

        showModal(fromLis, toLi);
    }

    document.addEventListener("mousedown", onMouseDown);
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);

})();