// Zähler für dynamisch hinzugefügte Antragsteller-Blöcke.
let antragstellerCount = 1;

// Tab "Antragsteller" initialisieren.
function initAntragsteller() {
    // Zähler zurücksetzen.
    antragstellerCount = 1;

    // "Adresse suchen"-Button: nur aktiv wenn eine Checkbox gesetzt.
    function adresseSuchenButtonAktualisieren() {
        // Prüfen ob mindestens eine Checkbox im Tab aktiviert ist.
        const irgendEineAktiv = document.querySelectorAll(
            '#tab-applicant .applicant-block input[type="checkbox"]:checked'
        ).length > 0;

        const btn = document.getElementById('adresseSuchenBtn');
        if (btn) btn.disabled = !irgendEineAktiv;
    }

    // Listener für alle Checkboxen im Tab (per Delegation auf den Tab-Container).
    document.getElementById('tab-applicant')
        ?.addEventListener('change', e => {
            if (e.target.type === 'checkbox') {
                adresseSuchenButtonAktualisieren();
            }
        });

    // "Weiteren Antragsteller hinzufügen"-Button.
    const hinzufuegenBtn = document.querySelector('.add-applicant-btn');
    if (hinzufuegenBtn) {
        hinzufuegenBtn.addEventListener('click', function () {
            antragstellerHinzufuegen();
        });
    }

    // Weitere Antragsteller-Blöcke bei Bearbeitung laden und ausfüllen (Antragsteller2, 3, ...).
    if (currentMode == "edit" && dynamischeAntragsteller.length > 0) {
        dynamischeAntragsteller.forEach(a => {

            antragstellerHinzufuegen();

            const blocks = document.querySelectorAll('.applicant-block[data-index]');
            const letzterBlock = blocks[blocks.length - 1];

            letzterBlock.querySelector('.antragsteller-adresse').value = a.Adresse ?? '';
            letzterBlock.querySelector('.antragsteller-adresszusatz').value = a.AdresseZusatz ?? '';
            letzterBlock.querySelector('.antragsteller-tel').value = a.Tel ?? '';
            letzterBlock.querySelector('.antragsteller-fax').value = a.Fax ?? '';
            letzterBlock.querySelector('.antragsteller-email').value = a.Email ?? '';
        });
    }

}

// Neuen Antragsteller-Block dynamisch einfügen.
function antragstellerHinzufuegen() {
    antragstellerCount++;

    const hinzufuegenBtn = document.querySelector('.add-applicant-btn');
    const block = document.createElement('div');
    block.className  = 'applicant-block';
    block.dataset.index = antragstellerCount; // Indexmarkierung für Neuordnung.

    block.innerHTML = `
        <div class="applicant-block-header" data-type="Antragsteller ${antragstellerCount}">
            <input type="checkbox" /> Antragsteller ${antragstellerCount}
            <button class="remove-applicant-btn" onclick="antragstellerEntfernen(this)">✕</button>
        </div>
        <div class="field-group cols-1">
            <textarea class="modal-textarea antragsteller-adresse req-tab2"       placeholder="Adresse *" oninput="validateTab2()"></textarea>
            <textarea class="modal-textarea antragsteller-adresszusatz"  placeholder="Adresszusatz"></textarea>
        </div>
        <div class="field-group">
            <input class="modal-input antragsteller-tel req-tab2" placeholder="Tel *" oninput="validateTab2()"/>
            <input class="modal-input antragsteller-fax" placeholder="Fax" />
            <input class="modal-input antragsteller-email req-tab2" placeholder="E-Mail *" style="grid-column:1/-1;" oninput="validateTab2()" />
        </div>
    `;

    // Block vor dem "Hinzufügen"-Button einfügen.
    hinzufuegenBtn.parentNode.insertBefore(block, hinzufuegenBtn);

    validateTab2(); // Validierung erneut ausführen, da neue Pflichtfelder hinzugekommen sind.
}

// Antragsteller-Block entfernen.
function antragstellerEntfernen(btn) {
    // Übergeordneten Block entfernen.
    btn.closest('.applicant-block').remove();

    // Nummerierung der verbleibenden dynamischen Blöcke anpassen.
    antragstellerNeuNummern();

    validateTab2(); // Validierung erneut ausführen, da neue Pflichtfelder hinzugekommen sind.
}


// Dynamische Blöcke neu nummerieren.
function antragstellerNeuNummern() {
    const dynamischeBlöcke = document.querySelectorAll('.applicant-block[data-index]');

    dynamischeBlöcke.forEach((block, i) => {
        const header    = block.querySelector('.applicant-block-header');
        const checkbox  = header.querySelector('input[type="checkbox"]');
        const entfernen = header.querySelector('.remove-applicant-btn');

        const neueNummer = i + 2; // Antragsteller 2, 3, 4 ...
        const neuerTyp   = `Antragsteller ${neueNummer}`;

        // Header leeren und neu aufbauen
        header.dataset.type = neuerTyp;
        header.innerHTML    = '';
        header.appendChild(checkbox);
        header.append(` ${neuerTyp}`);
        header.appendChild(entfernen);
    });

    // Zähler anpassen: letzter Index = feste Blöcke (1) + dynamische.
    antragstellerCount = dynamischeBlöcke.length + 1;
}

// Alle Antragsteller-Felder leeren (Entfernen-Button).
function clearApplicants() {
    document.querySelectorAll('.applicant-block').forEach(block => {
        // Alle Textfelder und Eingaben leeren
        block.querySelectorAll('textarea').forEach(t => t.value = '');
        block.querySelectorAll('input').forEach(i => {
            if (i.type === 'checkbox') i.checked = false; // Checkbox deaktivieren.
            else i.value = '';                            // Textfeld leeren.
        });
    });

    // Tab-2-Validierung erneut ausführen (Button und Badge aktualisieren).
    validateTab2();
}

// Antragsteller-Daten an den Server senden.
async function antragstellerSpeichern(aktenzeichen) {
    const eintraege = [];

    // Alle Antragsteller-Blöcke durchlaufen und Daten sammeln.
    document.querySelectorAll('#tab-applicant .applicant-block').forEach(block => {
        const header = block.querySelector('.applicant-block-header');
        eintraege.push({
            type: header?.dataset.type ?? '',
            adresse: block.querySelector('.antragsteller-adresse')?.value ?? '',
            adresseZusatz: block.querySelector('.antragsteller-adresszusatz')?.value ?? '',
            tel: block.querySelector('.antragsteller-tel')?.value ?? '',
            fax: block.querySelector('.antragsteller-fax')?.value ?? '',
            email: block.querySelector('.antragsteller-email')?.value ?? ''
        });
    });

    // Abbruch wenn keine Daten vorhanden.
    if (eintraege.length === 0) {
        await showAlert('Keine Antragsteller-Blöcke gefunden.', "error");
        return false;
    }

    try {
        const response = await fetch(`/bescheidbearbeitung/save-antragsteller?aktenzeichen=${aktenzeichen}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(eintraege)
        });

        if (!response.ok) {
            await showAlert("Fehler beim Speichern der Antragsteller.", "error");
            return false;
        }

        const result = await response.json();

        if (result.success) {
            return true;
        } else {
            await showAlert(result.message ?? "Fehler beim Speichern der Antragsteller.", "error");
            return false;
        }

    } catch {
        await showAlert("Netzwerkfehler beim Speichern der Antragsteller.", "error");
        return false;
    }
}

// Alle Antragsteller-Checkboxen auswählen oder abwählen (je nach aktuellem Zustand).
function alleAuswaehlen() {
    const checkboxen = document.querySelectorAll('#tab-applicant .applicant-block input[type="checkbox"]');
    const alleGecheckt = [...checkboxen].every(cb => cb.checked);
    checkboxen.forEach(cb => cb.checked = !alleGecheckt);

    // Button-Text und Icon anpassen.
    const btnAlle = document.getElementById('btnAlleAuswaehlen');
    btnAlle.innerHTML = alleGecheckt
        ? '<i class="fa-solid fa-list-check"></i> Alle auswählen'
        : '<i class="fa-solid fa-square-minus"></i> Alle abwählen';

    // "Adresse suchen"-Button aktualisieren.
    const btn = document.getElementById('adresseSuchenBtn');
    if (btn) btn.disabled = alleGecheckt;
}


// Antragsteller-Tab direkt speichern (nur Edit-Modus).
async function saveAntragstellerTab(aktenzeichen) {
    const btn = document.getElementById('btnSaveAntragsteller');

    // Spinner anzeigen.
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Wird gespeichert…';
    }

    try {
        const ok = await antragstellerSpeichern(aktenzeichen);

        if (ok) {
            // Kurzes visuelles Feedback: grüner Haken.
            if (btn) btn.innerHTML = '<i class="fa-solid fa-check"></i> Gespeichert';
            setTimeout(() => {
                if (btn) {
                    btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Antragsteller speichern';
                    validateTab2();
                }
            }, 1500);
        } else {
            // Fehler — Button wiederherstellen.
            if (btn) btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Antragsteller speichern';
            validateTab2();
        }

    } catch {
        await showAlert("Verbindungsfehler beim Speichern.", "error");
        if (btn) btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Antragsteller speichern';
        validateTab2();
    }
}