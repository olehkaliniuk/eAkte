const gebuehrenEndpoint = '/bescheidbearbeitung/load-gebuehre'; // Server-Endpunkt.

// Schaltflächen-Gruppen (Werte von data-funktion in Kleinbuchstaben).
const G_VORSCHUSS = ['vorschuss neu'];       // abhängig von der letzten Vorschussforderung (gibt es keine: letzte Gebührenforderung)
const G_GEBUEHR = ['gebühr neu'];          // abhängig von der letzten Gebührenforderung (gibt es keine: letzte Vorschussforderung)
const G_LETZTE = ['widerspruch neu'];     // abhängig von der letzten Zeile insgesamt
const G_ZEILE = ['stornieren', 'löschen']; // abhängig von der angeklickten Zeile
// "Status anpassen" ist immer aktiv und steht deshalb in keiner Gruppe.



// 1. Allgemeine Hilfsfunktionen
// Container-Getter.
function gebuehrenContainer() {
    return document.getElementById('gebuehrenTableContainer');
}

// Ist die Zeile eine Vorschussforderung?
function gIstVorschuss(typ) {
    const t = (typ ?? '').trim().toLowerCase();
    return t.includes('vorschuss') || t === 'v';
}

// Betrag aus der Eingabe lesen ("1.000,50" oder "500.5"); leer -> 0, ungültig -> NaN.
function gBetrag(text) {
    let s = String(text ?? '').trim();
    if (s === '') return 0;
    s = s.includes(',') ? s.replace(/\./g, '').replace(',', '.') : s;
    const n = Number(s);
    return Number.isFinite(n) && n >= 0 ? n : NaN;
}

// Zahl (Punkt als Dezimaltrenner) als deutschen Text, z. B. 500 -> "500,00"; 0 -> leer.
function gBetragText(zahl) {
    const n = Number(zahl);
    return Number.isFinite(n) && n !== 0 ? n.toFixed(2).replace('.', ',') : '';
}

// Ausgewählte Zeile der Tabelle (zuletzt angeklickt).
function gAusgewaehlteZeile() {
    return document.querySelector('#gebuehrenTable tbody.selected .main-row');
}

// Liest alle Beträge eines Formulars (Neu oder Bearbeiten) und gibt die Titel-Liste zurück.
// Bei einem Fehler wird eine Meldung angezeigt und null zurückgegeben.
async function gTitelAuslesen(panelId, titel3Id, betrag3Id) {
    const titel = [];
    for (const inp of document.querySelectorAll('#' + panelId + ' .g-betrag')) {
        const betrag = gBetrag(inp.value);
        if (Number.isNaN(betrag)) {
            await showAlert("Ungültiger Betrag.", "error");
            return null;
        }
        // Dritte Zeile: TitelNr kommt aus dem Select, die ersten beiden sind fest.
        const nr = inp.id === betrag3Id
            ? document.getElementById(titel3Id).value
            : inp.dataset.titel;
        if (betrag > 0 && !nr) {
            await showAlert("Bitte einen Titel auswählen.", "error");
            return null;
        }
        if (betrag > 0) titel.push({ titelNr: nr, betrag });
    }
    if (titel.length === 0) {
        await showAlert("Bitte mindestens einen Betrag eingeben.", "error");
        return null;
    }
    return titel;
}



// 2. Tabelle laden und Schaltflächen steuern
// Tabelle per AJAX nachladen. Parameter: params — Objekt mit aktenzeichen.
async function gebuehrenTabelleLaden(params) {
    const query = new URLSearchParams(params);
    try {
        const response = await fetch(`${gebuehrenEndpoint}?${query}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden der Gebühren.", "error");
            return;
        }
        const html = await response.text();
        // Neuen HTML-Inhalt in den Container einfügen.
        gebuehrenContainer().innerHTML = html;
        // Ereignis-Listener für die neuen DOM-Elemente neu binden.
        gListenersBinden();
        // Schaltflächen und Icons anhand der neu geladenen Tabelle aktualisieren.
        await gFunktionenAktualisieren();
        // Letzte (aktuellste) Zeile öffnen.
        gLetzteZeileOeffnen();
    } catch {
        await showAlert("Netzwerkfehler beim Laden der Gebühren.", "error");
    }
}

// Fragt beim Server die erlaubten Funktionen für Typ + Status ab. Gibt ein Array in Kleinbuchstaben zurück, bei Fehler null.
async function gErlaubteFunktionen(typ, status) {
    try {
        const url = '/bescheidbearbeitung/load-gebuehr-funktionen?typ=' + encodeURIComponent(typ ?? '') +
            '&status=' + encodeURIComponent(status ?? '');
        const response = await fetch(url);
        if (!response.ok) throw new Error('HTTP ' + response.status);
        return (await response.json()).map(f => f.trim().toLowerCase());
    } catch (err) {
        console.error(err);
        return null;
    }
}

// Aktiviert/deaktiviert die Schaltflächen einer Gruppe. erlaubt = null -> alle deaktivieren.
function gButtonsSetzen(gruppe, erlaubt) {
    document.querySelectorAll('.create-akte-btn-container [data-funktion]').forEach(btn => {
        const f = btn.dataset.funktion.toLowerCase();
        if (!gruppe.includes(f)) return;
        btn.disabled = !(erlaubt && erlaubt.includes(f));
    });
}

// Setzt pro Zeile die Klasse "edit-ok", wenn "Bearbeiten" für Typ + Status erlaubt ist (Stift statt Lupe).
async function gEditIconsSetzen(rows) {
    const cache = new Map(); // gleiche Typ/Status-Kombinationen nur einmal abfragen
    await Promise.all(rows.map(async r => {
        const key = r.dataset.typ + '|' + r.dataset.status;
        if (!cache.has(key)) cache.set(key, gErlaubteFunktionen(r.dataset.typ, r.dataset.status));
        const erlaubt = await cache.get(key);
        r.classList.toggle('edit-ok', !!erlaubt && erlaubt.includes('bearbeiten'));
    }));
}

// Nach dem Laden der Tabelle:
//  - Icons (Lupe/Stift) pro Zeile setzen
//  - Vorschuss Neu / Gebühr Neu / Widerspruch anhand der letzten Zeilen berechnen
//  - Stornieren/Löschen werden zurückgesetzt, bis eine Zeile angeklickt wird
async function gFunktionenAktualisieren() {
    gButtonsSetzen(G_ZEILE, null);

    const rows = [...document.querySelectorAll('#gebuehrenTable .main-row')];

    await gEditIconsSetzen(rows);

    // Keine Einträge: Vorschuss Neu und Gebühr Neu sind standardmäßig aktiv, Widerspruch nicht.
    if (rows.length === 0) {
        gButtonsSetzen(G_VORSCHUSS, G_VORSCHUSS);
        gButtonsSetzen(G_GEBUEHR, G_GEBUEHR);
        gButtonsSetzen(G_LETZTE, null);
        gFussSetzen(null);
        return;
    }

    const letzteVorschuss = rows.filter(r => gIstVorschuss(r.dataset.typ)).pop();
    const letzteGebuehr = rows.filter(r => !gIstVorschuss(r.dataset.typ)).pop();
    const letzte = rows[rows.length - 1];

    // Fehlt ein Typ, dient die letzte Zeile des anderen Typs als Quelle.
    const quelleVorschuss = letzteVorschuss ?? letzteGebuehr;
    const quelleGebuehr = letzteGebuehr ?? letzteVorschuss;

    const [vorschuss, gebuehr, widerspruch] = await Promise.all([
        gErlaubteFunktionen(quelleVorschuss.dataset.typ, quelleVorschuss.dataset.status),
        gErlaubteFunktionen(quelleGebuehr.dataset.typ, quelleGebuehr.dataset.status),
        gErlaubteFunktionen(letzte.dataset.typ, letzte.dataset.status)
    ]);

    gButtonsSetzen(G_VORSCHUSS, vorschuss);
    gButtonsSetzen(G_GEBUEHR, gebuehr);
    gButtonsSetzen(G_LETZTE, widerspruch);
}

// Klick auf eine Zeile: nur Stornieren/Löschen für diese Zeile.
let gKlickZaehler = 0;
async function gFunktionenFuerZeile(row) {
    gFussSetzen(row); 
    const nr = ++gKlickZaehler;               // Schutz gegen Race Conditions bei schnellen Klicks
    const erlaubt = await gErlaubteFunktionen(row.dataset.typ, row.dataset.status);
    if (nr !== gKlickZaehler) return;         // ein neuerer Klick ist inzwischen angekommen
    gButtonsSetzen(G_ZEILE, erlaubt);
}

// Auf-/Zuklappen der Titel-Untertabelle + Hervorheben + Schaltflächen der Zeile.
function gZeileUmschalten(e) {
    const row = e.target.closest("tr.main-row");
    if (!row) return;
    const entry = row.parentNode;

    // Klick auf den Stift: Zeile auswählen und das Bearbeiten-Formular öffnen.
    if (e.target.closest('td.icon-cell') && row.classList.contains('edit-ok')) {
        this.querySelectorAll("tbody.selected").forEach(t => t.classList.remove("selected"));
        entry.classList.add("open", "selected");
        gFunktionenFuerZeile(row);
        gEditOeffnen(row);
        return;
    }

    entry.classList.toggle("open");
    this.querySelectorAll("tbody.selected").forEach(t => t.classList.remove("selected"));
    entry.classList.add("selected");
    gFunktionenFuerZeile(row);
}

// Öffnet die letzte (aktuellste) Zeile: aufklappen, hervorheben, Schaltflächen berechnen.
function gLetzteZeileOeffnen() {
    const rows = document.querySelectorAll('#gebuehrenTable .main-row');
    const letzte = rows[rows.length - 1];
    if (!letzte) return;

    const entry = letzte.parentNode;                       // <tbody class="entry">
    document.querySelectorAll('#gebuehrenTable tbody.selected')
        .forEach(t => t.classList.remove('selected'));
    entry.classList.add('open', 'selected');

    gFunktionenFuerZeile(letzte);                          // Stornieren/Löschen
}

// Listener an die neu eingefügte Tabelle und an die Formulare binden.
function gListenersBinden() {
    document.getElementById('gebuehrenTable')?.addEventListener('click', gZeileUmschalten);
    gNeuBinden();
    gEditBinden();
    gStatusBinden();
}

// Gebühren laden.
window.loadGebuehrenPage = function () {
    gebuehrenTabelleLaden({
        aktenzeichen: aFeldWert('modalAktenzeichen')
    });
};

// Tab Gebühren: Listener an die initial gerenderte Tabelle binden.
async function initGebuehren() {
    gListenersBinden();
    await gFunktionenAktualisieren();
    gLetzteZeileOeffnen();
}



// 3. Neu anlegen: Vorschuss / Gebühr (Formular liegt fest im DOM, wird nur ein-/ausgeblendet)
let gNeuArt = null; // 'V' oder 'G', solange das Formular offen ist.

// Formular leeren und verstecken.
function gNeuSchliessen() {
    const panel = document.getElementById('gNeuPanel');
    if (!panel) return;
    panel.querySelectorAll('input, textarea').forEach(el => el.value = '');
    const titel3 = document.getElementById('gNeuTitel3');
    if (titel3) titel3.value = '';
    panel.hidden = true;
    gNeuArt = null;
}

// Formular anzeigen. art: 'V' = Vorschuss, 'G' = Gebühr.
function gNeuOeffnen(art) {
    const panel = document.getElementById('gNeuPanel');
    if (!panel) return;

    gEditSchliessen();     // Bearbeiten-Formular ausblenden
    gStatusSchliessen();   // Status-Formular ausblenden

    // Beim Wechsel zwischen Vorschuss und Gebühr die Eingaben zurücksetzen.
    if (gNeuArt !== art) gNeuSchliessen();
    gNeuArt = art;

    const istV = art === 'V';
    document.getElementById('gNeuTitelText').textContent =
        istV ? 'Vorschussforderung anlegen' : 'Gebührenforderung anlegen';
    document.getElementById('gNeuSpalte').textContent =
        istV ? 'Vorschuss gefordert' : 'Gebühr gefordert';

    panel.hidden = false;
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// Schaltflächen des Formulars binden (onclick wird überschrieben, daher mehrfaches Aufrufen unkritisch).
function gNeuBinden() {
    const speichern = document.getElementById('gNeuSpeichern');
    const abbrechen = document.getElementById('gNeuAbbrechen');
    if (!speichern || !abbrechen) return;
    abbrechen.onclick = gNeuSchliessen;
    speichern.onclick = () => gNeuSpeichern(gNeuArt);
}

// Validiert die Eingaben, sendet sie an den Server und lädt danach die Tabelle neu.
async function gNeuSpeichern(art) {
    if (!art) return;

    const titel = await gTitelAuslesen('gNeuPanel', 'gNeuTitel3', 'gNeuBetrag3');
    if (!titel) return;

    const daten = {
        aktenzeichen: aFeldWert('modalAktenzeichen'),
        art,
        tarifstelle: document.getElementById('gNeuTarif').value.trim(),
        ermittlung: document.getElementById('gNeuErmittlung').value.trim(),
        bemerkung: document.getElementById('gNeuBemerkung').value.trim(),
        iban: document.getElementById('gNeuIban').value.trim(),
        bic: document.getElementById('gNeuBic').value.trim(),
        titel
    };

    const btn = document.getElementById('gNeuSpeichern');
    btn.disabled = true;
    try {
        const response = await fetch('/bescheidbearbeitung/gebuehr-anlegen', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(daten)
        });
        if (!response.ok) {
            await showAlert(await response.text() || "Anlegen fehlgeschlagen.", "error");
            btn.disabled = false;
            return;
        }
        gNeuSchliessen();             // Formular leeren und verstecken
        btn.disabled = false;
        window.loadGebuehrenPage();   // Tabelle + Schaltflächen neu laden
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        btn.disabled = false;
    }
}



// 4. Bearbeiten (Stift-Icon): Formular liegt fest im DOM, wird nur ein-/ausgeblendet
let gEditLfdNr = null; // LfdNr der Zeile, die gerade bearbeitet wird.
let gEditArt = null;   // 'V' oder 'G'.

// Formular leeren und verstecken.
function gEditSchliessen() {
    const panel = document.getElementById('gEditPanel');
    if (!panel) return;
    panel.querySelectorAll('input, textarea').forEach(el => el.value = '');
    const titel3 = document.getElementById('gEditTitel3');
    if (titel3) titel3.value = '';
    panel.hidden = true;
    gEditLfdNr = null;
    gEditArt = null;
}

// Formular mit den Daten der ausgewählten Zeile füllen und anzeigen.
function gEditOeffnen(row) {
    const panel = document.getElementById('gEditPanel');
    if (!panel) return;

    gNeuSchliessen();      // "Neu"-Formular ausblenden
    gStatusSchliessen();   // Status-Formular ausblenden
    gEditSchliessen();     // alte Eingaben verwerfen

    const d = row.dataset;
    const istV = gIstVorschuss(d.typ);
    gEditArt = istV ? 'V' : 'G';
    gEditLfdNr = d.lfdnr;

    document.getElementById('gEditTitelText').textContent =
        (istV ? 'Vorschussforderung' : 'Gebührenforderung') + ' bearbeiten (LfdNr ' + d.lfdnr + ')';
    document.getElementById('gEditSpalte').textContent = istV ? 'Vorschuss gefordert' : 'Gebühr gefordert';

    // Beträge der Titel eintragen: 11106 und 28102 sind feste Zeilen, jeder andere Titel kommt in die dritte Zeile.
    row.parentNode.querySelectorAll('.sub tbody tr[data-titelnr]').forEach(tr => {
        const nr = tr.dataset.titelnr;
        const text = gBetragText(tr.dataset.betrag);
        if (nr === '11106') document.getElementById('gEditBetrag1').value = text;
        else if (nr === '28102') document.getElementById('gEditBetrag2').value = text;
        else {
            document.getElementById('gEditTitel3').value = nr;
            document.getElementById('gEditBetrag3').value = text;
        }
    });

    // Eingabefelder.
    document.getElementById('gEditTarif').value = d.tarif ?? '';
    document.getElementById('gEditErmittlung').value = d.ermittlung ?? '';
    document.getElementById('gEditBemerkung').value = d.bemerkung ?? '';
    document.getElementById('gEditIban').value = d.iban ?? '';
    document.getElementById('gEditBic').value = d.bic ?? '';

    // Nur lesbare Informationen.
    document.getElementById('gInfoGebuehr').textContent = d.gebuehr ?? '';
    document.getElementById('gInfoGezahlt').textContent = d.gezahlt ?? '';
    document.getElementById('gInfoGezahltAm').textContent = d.gezahltam ?? '';
    document.getElementById('gInfoBeantragt').textContent = d.beantragt ?? '';
    document.getElementById('gInfoAngelegtVon').textContent = d.angelegtvon ?? '';
    document.getElementById('gInfoAngelegtAm').textContent = d.angelegtam ?? '';
    document.getElementById('gInfoGeaendertVon').textContent = d.geaendertvon ?? '';
    document.getElementById('gInfoGeaendertAm').textContent = d.geaendertam ?? '';

    panel.hidden = false;
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// Schaltflächen des Formulars binden (onclick wird überschrieben, daher mehrfaches Aufrufen unkritisch).
function gEditBinden() {
    const speichern = document.getElementById('gEditSpeichern');
    const abbrechen = document.getElementById('gEditAbbrechen');
    if (!speichern || !abbrechen) return;
    abbrechen.onclick = gEditSchliessen;
    speichern.onclick = gEditSpeichern;
}

// Validiert die Eingaben, sendet sie an den Server und lädt danach die Tabelle neu.
async function gEditSpeichern() {
    if (!gEditLfdNr) return;

    const titel = await gTitelAuslesen('gEditPanel', 'gEditTitel3', 'gEditBetrag3');
    if (!titel) return;

    const daten = {
        aktenzeichen: aFeldWert('modalAktenzeichen'),
        lfdNr: Number(gEditLfdNr),
        tarifstelle: document.getElementById('gEditTarif').value.trim(),
        ermittlung: document.getElementById('gEditErmittlung').value.trim(),
        bemerkung: document.getElementById('gEditBemerkung').value.trim(),
        iban: document.getElementById('gEditIban').value.trim(),
        bic: document.getElementById('gEditBic').value.trim(),
        titel
    };

    const btn = document.getElementById('gEditSpeichern');
    btn.disabled = true;
    try {
        const response = await fetch('/bescheidbearbeitung/gebuehr-bearbeiten', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(daten)
        });
        if (!response.ok) {
            await showAlert(await response.text() || "Speichern fehlgeschlagen.", "error");
            btn.disabled = false;
            return;
        }
        gEditSchliessen();            // Formular leeren und verstecken
        btn.disabled = false;
        window.loadGebuehrenPage();   // Tabelle + Schaltflächen neu laden
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        btn.disabled = false;
    }
}



// 5. Status anpassen: nur die Spalte Status der Gebühr ändern
let gStatusLfdNr = null; // LfdNr der Zeile, deren Status gerade angepasst wird.

// Formular verstecken.
function gStatusSchliessen() {
    const panel = document.getElementById('gStatusPanel');
    if (!panel) return;
    panel.hidden = true;
    gStatusLfdNr = null;
}

// Formular mit dem aktuellen Status der ausgewählten Zeile anzeigen.
function gStatusOeffnen(row) {
    const panel = document.getElementById('gStatusPanel');
    if (!panel) return;

    gNeuSchliessen();    // "Neu"-Formular ausblenden
    gEditSchliessen();   // Bearbeiten-Formular ausblenden

    gStatusLfdNr = row.dataset.lfdnr;
    document.getElementById('gStatusTitelText').textContent = 'Status anpassen (LfdNr ' + row.dataset.lfdnr + ')';
    document.getElementById('gStatusAuswahl').value = (row.dataset.status ?? '').trim().toUpperCase();

    panel.hidden = false;
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

// Schaltflächen des Formulars binden (onclick wird überschrieben, daher mehrfaches Aufrufen unkritisch).
function gStatusBinden() {
    const speichern = document.getElementById('gStatusSpeichern');
    const abbrechen = document.getElementById('gStatusAbbrechen');
    if (!speichern || !abbrechen) return;
    abbrechen.onclick = gStatusSchliessen;
    speichern.onclick = gStatusSpeichern;
}

// Sendet den neuen Status an den Server und lädt danach die Tabelle neu.
async function gStatusSpeichern() {
    if (!gStatusLfdNr) return;

    const btn = document.getElementById('gStatusSpeichern');
    btn.disabled = true;
    try {
        const response = await fetch('/bescheidbearbeitung/gebuehr-status', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                aktenzeichen: aFeldWert('modalAktenzeichen'),
                lfdNr: Number(gStatusLfdNr),
                status: document.getElementById('gStatusAuswahl').value
            })
        });
        if (!response.ok) {
            await showAlert(await response.text() || "Speichern fehlgeschlagen.", "error");
            btn.disabled = false;
            return;
        }
        gStatusSchliessen();
        btn.disabled = false;
        window.loadGebuehrenPage();   // Tabelle, Icons und Schaltflächen neu laden
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        btn.disabled = false;
    }
}



// 6. Stornieren / Löschen
// POST-Anfrage für eine Aktion auf einer Gebühr. Gibt bei Erfolg true zurück.
async function gAktionAusfuehren(endpoint, row) {
    const body = new URLSearchParams({
        aktenzeichen: aFeldWert('modalAktenzeichen'),
        lfdNr: row.dataset.lfdnr
    });
    try {
        const response = await fetch(endpoint, { method: 'POST', body });
        if (!response.ok) {
            await showAlert(await response.text() || "Aktion fehlgeschlagen.", "error");
            return false;
        }
        return true;
    } catch {
        await showAlert("Netzwerkfehler.", "error");
        return false;
    }
}



// 7. Globaler Klick-Handler für die Schaltflächen
// Wird nur einmal gebunden, da die Schaltflächen außerhalb des nachgeladenen Containers liegen.
if (!window._gAktionenGebunden) {
    window._gAktionenGebunden = true;

    // Aktualisieren-Schaltfläche.
    document.addEventListener('click', function (e) {
        if (e.target.closest('#gAktualisieren')) gAktualisieren();
    });


    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.create-akte-btn-container [data-funktion]');
        if (!btn || btn.disabled) return;

        const funktion = btn.dataset.funktion.toLowerCase();

        // Neu anlegen: Formular unter der Tabelle einblenden.
        if (funktion === 'vorschuss neu') { gNeuOeffnen('V'); return; }
        if (funktion === 'gebühr neu') { gNeuOeffnen('G'); return; }

        // Für alle weiteren Funktionen wird die ausgewählte Zeile benötigt.
        if (funktion !== 'status anpassen' && funktion !== 'stornieren' && funktion !== 'löschen') return;

        const row = gAusgewaehlteZeile();
        if (!row) {
            await showAlert("Bitte zuerst eine Zeile auswählen.", "error");
            return;
        }

        // Status anpassen: Formular mit dem aktuellen Status einblenden.
        if (funktion === 'status anpassen') {
            gStatusOeffnen(row);
            return;
        }

        // Stornieren / Löschen: erst bestätigen, dann an den Server senden.
        const nr = row.dataset.lfdnr;
        const istStorno = funktion === 'stornieren';
        const frage = istStorno
            ? `Gebühr LfdNr ${nr} wirklich stornieren?`
            : `Gebühr LfdNr ${nr} wirklich löschen?`;

        const bestaetigt = await showConfirm(frage, istStorno ? 'Ja, stornieren' : 'Ja, löschen', 'Abbrechen');
        if (!bestaetigt) return;

        const endpoint = istStorno
            ? '/bescheidbearbeitung/gebuehr-stornieren'
            : '/bescheidbearbeitung/gebuehr-loeschen';

        btn.disabled = true;
        if (await gAktionAusfuehren(endpoint, row)) {
            window.loadGebuehrenPage();   // Tabelle + Schaltflächen neu laden
        } else {
            btn.disabled = false;
        }
    });
}


// Füllt die Fußzeile mit den Daten der angegebenen Zeile (null -> leeren).
function gFussSetzen(row) {
    const d = row?.dataset ?? {};
    const setze = (id, wert) => {
        const el = document.getElementById(id);
        if (el) el.textContent = wert ?? '';
    };
    setze('gFussLfdNr', d.lfdnr);
    setze('gFussAnnahme', d.annahmevom);
    setze('gFussBescheid', d.bescheidvom);
    setze('gFussStorniert', d.storniertam);
    setze('gFussKassenwirksam', d.kassenwirksamam);
}


// Aktualisieren: Tabelle neu laden und offene Formulare schließen.
async function gAktualisieren() {
    const btn = document.getElementById('gAktualisieren');
    if (btn) btn.disabled = true;

    gNeuSchliessen();
    gEditSchliessen();
    gStatusSchliessen();

    await gebuehrenTabelleLaden({ aktenzeichen: aFeldWert('modalAktenzeichen') });

    if (btn) btn.disabled = false;
}