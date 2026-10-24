

                                                                                      // State Handling  //

//############################################################################### objectId , nodeType speichern ###############################################################################//
// Speichert den aktuellen Zustand (objectId , nodeType) im Browser-Verlauf.
function saveState(objectId, nodeType) {
    history.replaceState(
        { objectId, nodeType },
        ""
    );
    renderDropdownButtons(); // Dropdown aktualisieren.
    showBtnEditV(); // Edit-Button für Vorgang anzeigen, wenn zutreffend.
    showPDFConverterButton(); // PDF Converter Button anzeigen, wenn zutreffend.
    showDokumentenubersichtButton(); // Dokumentenübersicht Button anzeigen, wenn zutreffend.
}


                                                                                          // Algemein //


//###############################################################################             Start             ###############################################################################//
$(document).ready(function () {
    // Prüfen ob der Baum exestiert + wir sind NICHT auf Login page. 
    if ($("#menu").length === 0) return;
    // Holt die ObjectId aus der URL.
    const objectId = new URLSearchParams(window.location.search).get("ObjectId");

    // STANDARD-TABELLE, wenn keine objectId vorhanden ist oder Mitzeichnung-Modus aktiv ist.
    if (!objectId) {
        $.get("/bescheidbearbeitung/get-access-mode", function (mode) {
            if (mode === "Mitzeichnung") {
                $("#tableContainer").load("/bescheidbearbeitung/load-vorgange-mitzeichnung");
            } else {
                $("#tableContainer").load("/bescheidbearbeitung/load-kategorien");
            }
        });
        return;
    }

    // ERKENNUNG DES NODE-TYPS.
    $.get("/bescheidbearbeitung/get-node-info", { objectId }, function (node) {

        // Abbruch, wenn keine gültigen Daten vorhanden sind.
        if (!node || !node.nodeType) return;

        // Speichert den aktuellen Zustand (objectId, nodeType).
        saveState(objectId, node.nodeType);

        // ERKENNT, OB DER KNOTEN IM MITZEICHNUNG- ODER IM BESCHEID-BAUM LIEGT.
        detectModeFromDB(objectId, node.nodeType);

        // BAUM AUFBAUEN BIS ZUM KNOTEN.
        openTreeToNode(objectId, function () {

            // TABELLE LADEN.
            loadTableByNodeType(node.nodeType, objectId);

            // PFAD LADEN (BREADCRUMB).
            $.get("/bescheidbearbeitung/get-node-path-with-node-type", {
                objectId,
                nodeType: node.nodeType
            }, function (path) {

                // Rendert den Pfad im UI.
                renderPath(path);

                if (path.length > 0) {
                    // Setzt den letzten Knoten als aktiv.
                    setActiveNode(path[path.length - 1].objectId);
                }
            });

        });
    });
    // AKTUALISIERT DEN BACK-BUTTON.
    updateBackButton();
});


//#######################################################################    Modus anhand des Baums erkennen    ###############################################################################//
// Ermittelt den Modus (Mitzeichnung oder Bescheidbearbeitung) anhand der Datenbank und setzt ihn in der Session.
function detectModeFromDB(objectId, nodeType) {
    let mode = "Bescheidbearbeitung";
    $.ajax({
        url: `/bescheidbearbeitung/check-access-mode`,
        type: "GET",
        async: false,
        data: {
            ObjektId: objectId,
            NodeType: nodeType
        },
        success: function (isMitzeichnung) {
            mode = isMitzeichnung ? "Mitzeichnung" : "Bescheidbearbeitung";
        },
        error: function () {
            console.error("detectModeFromDB: request failed");
        }
    });

    console.log(mode)

    // Modus in der Session setzen.
    $.ajax({
        url: `/bescheidbearbeitung/set-access-mode`,
        type: "GET",
        async: false,
        data: { mode: mode },
        error: function () {
            console.error("setAccessMode: request failed");
        }
    });

    return mode;
}



//###############################################################################       Breadcrumb rendern      ###############################################################################//
// Rendert den Breadcrumb basierend auf den übergebenen Daten.
function renderPath(path) {

    let html = `<span class="path-item" onclick="window.location.href=appUrl('bescheidbearbeitung')">/bescheidbearbeitung</span>`;

    for (const p of path) {
        html += `<span class="separator">/</span><span class="path-item"
            data-objectid="${p.objectId}"
            data-nodetype="${p.nodeType || ''}"
            onclick="navigateToNode(this.dataset.objectid, this.dataset.nodetype)">${p.title}</span>`;
    }

    $("#pathContent").html(html);
}


//###############################################################################          Baum öffnen          ###############################################################################//
// Öffnet den Baum bis zum angegebenen Knoten (objectId).
function openTreeToNode(objectId, callback) {

    const state = history.state;

    const nodeType = state?.nodeType;

    if (!nodeType) {
        callback && callback();
        return;
    }

    $.get("/bescheidbearbeitung/get-node-path-with-node-type", {
        objectId,
        nodeType
    }, function (path) {
        openPathSequentially(path, 0, callback);
    });
}


//###############################################################################     Baum (Knoten) rendern     ###############################################################################//
// Öffnet den gesamten Pfad im Baum Schritt für Schritt basierend auf dem Index.
function openPathSequentially(path, index, callback) {

    // Schutz vor leerem Pfad.
    if (!path || path.length === 0) {
        callback && callback();
        return;
    }

    // Prüft, ob das Ende des Pfades erreicht wurde.
    if (index >= path.length) {
        const lastNode = path[path.length - 1];

        // Setzt den letzten Knoten als aktiv.
        setActiveNode(lastNode.objectId);

        // Rendert den Pfad erneut (Breadcrumb aktualisieren).
        renderPath(path);

        // Führt Callback aus, nachdem der gesamte Pfad geöffnet wurde.
        callback && callback();
        return;
    }

    // Holt den aktuellen Knoten basierend auf dem Index.
    const node = path[index];

    // Sucht das entsprechende DOM-Element im Baum.
    const li = $(".tree-node[data-object-id='" + node.objectId + "']");

    // Wenn der Knoten bereits existiert, wird er geöffnet und mit dem nächsten fortgefahren.
    if (li.length) {
        openNodeForPath(li, node.nodeType, function () {
            openPathSequentially(path, index + 1, callback);
        });
        return;
    }

    // Knoten nicht im DOM gefunden - überspringen und weiter zum nächsten.
    // (passiert im Mitzeichnung-Modus: Kategorie/Hauptgruppe/Nebengruppe/Akte fehlen im Baum)
    openPathSequentially(path, index + 1, callback);
}


//###############################################################################     Aktiven Knoten setzen     ###############################################################################//
// Setzt den aktiven Knoten im Baum basierend auf objectId.
function setActiveNode(objectId) {

    // Entfernt den aktiven Zustand von allen Knoten.
    $(".tree-node").removeClass("active");

    // Prüft, ob eine objectId übergeben wurde.
    if (!objectId)
    {
        return;
    }

    // Setzt den aktiven Zustand auf den ausgewählten Knoten.
    $(".tree-node[data-object-id='" + objectId + "']")
        .addClass("active");
}


//############################################################################### Tabellen laden nach Knotentyp ###############################################################################//
// Lädt die passende Tabelle abhängig vom nodeType.
function loadTableByNodeType(nodeType, objectId) {

    switch (nodeType) {
        case "Kategorie":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-hauptgruppen?objectId=" + objectId);
            break;

        case "Hauptgruppe":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-nebengruppen?objectId=" + objectId);
            break;

        case "Nebengruppe":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-akten?objectId=" + objectId);
            break;

        case "Akte":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-vorgange?objectId=" + objectId);
            break;

        case "Vorgang":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-ordner?objectId=" + objectId);
            break;

        case "Ordner":
        case "UnterOrdner":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-unterordner?objectId=" + objectId);
            break;

        case "Dokument":
            $("#tableContainer")
                .load("/bescheidbearbeitung/load-dokument?objectId=" + objectId);
            break;

    }
}


//###########################################################################  Knoten für Pfad öffnen (Lazy Loading)  #########################################################################//
// Öffnet einen Knoten im Baum und lädt bei Bedarf seine Kinder (nur für Pfad-Navigation).
function openNodeForPath(li, nodeType, callback) {

    // Prüft, ob ein gültiges Element übergeben wurde.
    if (!li || !li.length) {
        callback && callback();
        return;
    }

    // Wenn der Knoten ein Dokument ist, gibt es keine Kinder zum Laden.
    if (nodeType === "Dokument") {
        callback && callback();
        return;
    }

    // Wenn der Knoten bereits geöffnet ist.
    if (li.hasClass("open")) {
        callback && callback();
        return;
    }

    // Holt das Icon des Knotens.
    const icon = li.find(".toggle-icon i").first();

    // Lädt die Kinder (nur einmal) und öffnet danach den Knoten.
    loadChildrenOnce(li, nodeType, function () {

        // Setzt den Knoten als geöffnet.
        li.addClass("open");

        // Aktualisiert das Icon auf "geöffneten Ordner".
        icon.removeClass("fa-folder").addClass("fa-folder-open");

        // Führt Callback aus.
        callback && callback();
    });
}

//###############################################################################   Kinder genau einmal laden   ###############################################################################//
// Lädt die Kindknoten eines Knotens genau einmal. Parallele Aufrufe warten auf dieselbe Anfrage.
function loadChildrenOnce(li, nodeType, callback) {

    // Kinder wurden bereits geladen.
    if (li.data("loaded") === true) {
        callback && callback();
        return;
    }

    // Prüft, ob bereits eine Anfrage läuft.
    let pending = li.data("loadingPromise");

    // Wenn keine Anfrage läuft, wird eine neue gestartet.
    if (!pending) {
        pending = $.get("/bescheidbearbeitung/LoadChildren", {
            parentObjectId: li.data("object-id"),
            parentNodeType: nodeType
        }).then(function (html) {

            // Fügt HTML nur hinzu, wenn Inhalt vorhanden ist.
            if (html && html.trim() !== "") {
                li.append(html);
            }

            // Markiert den Knoten als geladen.
            li.data("loaded", true);

            return html;
        }).always(function () {

            // Entfernt die laufende Anfrage, sobald sie abgeschlossen ist.
            li.removeData("loadingPromise");
        });

        // Speichert die laufende Anfrage am Knoten.
        li.data("loadingPromise", pending);
    }

    // Hängt sich an die laufende Anfrage an (verhindert doppeltes Laden).
    pending.done(function () {
        callback && callback();
    });
}


                                                                                        // Click events //


//###############################################################################   Klick auf einen Baumtitel   ###############################################################################//
// Klick auf einen Baumtitel.
$(document).on("click", ".js-tree-node", function (e) {

    // Verhindert Standard-Navigation des Browsers.
    e.preventDefault();

    // Holt objectId aus dem DOM-Element.
    const objectId = $(this).data("object-id");

    // Holt nodeType aus dem DOM-Element.
    const nodeType = $(this).data("node-type");

    // Abbruch, wenn keine gültigen Daten vorhanden sind.
    if (!objectId || !nodeType) return;

    // Bestimmt den aktuellen Modus (Mitzeichnung oder Bescheidbearbeitung) basierend auf dem Container.
    const treeContainer = $(this).closest("#mitzeichnungTreeContainer, #bescheidTreeContainer");
    const mode = treeContainer.attr("id") === "mitzeichnungTreeContainer" ? "Mitzeichnung" : "Bescheidbearbeitung";
    $.get("/bescheidbearbeitung/set-access-mode?mode=" + mode, function () {
        // Knopf-UI synchron mit dem neuen Modus aktualisieren.
        currentSearchMode = mode;
        updateToggleUi();
    });

    // Hebt den aktiven Knoten im Baum hervor.
    setActiveNode(objectId);

    // Aktualisiert die Browser-URL ohne Reload.
    history.pushState(
        { objectId, nodeType },
        "",
        appUrl("bescheidbearbeitung?ObjectId=" + objectId)
    );


    // Aktualisiert den Zurück-Button.
    updateBackButton();

    // Lädt die passende Tabelle basierend auf dem Knotentyp.
    loadTableByNodeType(nodeType, objectId);

    // Lädt und rendert den aktuellen Pfad (Breadcrumb).
    $.get("/bescheidbearbeitung/get-node-path-with-node-type", {
        objectId: objectId,
        nodeType: nodeType
    }, function (path) {

        // Rendert den Pfad im UI.
        renderPath(path);
    });

    // Speichert den aktuellen Zustand (objectId, nodeType).
    saveState(objectId, nodeType);
});


//###############################################################################      Klick auf das Icon       ###############################################################################//
// Klick auf das Auf-/Zuklapp-Icon im Baum.
$(document).on("click", ".toggle-icon", function (e) {

    // Verhindert Standardverhalten des Browsers.
    e.preventDefault();

    // Stoppt das Event-Bubbling (kein Trigger auf Parent-Elemente).
    e.stopPropagation();

    // Holt den zugehörigen Baumknoten (li-Element).
    const li = $(this).closest(".tree-node");

    // Holt die objectId des Knotens.
    const objectId = li.data("object-id");

    // Holt das Icon-Element.
    const icon = $(this).find("i");

    // Abbruch, wenn keine gültige objectId vorhanden ist.
    if (!objectId) {
        return;
    }

    // Abbruch, wenn die Kinder gerade geladen werden (verhindert Doppelklick-Probleme).
    if (li.data("loadingPromise")) {
        return;
    }

    // Bestimmt den aktuellen Modus (Mitzeichnung oder Bescheidbearbeitung) basierend auf dem Container.
    const treeContainer = $(this).closest("#mitzeichnungTreeContainer, #bescheidTreeContainer");
    const mode = treeContainer.attr("id") === "mitzeichnungTreeContainer" ? "Mitzeichnung" : "Bescheidbearbeitung";
    $.get("/bescheidbearbeitung/set-access-mode?mode=" + mode, function () {
        currentSearchMode = mode;
        updateToggleUi();
    });

    // Knoten schließen inkl. aller Kindknoten.
    if (li.hasClass("open")) {

        li.removeClass("open");

        // Entfernt "open"-Status auch von allen untergeordneten Knoten.
        li.find(".tree-node.open").removeClass("open");

        // Setzt alle Icons im Unterbaum auf "geschlossen".
        li.find(".toggle-icon i")
            .removeClass("fa-folder-open")
            .addClass("fa-folder");

        // Setzt das aktuelle Icon auf "geschlossen".
        icon.removeClass("fa-folder-open")
            .addClass("fa-folder");

        return;
    }

    // Lädt die Kinder (nur einmal) und öffnet danach den Knoten.
    loadChildrenOnce(li, li.find(".js-tree-node").first().data("node-type"), function () {

        // Öffnet den Knoten.
        li.addClass("open");

        // Aktualisiert das Icon auf "offener Ordner".
        icon.removeClass("fa-folder")
            .addClass("fa-folder-open");
    });

});

//###############################################################################  Klick auf die Tabellenzeile  ###############################################################################//
// Doppelklick auf eine Tabellenzeile.
$(document).on("dblclick", ".js-table-node", function () {
    const objectId = $(this).data("object-id");
    const nodeType = $(this).data("node-type");
    // Abbruch, wenn der Knoten ein Dokument ist (keine Navigation).
    if (nodeType === "Dokument") return;
    navigateToNode(objectId, nodeType);
});
// Click auf den "Weiter"-Button in der Tabelle.
$(document).on("click", ".weiter-btn", function (event) {
    // Verhindert, dass der Klick auf die Zeile (dblclick-Handler) mit ausgelöst wird.
    event.stopPropagation();
    // Holt die übergeordnete Tabellenzeile.
    const $row = $(this).closest(".js-table-node");
    const objectId = $row.data("object-id");
    const nodeType = $row.data("node-type");
    navigateToNode(objectId, nodeType);
});
// Speichert die letzte Navigation (verhindert doppelte Klicks auf denselben Knoten).
let lastNavigation = { id: null, time: 0 };
// Führt die Navigation zu einem Knoten aus (Tabelle, Baum, URL, Modus).
function navigateToNode(objectId, nodeType) {

    // Abbruch, wenn keine gültige objectId vorhanden ist.
    if (!objectId) return;

    // Ignoriert einen erneuten Klick auf denselben Knoten innerhalb von 500 ms.
    const now = Date.now();
    if (lastNavigation.id === objectId && now - lastNavigation.time < 500) return;
    lastNavigation = { id: objectId, time: now };

    // Ziel-URL für Navigation.
    const targetUrl = appUrl("bescheidbearbeitung?ObjectId=" + objectId);

    // Wenn wir uns nicht auf der Bescheidbearbeitung-Seite befinden, wird die Seite neu geladen.
    if (!window.location.pathname.toLowerCase().startsWith(appUrl("bescheidbearbeitung").toLowerCase())) {
        window.location.href = targetUrl;
        return;
    }

    // Aktualisiert die Browser-URL ohne Reload (inkl. nodeType im State).
    history.pushState({ objectId, nodeType }, "", targetUrl);

    // Aktualisiert den Zurück-Button.
    updateBackButton();

    // Speichert den aktuellen Zustand (objectId, nodeType).
    saveState(objectId, nodeType);

    // Modus anhand des aktuell aktiven Baumknotens bestimmen und in der Session setzen.
    const detectedMode = detectModeFromDB(objectId, nodeType);
    if (detectedMode) {
        currentSearchMode = detectedMode;
        updateToggleUi();
    }

    // Lädt die passende Tabelle basierend auf dem Knotentyp.
    loadTableByNodeType(nodeType, objectId);

    // Öffnet den Baum bis zum ausgewählten Knoten.
    openTreeToNode(objectId, function () {

        // Hebt den aktiven Knoten im Baum hervor (erst nachdem der Baum aufgebaut wurde).
        setActiveNode(objectId);

        // Scrollt im Baum zum aktiven Knoten (erst nachdem der Knoten im DOM existiert).
        scrollToTreeNode(objectId);
    });
}


//###############################################################################          Back-Button          ###############################################################################//
// Event für Zurück/Vorwärts Navigation im Browser.
window.addEventListener("popstate", function () {
    // Holt die objectId aus der URL.
    const objectId = new URLSearchParams(window.location.search).get("ObjectId");
    // Wenn keine objectId vorhanden ist → Root laden.
    if (!objectId) {
        // Setzt den aktiven Knoten zurück.
        setActiveNode(null);
        // Lädt die Root-Tabelle (Kategorien).
        $.get("/bescheidbearbeitung/get-access-mode", function (mode) {
            if (mode === "Mitzeichnung") {
                $("#tableContainer").load("/bescheidbearbeitung/load-vorgange-mitzeichnung");
                
            } else {
                $("#tableContainer").load("/bescheidbearbeitung/load-kategorien");
            }
        });
        // Pfad zurücksetzen.
        renderPath([]);
        // Aktualisiert den Zurück-Button.
        updateBackButton();

        // Vorgang "V" taste ausschalten.  
        document.getElementById('btnEditV').style.display = 'none';

        return;
    }
    // Stellt den Baum wieder her.
    openTreeToNode(objectId);
    // Lädt die Tabelle basierend auf dem History-State.
    const state = history.state;
    if (!state || !state.nodeType) return;

    const detectedMode = detectModeFromDB(objectId, state.nodeType);

    console.log(state.nodeType)

    if (detectedMode) {
        currentSearchMode = detectedMode;
        updateToggleUi();
    }

    loadTableByNodeType(state.nodeType, objectId);
    // Aktualisiert den Pfad (Breadcrumb) mit objectId und nodeType aus dem State.
    $.get("/bescheidbearbeitung/get-node-path-with-node-type", { objectId, nodeType: state.nodeType }, function (path) {
        renderPath(path);
    });
    // Aktualisiert den Zurück-Button.
    updateBackButton();

    // Speichert den aktuellen Zustand (objectId, nodeType).
    saveState(objectId, state.nodeType);
});


                                                                                         // Helpers //


//###############################################################################     Back-Button Steuerung     ###############################################################################//
// Aktualisiert die Sichtbarkeit des Zurück-Buttons basierend auf der URL.
function updateBackButton() {

    // Holt die objectId aus der URL-Query.
    const objectId = new URLSearchParams(window.location.search).get("ObjectId");

    // Referenz auf den Back-Button.
    const btnBack = document.getElementById("btnBack");

    // Abbruch, wenn der Button nicht existiert.
    if (!btnBack) return;

    // Zeigt den Button und Divider nur an, wenn eine objectId vorhanden ist.
    const display = objectId ? "inline-block" : "none";
    btnBack.style.display = display;
    document.getElementById('btnDividerBack').style.display = objectId ? "flex" : "none";
}


//###############################################################################            Scroll             ###############################################################################//
// Scrollt zum angegebenen Baumknoten.
function scrollToTreeNode(objectId) {

    // Container des Baums.
    const $container = $('.nav-container');

    // Zielknoten im Baum.
    const $node = $('.tree-node[data-object-id="' + objectId + '"]');

    // Prüft, ob der Knoten existiert.
    if ($node.length) {

        // Berechnet die Position des Containers.
        const containerTop = $container.offset().top;

        // Berechnet die Position des Knotens.
        const nodeTop = $node.offset().top;

        // Berechnet die Scroll-Position mit Offset.
        const scrollPosition = $container.scrollTop() + (nodeTop - containerTop) - 100;

        // Animiertes Scrollen zum Knoten.
        $container.animate({
            scrollTop: scrollPosition
        }, 300);

    }
}


