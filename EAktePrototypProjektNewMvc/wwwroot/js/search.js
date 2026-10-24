// Search for types - open page.
$(document).on("click", "#searchType", function () {

    $("#tableContainer").load(
        "/bescheidbearbeitung/search-results",
        function () {
            searchModule.init(); 
        }
    );
});

// Stellt sicher, dass das globale searchModule existiert.
// Falls es bereits definiert ist, wird es nicht überschrieben, sonst wird ein neues Objekt erstellt.
window.searchModule = window.searchModule || {};

(function (module) {

    // Definiert die Suchfelder für jeden Typ (Akte, Vorgang, Ordner).
    module.fieldMap = {
        Akte: [
            { label: "Zulassungsnummer..", name: "Zulassungsnummer" },
            { label: "Hauptgruppenummer..", name: "HauptgruppeNummer" },
            { label: "NebengruppeNummer..", name: "NebengruppeNummer" },
            { label: "Zustand..", name: "Zustand" },
            { label: "Erzeugt Von..", name: "ErzeugtVon" },
            { label: "Erzeugt Am..", type: "daterange", nameVon: "ErzeugtAmVon", nameBis: "ErzeugtAmBis" },
            { label: "Geandert Von..", name: "GeandertVon" },
            { label: "Geandert Am..", type: "daterange", nameVon: "GeandertAmVon", nameBis: "GeandertAmBis" },
            { label: "Objekt-Id..", name: "ObjektId" }
        ],
        Vorgang: [
            { label: "Aktenzeichen..", name: "Aktenzeichen" },
            { label: "Zulassungsnummer..", name: "Zulassungsnummer" },
            { label: "UserId..", name: "UserId" },
            { label: "Abteilung..", name: "Abteilung" },
            { label: "Stelle..", name: "Stelle" },
            { label: "Status..", name: "Status" },
            { label: "Gegenstand..", name: "Gegenstand" },
            { label: "Betreff..", name: "Betreff" },
            { label: "Bemerkung..", name: "Bemerkung" },
            { label: "Erzeugt Am..", type: "daterange", nameVon: "ErzeugtAmVon", nameBis: "ErzeugtAmBis" },
            { label: "Objekt-Id..", name: "ObjektId" }
        ],
        Ordner: [
            { label: "Name..", name: "Name" },
            { label: "Type..", name: "Type" },
            { label: "Aktenzeichen..", name: "Aktenzeichen" },
            { label: "Hauptgruppe..", name: "Hauptsachgebiet" },
            { label: "Nebengruppe..", name: "Sachgebiet" },
            { label: "Bemerkungen..", name: "Bemerkungen" },
            { label: "OwnerName..", name: "OwnerName" },
            { label: "Status..", name: "Status" },
            { label: "Erzeugt Am..", type: "daterange", nameVon: "CreationDateVon", nameBis: "CreationDateBis" },
            { label: "Objekt-Id..", name: "ObjektId" }
        ],
        Dokument: [
            { label: "Name..", name: "Name" },
            { label: "Ersteller..", name: "CreatorName" },
            { label: "Erzeugt Am..", type: "daterange", nameVon: "CreationDateVon", nameBis: "CreationDateBis" },
            { label: "Geändert Am..", type: "daterange", nameVon: "ModifyDateVon", nameBis: "ModifyDateBis" }
        ]
    };



    module.renderSortHeader = function (type, sortCol, sortDir) {

        const sortRow = document.getElementById("sortHeaderRow");
        if (!sortRow) return;

        const arrow = (col) => {
            if (sortCol !== col) return "";
            const cls = sortDir === "asc" ? "sort-asc" : "sort-desc";
            const sym = sortDir === "asc" ? "▲" : "▼";
            return ` <span class="sort-arrow ${cls}">${sym}</span>`;
        };

        const columnsMap = {
            Akte: [
                { label: "Zulassungsnummer", name: "Zulassungsnummer" },
                { label: "Hauptgruppenummer", name: "HauptgruppeNummer" },
                { label: "NebengruppeNummer", name: "NebengruppeNummer" },
                { label: "Zustand", name: "Zustand" },
                { label: "Erzeugt Von", name: "ErzeugtVon" },
                { label: "Erzeugt Am", name: "ErzeugtAm" },
                { label: "Geandert Von", name: "GeandertVon" },
                { label: "Geandert Am", name: "GeandertAm" },
                { label: "Objekt-Id", name: "ObjektId" }
            ],
            Vorgang: [
                { label: "Aktenzeichen", name: "Aktenzeichen" },
                { label: "Zulassungsnummer", name: "Zulassungsnummer" },
                { label: "UserId", name: "UserId" },
                { label: "Abteilung", name: "Abteilung" },
                { label: "Stelle", name: "Stelle" },
                { label: "Status", name: "Status" },
                { label: "Gegenstand", name: "Gegenstand" },
                { label: "Betreff", name: "Betreff" },
                { label: "Bemerkung", name: "Bemerkung" },
                { label: "Erzeugt Am", name: "ErzeugtAm" },
                { label: "Objekt-Id", name: "ObjektId" }
            ],
            Ordner: [
                { label: "Name", name: "Name" },
                { label: "Type", name: "Type" },
                { label: "Aktenzeichen", name: "Aktenzeichen" },
                { label: "Hauptgruppe", name: "Hauptsachgebiet" },
                { label: "Nebengruppe", name: "Sachgebiet" },
                { label: "Bemerkungen", name: "Bemerkungen" },
                { label: "OwnerName", name: "OwnerName" },
                { label: "Status", name: "Status" },
                { label: "Erzeugt Am", name: "CreationDate" },
                { label: "Objekt-Id", name: "ObjektId" }
            ],
            Dokument: [
                { label: "Name", name: "Name" },
                { label: "Ersteller", name: "CreatorName" },
                { label: "Erzeugt Am", name: "CreationDate" },
                { label: "Geändert Am", name: "ModifyDate" }
            ]
        };

        const columns = columnsMap[type] || [];

        sortRow.innerHTML =
            `<th>Typ</th>` +
            columns.map(c =>
                `<th onclick="sortTableSearch('${c.name}')" style="cursor:pointer;">
              ${c.label}${arrow(c.name)}
            </th>`
                    ).join("") +
                    `<th>Aktionen</th>`;
    };

    // Initialisiert die Suchseite:
    // setzt den Typ, rendert Felder, stellt gespeicherte Werte wieder her und lädt Daten.
    module.init = function (forceType = null) {

        const select = document.getElementById("objectTypeSelect");
        if (!select) return;

        let type = forceType                              // 1. из resetFilters
            || (module.savedState && module.savedState.Type) // 2. из сессии
            || select.value                               // 3. из DOM
            || "Akte";                                    // 4. fallback

        select.value = type;

        select.addEventListener("change", function () {
            module.renderHeader(this.value);
        });

        module.renderHeader(type);
        module.restoreInputs();

        if (module.savedState && module.savedState.Data) {
            module.loadSearch(module.savedState.Page || 1);
        }
    };

    // Rendert dynamisch die Suchfelder basierend auf dem gewählten Typ.
    module.renderHeader = function (type) {

        const row = document.getElementById("searchHeaderRow");
        const actionTh = document.getElementById("actionButtons");

        const sortRow = document.getElementById("sortHeaderRow");
        if (sortRow) sortRow.innerHTML = "";

        document.getElementById("tableSearchContainer").innerHTML = "";

        row.querySelectorAll(".dynamic-field").forEach(e => e.remove());

        if (!type || !module.fieldMap[type]) return;

        module.fieldMap[type].forEach(f => {

            const th = document.createElement("th");
            th.className = "dynamic-field";

            if (f.type === "daterange") {
                th.classList.add("daterange-field");

                const labelVon = document.createElement("span");
                labelVon.className = "von-bis";
                labelVon.textContent = "von:";

                const inputVon = document.createElement("input");
                inputVon.type = "date";
                inputVon.name = f.nameVon;
                inputVon.title = f.label + " (von)";
                inputVon.className = "header-search-input daterange-input";

                const labelBis = document.createElement("span");
                labelBis.className = "von-bis";
                labelBis.textContent = "bis:";

                const inputBis = document.createElement("input");
                inputBis.type = "date";
                inputBis.name = f.nameBis;
                inputBis.title = f.label + " (bis)";
                inputBis.className = "header-search-input daterange-input";

                th.appendChild(labelVon);
                th.appendChild(inputVon);
                th.appendChild(labelBis);
                th.appendChild(inputBis);
            } else {
                const input = document.createElement("input");
                input.placeholder = f.label;
                input.name = f.name;
                input.className = "header-search-input";
                th.appendChild(input);
            }

            row.insertBefore(th, actionTh);
        });
    };

    // Stellt die gespeicherten Suchwerte aus der Session wieder her.
    module.restoreInputs = function () {

        if (!module.savedState || !module.savedState.Data) return;

        const inputs = document.querySelectorAll(".header-search-input");

        inputs.forEach(input => {
            if (module.savedState.Data[input.name] != null) {
                input.value = module.savedState.Data[input.name];
            }
        });

        if (module.savedState.GlobalPageSize) {
            document.getElementById("globalPageSizeSelect").value = module.savedState.GlobalPageSize;
        }
    };

    // Führt die Suche aus:
    // sammelt Filter, baut URL und lädt Ergebnisse per fetch.
    module.loadSearch = function (page = 1) {

        const type = document.getElementById("objectTypeSelect").value;
        if (!type) return;

        const inputs = document.querySelectorAll(".header-search-input");
        const params = new URLSearchParams();

        inputs.forEach(i => {
            if (i.value && i.name)
                params.append(i.name, i.value);
        });

        const globalPageSize = document.getElementById("globalPageSizeSelect").value;

        // Erst pageSizeSelect prüfen, dann savedState, dann Default 10.
        let pageSize = 10;
        const pageSizeElement = document.getElementById("pageSizeSelect");
        if (pageSizeElement) {
            pageSize = pageSizeElement.value;
        }else if (module.savedState && module.savedState.PageSize) {
            pageSize = String(module.savedState.PageSize); 
        }

        params.append("globalPageSize", globalPageSize);
        params.append("pageSize", pageSize);
        params.append("page", page);

        // Сортировка: из временного стейта (после клика) или из сохранённого Session-стейта.
        const sortCol = module._sortColumn
            || (module.savedState && module.savedState.SortColumn)
            || "ErzeugtAm";
        const sortDir = module._sortDir
            || (module.savedState && module.savedState.SortDirection)
            || "asc";

        params.append("SortColumn", sortCol);
        params.append("SortDirection", sortDir);

        const url = `/bescheidbearbeitung/load-search-${type.toLowerCase()}?${params.toString()}`;

        fetch(url)
            .then(r => r.text())
            .then(html => {
                document.getElementById("tableSearchContainer").innerHTML = html;

                const meta = document.querySelector("[data-sort-header]");
                if (meta) {
                    const sortCol = meta.dataset.sortColumn || "";
                    const sortDir = meta.dataset.sortDirection || "asc";
                    const type = meta.dataset.sortHeader;

                    searchModule.renderSortHeader(type, sortCol, sortDir);
                }
            });
    };


    module.sortTableSearch = function (col) {
        // Читаем текущий стейт из скрытой мета-строки (data-sort-header).
        const meta = document.querySelector("[data-sort-header]");
        let currentCol = meta ? meta.dataset.sortColumn : "";
        let currentDir = meta ? meta.dataset.sortDirection : "asc";

        // Toggle: та же колонка — меняем направление, другая — сбрасываем на asc.
        const newDir = (currentCol === col && currentDir === "asc") ? "desc" : "asc";

        // Сохраняем в module, чтобы loadSearch мог дочитать.
        module._sortColumn = col;
        module._sortDir = newDir;

        module.loadSearch(1);
    };

    // Setzt alle Filter zurück und lädt Standardzustand (Akte).
    module.resetFilters = function () {
        const select = document.getElementById("objectTypeSelect");
        if (!select) return;

        const currentType = select.value; // ← запоминаем ДО того как DOM пересоздаётся

        module.savedState = null;

        fetch("/bescheidbearbeitung/reset-search")
            .then(r => r.text())
            .then(html => {
                document.getElementById("tableContainer").innerHTML = html;

                searchModule.init(currentType); 
            });
    };

})(window.searchModule);

window.sortTableSearch = function (col) {
    searchModule.sortTableSearch(col);
};

// Wird von der Pagination aufgerufen und lädt eine bestimmte Seite neu.
window.loadPageS = function (page) {
    searchModule.loadSearch(page);
};






