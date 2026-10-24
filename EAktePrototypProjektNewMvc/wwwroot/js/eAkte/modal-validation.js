let tab1Fertig = false; // true = Tab 1 vollständig ausgefüllt.
let tab2Fertig = false; // true = Tab 2 vollständig ausgefüllt.


// Pflichtfelder Tab 1 (Vorgangsdaten) Gibt ein Array aller Pflichtfeld-Elemente zurück.
function getPflichtfelderTab1() {
    return [
        // Textfelder.
        document.getElementById('inputAntragsdatum'),

        // Selects.
        document.getElementById('selectVorgangstyp'),
        document.getElementById('selectAntragsart'),

        // Textareas.
        document.getElementById('inputGegenstand'),
        document.getElementById('inputBetreff')
    ];
}

// Prüft, ob in einer Radio-Gruppe (nach name) eine Option ausgewählt ist.
function istRadioGruppeAusgefuellt(name) {
    return document.querySelector(`input[type="radio"][name="${name}"]:checked`) !== null;
}

// Pflichtfelder Tab 2 (Antragsteller 1).
function getPflichtfelderTab2() {
    const felder = [];
    document.querySelectorAll('#tab-applicant .applicant-block').forEach(block => {
        felder.push(
            block.querySelector('.antragsteller-adresse'),
            block.querySelector('.antragsteller-tel'),
            block.querySelector('.antragsteller-email'),
        );
    });
    return felder;
}


// Hilfsfunktion: Ist das Feld ausgefüllt?
function istAusgefuellt(el) {
    if (!el) return false;
    return el.value.trim().length > 0;
}

// Tab 1 validieren. Überprüft alle Pflichtfelder, aktualisiert Buttons, Fortschrittsbalken und Tab-Badges.
function validateTab1() {
    const felder = getPflichtfelderTab1();
    const alleNormalenFertig = felder.every(istAusgefuellt);

    // Radio-Gruppen separat prüfen.
    const antragGewaehlt = istRadioGruppeAusgefuellt('vorgang_antrag');
    const bescheidGewaehlt = istRadioGruppeAusgefuellt('vorgang_bescheid');

    const alleFertig = alleNormalenFertig && antragGewaehlt && bescheidGewaehlt;

    felder.forEach(el => {
        if (!el) return;
        el.classList.toggle('invalid', !istAusgefuellt(el));
    });

    // Visuelles Feedback für Radio-Gruppen (rote Umrandung, falls nichts gewählt).
    document.querySelector('[data-radio-group="vorgang_antrag"]')
        ?.classList.toggle('invalid', !antragGewaehlt);
    document.querySelector('[data-radio-group="vorgang_bescheid"]')
        ?.classList.toggle('invalid', !bescheidGewaehlt);

    const btnWeiter = document.getElementById('btnWeiter1');
    const btnSaveVorgang = document.getElementById('btnSaveVorgang');
    const badge = document.getElementById('badge-tab1');
    const badgeIcon = document.getElementById('badge-tab1-icon');
    const badgeText = document.getElementById('badge-tab1-text');

    if (alleFertig) {
        if (btnWeiter) {
            btnWeiter.disabled = false;
            btnWeiter.classList.add('btn-ready');
        }
        if (btnSaveVorgang) {
            btnSaveVorgang.disabled = false;
            btnSaveVorgang.className = 'akte-btn akte-btn-save-ready';
        }

        badge.classList.remove('text-error-modal');
        badge.classList.add('text-success-modal', 'visible');
        badgeIcon.classList.remove('fa-circle-exclamation', 'icon-error');
        badgeIcon.classList.add('fa-circle-check', 'icon-success');
        badgeText.textContent = 'Alle Pflichtfelder ausgefüllt.';

        tab1Fertig = true;
        tabGalochkeSetzen('process');
    } else {
        if (btnWeiter) {
            btnWeiter.disabled = true;
            btnWeiter.classList.remove('btn-ready');
        }
        if (btnSaveVorgang) {
            btnSaveVorgang.disabled = true;
            btnSaveVorgang.className = 'akte-btn akte-btn-save-locked';
        }

        badge.classList.remove('visible', 'text-error-modal', 'text-success-modal');

        tab1Fertig = false;
        tabGalochkeEntfernen('process');
    }

    fortschrittsbalkenAktualisieren();
    speichernButtonAktualisieren();
    updateTabButtons();
}


// Tab 2 validieren.
function validateTab2() {
    const felder = getPflichtfelderTab2();
    const alleFertig = felder.every(istAusgefuellt);

    felder.forEach(el => {
        if (!el) return;
        el.classList.toggle('invalid', !istAusgefuellt(el));
    });

    const btnWeiter = document.getElementById('btnWeiter2');
    const btnSaveAntr = document.getElementById('btnSaveAntragsteller');
    const badge = document.getElementById('badge-tab2');
    const badgeIcon = document.getElementById('badge-tab2-icon');
    const badgeText = document.getElementById('badge-tab2-text');

    if (alleFertig) {
        if (btnWeiter) {
            btnWeiter.disabled = false;
            btnWeiter.classList.add('btn-ready');
        }
        if (btnSaveAntr) {
            btnSaveAntr.disabled = false;
            btnSaveAntr.className = 'akte-btn akte-btn-save-ready';
        }

        // Grüner Erfolgsstatus.
        badge.classList.remove('text-error-modal');
        badge.classList.add('text-success-modal', 'visible');
        badgeIcon.classList.remove('fa-circle-exclamation', 'icon-error');
        badgeIcon.classList.add('fa-circle-check', 'icon-success');
        badgeText.textContent = 'Alle Pflichtfelder ausgefüllt.';

        tab2Fertig = true;
        tabGalochkeSetzen('applicant');
    } else {
        if (btnWeiter) {
            btnWeiter.disabled = true;
            btnWeiter.classList.remove('btn-ready');
        }
        if (btnSaveAntr) {
            btnSaveAntr.disabled = true;
            btnSaveAntr.className = 'akte-btn akte-btn-save-locked';
        }

        // Solange der Nutzer noch tippt, verstecken wir den Status.
        badge.classList.remove('visible', 'text-error-modal', 'text-success-modal');

        tab2Fertig = false;
        tabGalochkeEntfernen('applicant');
    }

    fortschrittsbalkenAktualisieren();
    speichernButtonAktualisieren();
    updateTabButtons();
}



// Hinweis Tab 1 anzeigen. Zeigt den Fehlerhinweis und markiert alle leeren Pflichtfelder. Wird aufgerufen, wenn "Weiter" geklickt wird, obwohl Tab 1 unvollständig ist.
function zeigeHinweisTab1() {
    const badge = document.getElementById('badge-tab1');
    const badgeIcon = document.getElementById('badge-tab1-icon');
    const badgeText = document.getElementById('badge-tab1-text');

    badge.classList.remove('text-success-modal');
    badge.classList.add('text-error-modal', 'visible');
    badgeIcon.classList.remove('fa-circle-check', 'icon-success');
    badgeIcon.classList.add('fa-circle-exclamation', 'icon-error');
    badgeText.textContent = 'Bitte alle Pflichtfelder ausfüllen.';

    getPflichtfelderTab1().forEach(el => {
        if (!istAusgefuellt(el)) el?.classList.add('invalid');
    });

    // Radio-Gruppen markieren, falls nichts gewählt.
    if (!istRadioGruppeAusgefuellt('vorgang_antrag')) {
        document.querySelector('[data-radio-group="vorgang_antrag"]')?.classList.add('invalid');
    }
    if (!istRadioGruppeAusgefuellt('vorgang_bescheid')) {
        document.querySelector('[data-radio-group="vorgang_bescheid"]')?.classList.add('invalid');
    }
}

// Hinweis Tab 2 anzeigen. Analog zu Tab 1 für den Antragsteller-Bereich.
function zeigeHinweisTab2() {
    const badge = document.getElementById('badge-tab2');
    const badgeIcon = document.getElementById('badge-tab2-icon');
    const badgeText = document.getElementById('badge-tab2-text');

    badge.classList.remove('text-success-modal');
    badge.classList.add('text-error-modal', 'visible');
    badgeIcon.classList.remove('fa-circle-check', 'icon-success');
    badgeIcon.classList.add('fa-circle-exclamation', 'icon-error');
    badgeText.textContent = 'Bitte alle Pflichtfelder ausfüllen.';

    getPflichtfelderTab2().forEach(el => {
        if (!istAusgefuellt(el)) el?.classList.add('invalid');
    });
}



// Zeigt ein grünes Häkchen-Badge neben dem Tab-Button.
function tabGalochkeSetzen(name) {
    const btn = document.getElementById('tab-btn-' + name);
    if (!btn) return;

    // Bereits vorhanden? Nicht doppelt einfügen.
    if (btn.querySelector('.tab-check')) return;
    
    // Häkchen-Element erstellen und einfügen.
    const check = document.createElement('span');
    check.className = 'tab-check';
    check.title     = 'Ausgefüllt';
    check.innerHTML = '&#10003;'; // Zeichen.
    btn.appendChild(check);
}

// Häkchen-Badge aus Tab-Leiste entfernen. Wird aufgerufen, wenn ein Pflichtfeld wieder geleert wird.
function tabGalochkeEntfernen(name) {
    const btn   = document.getElementById('tab-btn-' + name);
    const check = btn?.querySelector('.tab-check');
    if (check) check.remove();
}



// Speichern-Button auf Tab 3 aktualisieren. Wird nach jeder Validierung aufgerufen.
function speichernButtonAktualisieren() {
    const btn   = document.getElementById('btnSaveAll');
    const icon  = document.getElementById('saveIcon');
    const badge = document.getElementById('badge-tab3');

    if (!btn) return;

    if (tab1Fertig && tab2Fertig) {
        // Beide Tabs fertig: Speichern-Button aktivieren (grün).
        btn.disabled  = false;
        btn.className = 'akte-btn akte-btn-save-ready';
        if (icon) icon.className = 'fa-solid fa-floppy-disk'; // Disketten-Icon.
        if (badge) badge.style.opacity = 1;          // Grünes Badge.
    } else {
        // Noch nicht fertig: Speichern-Button sperren (grau mit Schloss).
        btn.disabled  = true;
        btn.className = 'akte-btn akte-btn-save-locked';
        if (icon) icon.className = 'fa-solid fa-lock';         // Schloss-Icon.
        if (badge) badge.style.opacity = 0;
    }
}



// Fortschrittsbalken aktualisieren.
function fortschrittsbalkenAktualisieren() {
    const balken = document.getElementById('progressBar');
    if (!balken) return;

    const fertigAnzahl = (tab1Fertig ? 1 : 0) + (tab2Fertig ? 1 : 0);
    const prozent = fertigAnzahl * 50;

    balken.style.width = prozent + '%';
    balken.style.background = fertigAnzahl === 2 ? '#16a34a' : '#2563EB';
}
