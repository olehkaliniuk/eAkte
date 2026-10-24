const kontakteEndpoint = '/bescheidbearbeitung/load-kontakte'; // Server-Endpunkt.

// Modus aus dem DOM lesen (wird vom Razor-View ins HTML geschrieben).
function isCreateModus() {
    return document.getElementById('btnSaveAll') !== null;
}

// Container-Getter.
function kontakteContainer() {
    return document.getElementById('kontakteTableContainer');
}

// Hilfsfunktion: Feldwert auslesen. Parameter: id — Element-ID, fallback — Standardwert.
function feldWert(id, fallback = '') {
    return document.getElementById(id)?.value ?? fallback;
}

// Aktuelle Filterkriterien als Objekt zusammenbauen.
function filterWerte() {
    return {
        Anrede: feldWert('kSearchAnrede'),
        Vorname: feldWert('kSearchVorname'),
        Name: feldWert('kSearchName'),
        Email: feldWert('kSearchEmail'),
        EmailAnzeigename: feldWert('kSearchEmailAnzeigename'),
        EmailAn: feldWert('kSearchEmailAn'),
        Telefon: feldWert('kSearchTelefon'),
        Fax: feldWert('kSearchFax'),
        pageSize: feldWert('kPageSizeSelect', 10)
    };
}

// Tabelle per AJAX nachladen. Parameter: params — Objekt mit Filterkriterien + Paginierung.
async function tabelleLaden(params) {
    const query = new URLSearchParams(params);
    try {
        const response = await fetch(`${kontakteEndpoint}?${query}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden der Kontakte.", "error");
            return;
        }
        const html = await response.text();
        // Neuen HTML-Inhalt in den Container einfügen.
        kontakteContainer().innerHTML = html;
        // Ereignis-Listener für neue DOM-Elemente neu binden.
        listenersBinden();
    } catch {
        await showAlert("Netzwerkfehler beim Laden der Kontakte.", "error");
    }
}

// Seite laden.
window.loadKontaktePage = function (page = 1) {
    tabelleLaden({
        ...filterWerte(),
        page,
        aktenzeichen: feldWert('modalAktenzeichen')
    });
};

// Spalte sortieren.
window.sortKontakte = function (spalte) {
    const aktueleSpalte = feldWert('kSortColumn');
    const aktuelRichtung = feldWert('kSortDirection', 'asc');

    // Wenn gleiche Spalte: Richtung umkehren, sonst aufsteigend starten.
    const neueRichtung = (aktueleSpalte === spalte && aktuelRichtung === 'asc')
        ? 'desc'
        : 'asc';

    tabelleLaden({ ...filterWerte(), aktenzeichen: feldWert('modalAktenzeichen'), page: 1, sortColumn: spalte, sortDirection: neueRichtung });
};

// Ereignis-Listener binden.
function listenersBinden() {
    // IDs aller Suchfelder.
    const filterIDs = [
        '#kSearchAnrede', '#kSearchVorname', '#kSearchName',
        '#kSearchEmail', '#kSearchEmailAnzeigename',
        '#kSearchEmailAn', '#kSearchTelefon', '#kSearchFax'
    ];

    // Enter-Taste in Suchfeldern → Seite 1 neu laden.
    document.querySelectorAll(filterIDs.join(','))
        .forEach(inp => inp?.addEventListener('keypress', e => {
            if (e.key === 'Enter') window.loadKontaktePage(1);
        }));

    // Seitengröße geändert → Seite 1 neu laden.
    document.getElementById('kPageSizeSelect')
        ?.addEventListener('change', () => window.loadKontaktePage(1));

    // Direkte Seiteneingabe (Zahl in Eingabefeld).
    document.getElementById('kCurrentPage')
        ?.addEventListener('input', e => {
            const seite = parseInt(e.target.value, 10);
            const max = parseInt(e.target.max, 10);

            if (!isNaN(seite)) {
                // Grenzen einhalten.
                if (seite < 1) return (e.target.value = 1);
                if (seite > max) return (e.target.value = max);
                window.loadKontaktePage(seite);
            }
        });

    // Reset-Button: alle Filter leeren und Seite 1 laden.
    document.getElementById('kResetBtn')
        ?.addEventListener('click', () => {
            document.querySelectorAll(filterIDs.join(','))
                .forEach(i => i.value = '');
            const sel = document.getElementById('kPageSizeSelect');
            if (sel) sel.value = '10';
            window.loadKontaktePage(1);
        });
}

// Kontakte-Tab initialisieren. Wird beim Öffnen des Modals aufgerufen.
function initKontakte() {
    // Tabelle automatisch laden, wenn Tab "Kontakte" geöffnet wird.
    document.querySelectorAll('.modal-tab-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            if (btn.textContent.trim().includes('Kontakte')) {
                window.loadKontaktePage(1);
            }
        });
    });

    // Listener für initial geladene Elemente binden.
    listenersBinden();
}


// CRUD — Zeilen bearbeiten, speichern, löschen.
window.editKontakteRow = function (btn) {
    const anzeigeZeile = btn.closest('tr.k-row-view');
    const id = anzeigeZeile.dataset.id;
    const bearbeitungsZeile = document.querySelector(`tr.k-row-edit[data-id="${id}"]`);

    anzeigeZeile.style.display = 'none'; // Anzeige ausblenden.
    bearbeitungsZeile.style.display = '';     // Eingabe einblenden.
};

// Bearbeitung abbrechen.
window.cancelKontakteRow = function (btn) {
    const bearbeitungsZeile = btn.closest('tr.k-row-edit');
    const id = bearbeitungsZeile.dataset.id;
    const anzeigeZeile = document.querySelector(`tr.k-row-view[data-id="${id}"]`);

    bearbeitungsZeile.style.display = 'none'; // Eingabe ausblenden.
    anzeigeZeile.style.display = '';     // Anzeige einblenden.
};

// Geänderte Zeile speichern.
window.saveKontakteRow = async function (btn, id) {
    const bearbeitungsZeile = btn.closest('tr.k-row-edit');
    const daten = {
        id: id,
        anrede: bearbeitungsZeile.querySelector('.k-edit-Anrede').value,
        vorname: bearbeitungsZeile.querySelector('.k-edit-Vorname').value,
        name: bearbeitungsZeile.querySelector('.k-edit-Name').value,
        email: bearbeitungsZeile.querySelector('.k-edit-Email').value,
        emailAnzeigename: bearbeitungsZeile.querySelector('.k-edit-EmailAnzeigename').value,
        emailAn: bearbeitungsZeile.querySelector('.k-edit-EmailAn').value,
        telefon: bearbeitungsZeile.querySelector('.k-edit-Telefon').value,
        fax: bearbeitungsZeile.querySelector('.k-edit-Fax').value,
    };


    // Validierung der Eingaben.
    const check = validiereKontaktDaten(daten);
    if (!check.valid) {
        await showAlert(check.message, 'error');
        return;
    }

    try {
        const r = await fetch('/bescheidbearbeitung/save-kontakt', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(daten)
        });
        const result = await r.json();

        if (result.success) {
            // Anzeige-Zeile direkt aktualisieren (kein Seiten-Reload nötig).
            const anzeigeZeile = document.querySelector(`tr.k-row-view[data-id="${id}"]`);
            const zellen = anzeigeZeile.querySelectorAll('td');
            zellen[1].textContent = daten.anrede;
            zellen[2].textContent = daten.vorname;
            zellen[3].textContent = daten.name;
            zellen[4].textContent = daten.email;
            zellen[5].textContent = daten.emailAnzeigename;
            zellen[6].textContent = daten.emailAn;
            zellen[7].textContent = daten.telefon;
            zellen[8].textContent = daten.fax;
            bearbeitungsZeile.style.display = 'none';
            anzeigeZeile.style.display = '';
        } else {
            await showAlert('Fehler beim Speichern: ' + result.message, 'error');
        }
    } catch (err) {
        await showAlert('Verbindungsfehler: ' + err.message, 'error');
    }
};

// Kontakt-Eintrag löschen.
window.deleteKontakte = async function (id) {
    // Sicherheitsabfrage vor dem Löschen.
    const ok = await showConfirm('Eintrag wirklich löschen?');
    if (!ok) return;

    try {
        const r = await fetch(`/bescheidbearbeitung/delete-kontakt/${id}`, { method: 'DELETE' });
        const result = await r.json();

        if (result.success) {
            // Beide Zeilen (Anzeige + Bearbeitung) aus dem DOM entfernen.
            document.querySelector(`tr.k-row-view[data-id="${id}"]`)?.remove();
            document.querySelector(`tr.k-row-edit[data-id="${id}"]`)?.remove();

            // Prüfen ob noch Zeilen auf dieser Seite vorhanden sind.
            const tbody = document.getElementById('kontakteTbody');
            const verbleibend = tbody.querySelectorAll('tr.k-row-view').length;
            const aktuelleSeite = parseInt(
                document.getElementById('kCurrentPage')?.value || '1', 10
            );

            if (verbleibend === 0) {
                // Keine Einträge mehr → eine Seite zurück (mindestens Seite 1).
                window.loadKontaktePage(Math.max(1, aktuelleSeite - 1));
            }
            // Sonst: DOM bereits aktuell, kein Reload nötig.

        } else {
            await showAlert('Fehler beim Löschen: ' + result.message, 'error');
        }
    } catch (err) {
        await showAlert('Verbindungsfehler: ' + err.message, 'error');
    }
};

// Neue leere Eingabezeile oben in die Tabelle einfügen.
window.addKontakteRow = function (prefill = {}) {
    const tbody = document.getElementById('kontakteTbody');
    const leerZeile = document.getElementById('kEmptyRow');
    if (leerZeile) leerZeile.remove();

    const tr = document.createElement('tr');
    tr.className = 'dark-search k-row-edit k-row-new';
    tr.innerHTML = `
        <td class="k-actions">
            <button class="p-btn p-btn-success" onclick="saveNewKontakte(this)" title="Speichern">
                <i class="fa fa-check"></i>
            </button>
            <button class="p-btn" onclick="this.closest('tr').remove()" title="Abbrechen">
                <i class="fa fa-xmark"></i>
            </button>
        </td>
        <td><input class="modal-input k-edit-Anrede"           placeholder="Anrede"     value="${escapeHtml(prefill.anrede)}" /></td>
        <td><input class="modal-input k-edit-Vorname"          placeholder="Vorname"    value="${escapeHtml(prefill.vorname)}" /></td>
        <td><input class="modal-input k-edit-Name"             placeholder="Name"       value="${escapeHtml(prefill.name)}" /></td>
        <td><input class="modal-input k-edit-Email"            placeholder="E-Mail"     value="${escapeHtml(prefill.email)}" /></td>
        <td><input class="modal-input k-edit-EmailAnzeigename" placeholder="Anzeigename" value="${escapeHtml(prefill.emailAnzeigename)}" /></td>
        <td><input class="modal-input k-edit-EmailAn"          placeholder="E-Mail an"  value="${escapeHtml(prefill.emailAn)}" /></td>
        <td><input class="modal-input k-edit-Telefon"          placeholder="Telefon"    value="${escapeHtml(prefill.telefon)}" /></td>
        <td><input class="modal-input k-edit-Fax"              placeholder="Fax"        value="${escapeHtml(prefill.fax)}" /></td>
    `;

    tbody.insertBefore(tr, tbody.firstChild);
    tr.querySelector('input').focus();
};

window.saveNewKontakte = async function (btn) {
    const tr = btn.closest('tr');

    // ── CREATE-MODUS: nur im DOM behalten ──────────────────────────────
    if (isCreateModus()) {
        const daten = _neueKontaktDaten(tr);

        // Validierung der Eingaben.
        const check = validiereKontaktDaten(daten);
        if (!check.valid) {
            await showAlert(check.message, 'error');
            return;
        }

        // Aktions-Zelle durch reine Anzeige-Buttons ersetzen.
        const aktionsZelle = tr.querySelector('.k-actions');
        aktionsZelle.innerHTML = `
            <button class="p-btn" onclick="editNewKontakteRow(this)" title="Bearbeiten">
                <i class="fa fa-pencil"></i>
            </button>
            <button class="p-btn p-btn-danger" onclick="this.closest('tr').remove()" title="Löschen">
                <i class="fa fa-trash"></i>
            </button>
        `;
        tr.classList.remove('dark-search');
        tr.querySelectorAll('input').forEach(i => i.readOnly = true);
        return; // Kein Fetch.
    }

    // ── EDIT-MODUS: sofort speichern. ────────────
    const daten = _neueKontaktDaten(tr);


    // Validierung der Eingaben.
    const check = validiereKontaktDaten(daten);
    if (!check.valid) {
        await showAlert(check.message, 'error');
        return;
    }

    try {
        const r = await fetch('/bescheidbearbeitung/save-kontakt', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(daten)
        });
        const result = await r.json();
        if (result.success) {
            window.loadKontaktePage(1);
        } else {
            await showAlert('Fehler beim Speichern: ' + result.message, 'error');
        }
    } catch (err) {
        await showAlert('Verbindungsfehler: ' + err.message, 'error');
    }
};

// Neue (noch nicht gespeicherte) Zeile im Create-Modus bearbeiten.
window.editNewKontakteRow = function (btn) {
    const tr = btn.closest('tr');
    tr.querySelectorAll('input').forEach(i => i.readOnly = false);
    tr.classList.add('dark-search');

    const aktionsZelle = tr.querySelector('.k-actions');
    aktionsZelle.innerHTML = `
        <button class="p-btn p-btn-success" onclick="saveNewKontakte(this)" title="Speichern">
            <i class="fa fa-check"></i>
        </button>
        <button class="p-btn" onclick="this.closest('tr').remove()" title="Abbrechen">
            <i class="fa fa-xmark"></i>
        </button>
    `;
    tr.querySelector('input').focus();
};

// Hilfsfunktion: Felder aus einer neuen Zeile auslesen.
function _neueKontaktDaten(tr) {
    return {
        id: 0,
        aktenzeichen: document.getElementById('modalAktenzeichen').value,
        anrede: tr.querySelector('.k-edit-Anrede').value,
        vorname: tr.querySelector('.k-edit-Vorname').value,
        name: tr.querySelector('.k-edit-Name').value,
        email: tr.querySelector('.k-edit-Email').value,
        emailAnzeigename: tr.querySelector('.k-edit-EmailAnzeigename').value,
        emailAn: tr.querySelector('.k-edit-EmailAn').value,
        telefon: tr.querySelector('.k-edit-Telefon').value,
        fax: tr.querySelector('.k-edit-Fax').value,
    };
}

// Validierung.
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const TELEFON_REGEX = /^[0-9+\-\s()]{5,}$/;

// Felder-Bezeichnungen für Fehlermeldungen.
const KONTAKT_FELD_LABELS = {
    name: 'Name',
    email: 'E-Mail Adresse',
    telefon: 'Telefon'
};

// Prüft ein einzelnes Kontakt-Datenobjekt. Gibt { valid, message } zurück.
function validiereKontaktDaten(daten) {
    // Nur Name, E-Mail und Telefon sind Pflichtfelder.
    for (const feld of Object.keys(KONTAKT_FELD_LABELS)) {
        if (!daten[feld] || !daten[feld].trim()) {
            return { valid: false, message: `Bitte füllen Sie das Feld "${KONTAKT_FELD_LABELS[feld]}" aus.` };
        }
    }

    if (!EMAIL_REGEX.test(daten.email.trim())) {
        return { valid: false, message: 'Bitte geben Sie eine gültige E-Mail-Adresse ein.' };
    }

    if (!TELEFON_REGEX.test(daten.telefon.trim())) {
        return { valid: false, message: 'Bitte geben Sie eine gültige Telefonnummer ein.' };
    }

    return { valid: true, message: '' };
}







// Drag & Drop e-mail 
function escapeHtml(str) {
    return String(str ?? '').replace(/[&<>"']/g, c => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
}

const EMAIL_REGEX_LOOSE = /[^\s@<>"]+@[^\s@<>"]+\.[^\s@<>"]+/;

// Versucht, aus einem Text (z.B. Drag & Drop) Name, E-Mail und Telefon zu extrahieren.
function parseKontaktText(text) {
    if (!text) return null;
    text = text.trim();
    let name = '', email = '';

    const nameEmailMatch = text.match(/"?([^"<\n]*)"?\s*<([^>\s]+@[^>\s]+)>/);
    if (nameEmailMatch) {
        name = nameEmailMatch[1].trim();
        email = nameEmailMatch[2].trim();
    } else {
        const mailtoMatch = text.match(/mailto:([^\s"'<>]+)/i);
        if (mailtoMatch) {
            email = mailtoMatch[1];
        } else {
            const emailMatch = text.match(EMAIL_REGEX_LOOSE);
            if (emailMatch) email = emailMatch[0];
        }
    }

    let telefon = '';
    const telMatch = text.match(/(\+?\d[\d\-\s()]{6,}\d)/);
    if (telMatch) telefon = telMatch[0].trim();

    if (!email && !name && !telefon) return null;

    const teile = name.split(/\s+/).filter(Boolean);
    const vorname = teile.length > 1 ? teile.slice(0, -1).join(' ') : '';
    const nachname = teile.length > 1 ? teile[teile.length - 1] : (teile[0] || '');

    return { vorname, name: nachname, email, telefon };
}

// Sendet .msg oder .eml Datei an den Server zum Parsen (via MsgReader NuGet package).
async function parseMailFileViaServer(file) {
    const formData = new FormData();
    formData.append('file', file);

    const resp = await fetch('/bescheidbearbeitung/parse-mail', {
        method: 'POST',
        body: formData
    });

    if (!resp.ok) {
        const errText = await resp.text().catch(() => resp.statusText);
        throw new Error(`Server-Parsing fehlgeschlagen (${resp.status}): ${errText}`);
    }

    const data = await resp.json();
    // Server liefert bereits vorname/name/email/telefon getrennt
    if (!data.email && !data.name && !data.vorname) return null;
    return {
        vorname: data.vorname || '',
        name: data.name || '',
        email: data.email || '',
        telefon: data.telefon || ''
    };
}

window.handleKontakteDragOver = function (e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
};

// Drag & Drop Event-Handler für den Kontakte-Tab.
window.handleKontakteDrop = async function (e) {
    e.preventDefault();
    const dt = e.dataTransfer;
    let daten = null;

    // .msg (Outlook) oder .eml Datei immer über den Server parsen.
    if (dt.files && dt.files.length > 0) {
        const file = dt.files[0];
        const isMailFile = /\.(msg|eml)$/i.test(file.name);

        if (isMailFile) {
            try {
                daten = await parseMailFileViaServer(file);
            } catch (err) {
                console.error('Fehler beim Server-Parsing:', err);
                await showAlert('Fehler beim Verarbeiten der Datei: ' + err.message, 'error');
                return;
            }
        }
    }

    // Fallback: Text, HTML oder URI (z.B. wenn kein File-Objekt vorhanden ist).
    if (!daten || (!daten.email && !daten.name)) {
        const plain = dt.getData('text/plain');
        const html = dt.getData('text/html');
        const uri = dt.getData('text/uri-list');
        daten = parseKontaktText(plain) || parseKontaktText(html) || parseKontaktText(uri);
    }

    if (!daten || (!daten.email && !daten.name)) {
        await showAlert('Fehler beim Extrahieren der Kontaktdaten aus dem gezogenen Element.', 'error');
        return;
    }

    addKontakteRow(daten);
};