const ablaufdatenEndpoint = '/bescheidbearbeitung/load-ablaufdaten'; // Server-Endpunkt.

// Container-Getter.
function ablaufdatenContainer() {
    return document.getElementById('ablaufdatenTableContainer');
}

// Hilfsfunktion: Feldwert auslesen. Parameter: id — Element-ID, fallback — Standardwert.
function aFeldWert(id, fallback = '') {
    return document.getElementById(id)?.value ?? fallback;
}

// Aktuelle Filterkriterien als Objekt zusammenbauen.
function aFilterWerte() {
    return {
        EintragAmVom: aFeldWert('aSearchEintragAmVom'),
        EintragVon: aFeldWert('aSearchEintragVon'),
        AktionAm: aFeldWert('aSearchAktionAm'),
        AktionTyp: aFeldWert('aSearchAktionTyp'),
        Bemerkung: aFeldWert('aSearchBemerkung'),
        pageSize: aFeldWert('aPageSizeSelect', 10)
    };
}

// Tabelle per AJAX nachladen. Parameter: params — Objekt mit Filterkriterien + Paginierung.
async function ablaufdatenTabelleLaden(params) {
    const query = new URLSearchParams(params);
    try {
        const response = await fetch(`${ablaufdatenEndpoint}?${query}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden der Ablaufdaten.", "error");
            return;
        }
        const html = await response.text();
        // Neuen HTML-Inhalt in den Container einfügen.
        ablaufdatenContainer().innerHTML = html;
        // Ereignis-Listener für neue DOM-Elemente neu binden.
        aListenersBinden();
    } catch {
        await showAlert("Netzwerkfehler beim Laden der Ablaufdaten.", "error");
    }
}

// Seite laden.
window.loadAblaufdatenPage = function (page = 1) {
    ablaufdatenTabelleLaden({
        ...aFilterWerte(),
        page,
        aktenzeichen: aFeldWert('modalAktenzeichen')
    });
};

// Spalte sortieren.
window.sortAblaufdaten = function (spalte) {
    const aktueleSpalte = aFeldWert('aSortColumn');
    const aktuelRichtung = aFeldWert('aSortDirection', 'asc');

    // Wenn gleiche Spalte: Richtung umkehren, sonst aufsteigend starten.
    const neueRichtung = (aktueleSpalte === spalte && aktuelRichtung === 'asc')
        ? 'desc'
        : 'asc';

    ablaufdatenTabelleLaden({ ...aFilterWerte(), aktenzeichen: aFeldWert('modalAktenzeichen'), page: 1, sortColumn: spalte, sortDirection: neueRichtung });
};

// Ereignis-Listener binden.
function aListenersBinden() {
    // IDs aller Suchfelder.
    const filterIDs = [
        '#aSearchEintragAmVom', '#aSearchEintragVon',
        '#aSearchAktionAm', '#aSearchAktionTyp', '#aSearchBemerkung'
    ];

    // Enter-Taste in Suchfeldern → Seite 1 neu laden.
    document.querySelectorAll(filterIDs.join(','))
        .forEach(inp => inp?.addEventListener('keypress', e => {
            if (e.key === 'Enter') window.loadAblaufdatenPage(1);
        }));

    // Datumsfelder und Select: sofort bei Änderung → Seite 1 neu laden.
    const instantIDs = ['#aSearchEintragAmVom', '#aSearchAktionAm', '#aSearchAktionTyp'];
    document.querySelectorAll(instantIDs.join(','))
        .forEach(inp => inp?.addEventListener('change', () => window.loadAblaufdatenPage(1)));

    // Seitengröße geändert → Seite 1 neu laden.
    document.getElementById('aPageSizeSelect')
        ?.addEventListener('change', () => window.loadAblaufdatenPage(1));

    // Direkte Seiteneingabe (Zahl in Eingabefeld).
    document.getElementById('aCurrentPage')
        ?.addEventListener('input', e => {
            const seite = parseInt(e.target.value, 10);
            const max = parseInt(e.target.max, 10);

            if (!isNaN(seite)) {
                // Grenzen einhalten.
                if (seite < 1) return (e.target.value = 1);
                if (seite > max) return (e.target.value = max);
                window.loadAblaufdatenPage(seite);
            }
        });

    // Reset-Button: alle Filter leeren und Seite 1 laden.
    document.getElementById('aResetBtn')
        ?.addEventListener('click', () => {
            document.querySelectorAll(filterIDs.join(','))
                .forEach(i => i.value = '');
            const sel = document.getElementById('aPageSizeSelect');
            if (sel) sel.value = '10';
            window.loadAblaufdatenPage(1);
        });
}

// Ablaufdaten-Tab initialisieren. Wird beim Öffnen des Modals aufgerufen.
function initAblaufdaten() {
    // Tabelle automatisch laden, wenn Tab "Ablaufdaten" geöffnet wird.
    document.querySelectorAll('.modal-tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            if (btn.textContent.trim().includes('Ablaufdaten')) {
                window.loadAblaufdatenPage(1);
            }
        });
    });

    // Listener für initial geladene Elemente binden.
    aListenersBinden();
}


// CRUD.
// Neue leere Eingabezeile oben in die Tabelle einfügen.
window.addAblaufdatenRow = function () {
    const tbody = document.getElementById('ablaufdatenTbody');
    const leerZeile = document.getElementById('aEmptyRow');
    if (leerZeile) leerZeile.remove();

    // Optionen aus bestehendem Select (Filter) übernehmen.
    const optionen = Array.from(
        document.querySelectorAll('#aSearchAktionTyp option')
    )
        .filter(o => o.value !== '')
        .map(o => `<option value="${o.value}">${o.text}</option>`)
        .join('');

    const tr = document.createElement('tr');
    tr.className = 'dark-search a-row-edit a-row-new';
    tr.innerHTML = `
        <td class="a-actions">
            <button class="p-btn p-btn-success" onclick="saveNewAblaufdaten(this)" title="Speichern">
                <i class="fa fa-check"></i>
            </button>
            <button class="p-btn" onclick="this.closest('tr').remove()" title="Abbrechen">
                <i class="fa fa-xmark"></i>
            </button>
        </td>
        <td><input  class="modal-input a-edit-EintragAmVom" type="date" required /></td>
        <td><input  class="modal-input a-edit-EintragVon"   placeholder="Eingetragen von" /></td>
        <td><input  class="modal-input a-edit-AktionAm"     type="date" /></td>
        <td>
            <select class="modal-input a-edit-AktionTyp">
                <option value="">— Bitte wählen —</option>
                ${optionen}
            </select>
        </td>
        <td><input  class="modal-input a-edit-Bemerkung"    placeholder="Bemerkung" /></td>
    `;

    tbody.insertBefore(tr, tbody.firstChild);
    tr.querySelector('input').focus();
};

// Neue Ablaufdaten-Zeile speichern.
window.saveNewAblaufdaten = async function (btn) {
    const tr = btn.closest('tr');

    const daten = {
        id: 0,
        aktenzeichen: document.getElementById('modalAktenzeichen').value,
        eintragAmVom: tr.querySelector('.a-edit-EintragAmVom').value || null,
        eintragVon: tr.querySelector('.a-edit-EintragVon').value,
        aktionAm: tr.querySelector('.a-edit-AktionAm').value || null,
        aktionTyp: tr.querySelector('.a-edit-AktionTyp').value,
        bemerkung: tr.querySelector('.a-edit-Bemerkung').value,
    };

    const check = validiereAblaufdatenDaten(daten);
    if (!check.valid) {
        await showAlert(check.message, 'error');
        return;
    }

    try {
        const r = await fetch('/bescheidbearbeitung/save-ablaufdaten', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(daten)
        });
        const result = await r.json();

        if (result.success) {
            window.loadAblaufdatenPage(1);
        } else {
            await showAlert('Fehler beim Speichern: ' + result.message, 'error');
        }
    } catch (err) {
        await showAlert('Verbindungsfehler: ' + err.message, 'error');
    }
};


// Validierung.
const ABLAUFDATEN_MAX_LENGTHS = {
    eintragVon: 255,
    aktionTyp: 100
};

const ABLAUFDATEN_FELD_LABELS = {
    eintragAmVom: 'Eingetragen am',
    eintragVon: 'Eingetragen von',
    aktionTyp: 'Aktionstyp',
    bemerkung: 'Bemerkung'
};

// Prüft ein einzelnes Ablaufdaten-Objekt. Gibt { valid, message } zurück.
function validiereAblaufdatenDaten(daten) {
    // Pflichtfelder laut Datenbankschema.
    if (!daten.eintragAmVom) {
        return { valid: false, message: `Bitte füllen Sie das Feld "${ABLAUFDATEN_FELD_LABELS.eintragAmVom}" aus.` };
    }

    if (!daten.eintragVon || !daten.eintragVon.trim()) {
        return { valid: false, message: `Bitte füllen Sie das Feld "${ABLAUFDATEN_FELD_LABELS.eintragVon}" aus.` };
    }

    // Maximale Länge prüfen.
    for (const [feld, maxLength] of Object.entries(ABLAUFDATEN_MAX_LENGTHS)) {
        const wert = daten[feld];
        if (wert && wert.trim().length > maxLength) {
            return {
                valid: false,
                message: `Feld "${ABLAUFDATEN_FELD_LABELS[feld]}" darf maximal ${maxLength} Zeichen lang sein.`
            };
        }
    }

    // Datumslogik: "Aktion am" darf nicht vor "Eingetragen am" liegen.
    if (daten.aktionAm && new Date(daten.aktionAm) < new Date(daten.eintragAmVom)) {
        return { valid: false, message: '"Aktion am" darf nicht vor "Eingetragen am" liegen.' };
    }

    return { valid: true, message: '' };
}