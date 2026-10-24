function getNode(node) {
    if (!node) return null;
    return {
        objectId: node.dataset.objectId,
        nodeType: node.dataset.nodeType,
        parentId:
            node.dataset.parentObjectId ||
            node.closest(".tree-node")?.dataset.parentObjectId
    };
}

// Klick auf "Icon hochladen" — öffnet den Datei-Dialog.
document.addEventListener("click", function (e) {
    if (!e.target.closest(".icon-item")) return;
    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none";
    document.getElementById("iconUpload").click();
});

// Datei ausgewählt — Icon hochladen.
document.getElementById("iconUpload").addEventListener("change", async function () {
    const file = this.files[0];
    if (!file) return;
    let node = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    if (!node) {
        node = document.querySelector(`.js-tree-node[data-object-id="${currentObjectId}"]`);
    }
    const nodeData = getNode(node);
    if (!nodeData) return;
    const formData = new FormData();
    formData.append("Icon", file);
    formData.append("ObjektId", nodeData.objectId);
    formData.append("NodeType", nodeData.nodeType);

    try {
        const response = await fetch("/bescheidbearbeitung/upload-icon", {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            throw new Error("Fehler beim Hochladen des Icons.");
        }

        const data = await response.json();
        updateNodeIconUI(nodeData.objectId, data.iconBase64);
        document.getElementById("iconUpload").value = "";
        await showAlert("Icon wurde erfolgreich hochgeladen.", "success");
    } catch (err) {
        await showAlert("Fehler beim Hochladen des Icons.", "error");
    }
});

// Klick auf "Icon löschen" — Icon entfernen.
document.addEventListener("click", async function (e) {
    if (!e.target.closest(".delete-icon-item")) return;
    const contextMenu = document.getElementById("contextMenu");
    if (contextMenu) contextMenu.style.display = "none";
    let node = document.querySelector(`.js-table-node[data-object-id="${currentObjectId}"]`);
    if (!node) {
        node = document.querySelector(`.js-tree-node[data-object-id="${currentObjectId}"]`);
    }
    const nodeData = getNode(node);
    if (!nodeData) return;
    const formData = new FormData();
    formData.append("ObjektId", nodeData.objectId);
    formData.append("NodeType", nodeData.nodeType);

    try {
        const response = await fetch("/bescheidbearbeitung/delete-icon", {
            method: "POST",
            body: formData
        });

        if (!response.ok) {
            throw new Error("Fehler beim Löschen des Icons.");
        }

        const data = await response.json();
        updateNodeIconUI(nodeData.objectId, data.iconBase64);
        await showAlert("Icon wurde erfolgreich gelöscht.", "success");
    } catch (err) {
        await showAlert("Fehler beim Löschen des Icons.", "error");
    }
});

// Aktualisiert Icon im Baum, Header und Tabelle nach Upload/Löschen.
function updateNodeIconUI(objectId, iconBase64) {

    // 1. Icon im Baum aktualisieren.
    const treeNode = document.querySelector(`.js-tree-node[data-object-id="${objectId}"]`);
    const treeNodeContainer = treeNode?.closest(".tree-node");
    const iconContainer = treeNodeContainer?.querySelector(
        ":scope > .tree-item .toggle-icon, :scope > .tree-item .cant-toggle, :scope > .tree-item .cant-icon"
    );

    const isOpen = treeNodeContainer?.classList.contains("open");

    if (iconContainer) {
        iconContainer.innerHTML = iconBase64
            ? `<img src="data:image/x-icon;base64,${iconBase64}" style="width:1em;" />`
            : isOpen
                ? `<i class="fa-solid fa-folder-open"></i>`
                : `<i class="fa-solid fa-folder"></i>`;
    }

    // 2. Header nach dem Tabellen-Reload aktualisieren.
    const currentObjectId = new URLSearchParams(window.location.search).get("ObjectId");

    function updateHeader() {
        if (currentObjectId !== objectId) return;
        const headerIcon = document.querySelector(
            ".info-header h2 img, .info-header h2 i, .info-header h2 .toggle-icon"
        );
        if (headerIcon) {
            headerIcon.outerHTML = iconBase64
                ? `<img src="data:image/x-icon;base64,${iconBase64}" style="width:1em;" />`
                : `<i class="fa-solid fa-folder"></i>`;
        }
    }

    // 3. Tabelle neu laden — nodeType aus history.state,
    if (currentObjectId) {
        const state = history.state;
        if (state && state.nodeType) {
            loadTableByNodeType(state.nodeType, currentObjectId, updateHeader);
        } else {
            fetch(`/bescheidbearbeitung/get-node-info?objectId=${encodeURIComponent(currentObjectId)}`)
                .then(function (response) {
                    return response.json();
                })
                .then(function (node) {
                    if (!node || !node.nodeType) return;
                    loadTableByNodeType(node.nodeType, currentObjectId, updateHeader);
                });
        }
    } else {
        // 4. Ohne ObjektId.
        fetch("/bescheidbearbeitung/get-access-mode")
            .then(function (response) {
                return response.text();
            })
            .then(function (mode) {
                if (mode === "Mitzeichnung") {
                    return fetch("/bescheidbearbeitung/load-vorgange-mitzeichnung")
                        .then(function (response) { return response.text(); })
                        .then(function (html) {
                            document.getElementById("tableContainer").innerHTML = html;
                        });
                } else {
                    return fetch("/bescheidbearbeitung/load-kategorien")
                        .then(function (response) { return response.text(); })
                        .then(function (html) {
                            document.getElementById("tableContainer").innerHTML = html;
                        });
                }
            });
        return;
    }
}