(function () {
    // Basis-Route des SpaltenControllers (GET/POST/DELETE /spalten/{tableKey}).
    const base = "/spalten";

    // Kleiner Helper für alle drei HTTP-Methoden gegen den Spalten-Endpoint.
    async function request(method, tableKey, body) {
        return fetch(`${base}/${encodeURIComponent(tableKey)}`, {
            method,
            headers: body ? { "Content-Type": "application/json" } : {},
            body: body ? JSON.stringify(body) : undefined
        });
    }

    // Öffnet den "Spalten anpassen"-Dialog für eine bestimmte Tabelle (tableKey).
    // Wird direkt aus der View aufgerufen, z. B. onclick="openColumnDialog('Kategorien')".
    window.openColumnDialog = async function (tableKey) {

        // Aktuelle Spaltenkonfiguration (global + persönlich) vom Server holen.
        const res = await request("GET", tableKey);
        if (!res.ok) { alert("Spalten konnten nicht geladen werden."); return; }
        const cols = await res.json();

        // Dialog-Grundgerüst als natives <dialog>-Element aufbauen.
        const dlg = document.createElement("dialog");
        dlg.className = "column-dialog";
        dlg.innerHTML = `
            <h3>Spalten anpassen</h3>
            <ul class="column-list"></ul>
            <div class="column-dialog-actions">
                <button type="button" data-act="reset">Zurücksetzen</button>
                <button type="button" data-act="cancel">Abbrechen</button>
                <button type="button" data-act="save">Speichern</button>
            </div>`;
        const list = dlg.querySelector(".column-list");

        // Zeichnet die Spaltenliste anhand des aktuellen "cols"-Arrays neu.
        // Wird nach jeder Änderung (Checkbox, Reihenfolge) erneut aufgerufen.
        function render() {
            list.innerHTML = "";
            cols.forEach((c, i) => {
                const li = document.createElement("li");

                // Checkbox zum Ein-/Ausblenden der Spalte.
                const label = document.createElement("label");
                const cb = document.createElement("input");
                cb.type = "checkbox";
                cb.checked = c.isVisible;
                cb.onchange = () => { c.isVisible = cb.checked; };
                label.append(cb, " ", document.createTextNode(c.title));

                // Button zum Hochschieben (Reihenfolge nach oben).
                const up = document.createElement("button");
                up.type = "button"; up.textContent = "▲"; up.disabled = i === 0;
                up.onclick = () => { [cols[i - 1], cols[i]] = [cols[i], cols[i - 1]]; render(); };

                // Button zum Runterschieben (Reihenfolge nach unten).
                const down = document.createElement("button");
                down.type = "button"; down.textContent = "▼"; down.disabled = i === cols.length - 1;
                down.onclick = () => { [cols[i + 1], cols[i]] = [cols[i], cols[i + 1]]; render(); };

                li.append(label, up, down);
                list.append(li);
            });
        }
        render();

        // Schließt den Dialog und entfernt ihn wieder aus dem DOM.
        function close() { dlg.close(); dlg.remove(); }

        // Nach einer Änderung reicht ein Partial-Reload der Tabelle nicht mehr aus,
        // da auch andere Teile der Seite (z. B. das Ansicht-Menü) den neuen Stand
        // widerspiegeln sollen. Deshalb die ganze Seite neu laden.
        function reloadPage() { location.reload(); }

        // Klick-Handler für die drei Aktions-Buttons im Dialog.
        dlg.addEventListener("click", async e => {
            const act = e.target.dataset?.act;
            if (!act) return;

            // Abbrechen: nichts speichern, Dialog einfach schließen.
            if (act === "cancel") return close();

            // Speichern: aktuelle Reihenfolge + Sichtbarkeit an den Server schicken.
            if (act === "save") {
                // Mindestens eine sichtbare Spalte ist Pflicht, sonst wäre die Tabelle leer.
                if (!cols.some(c => c.isVisible)) { await showAlert("Mindestens eine Spalte muss sichtbar sein.", "warning"); return; }

                const r = await request("POST", tableKey,
                    cols.map(c => ({ columnKey: c.columnKey, isVisible: c.isVisible })));
                if (!r.ok) { await showAlert("Speichern fehlgeschlagen.", "error"); return; }
                close(); reloadPage();
            }

            // Zurücksetzen: persönliche Einstellungen löschen, Standard aus der DB greift wieder.
            if (act === "reset") {
                const r = await request("DELETE", tableKey);
                if (!r.ok) { await showAlert("Zurücksetzen fehlgeschlagen.", "error"); return; }
                close(); reloadPage();
            }
        });

        // Dialog anzeigen.
        document.body.append(dlg);
        dlg.showModal();
    };
})();