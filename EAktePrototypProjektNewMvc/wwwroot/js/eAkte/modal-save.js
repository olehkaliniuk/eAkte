// Globale Speichern-Funktion für das eAkte-Modal.
async function saveAll(zulassung, aktenzeichen) {
    if (!tab1Fertig || !tab2Fertig) {
        await showAlert('Bitte zuerst alle Pflichtfelder in Tab 1 und Tab 2 ausfüllen.', 'warning');
        return;
    }

    const neueKontakte = [];
    document.querySelectorAll('tr.k-row-new').forEach(tr => {
        neueKontakte.push(_neueKontaktDaten(tr));
    });

    let endpoint, payload;
    const vorgangObjektId = '00000000-0000-0000-0000-000000000000';

    const abteilung = document.getElementById('inputAbteilung')?.value ?? '';
    const referat = document.getElementById('inputReferat')?.value ?? '';
    const stelleNummer = document.getElementById('inputStelleNummer')?.value ?? '';

    // Radio-Gruppen auslesen.
    const antragWert = document.querySelector('input[name="vorgang_antrag"]:checked')?.value;
    const bescheidWert = document.querySelector('input[name="vorgang_bescheid"]:checked')?.value;

    const antragVorhanden = antragWert === 'mit_antrag'; // true bei "mit_antrag", false bei "ohne_antrag" oder nichts gewählt
    const bescheidNeufassung = bescheidWert === 'neufassung'; // true bei "neufassung", false bei "bescheid" oder nichts gewählt

    // Ansprechpartner: je nach Modus <select> (create) oder <input> (edit).
    const ansprechpartner = document.getElementById('selectAnsprechpartner')?.value ?? '';

    if (currentObjectType === 'vorgang') {
        zulassung = document.getElementById('modalZulassungsnummerAkte')?.value ?? zulassung;
        endpoint = '/bescheidbearbeitung/create-vorgang';
        payload = {
            vorgang: {
                aktenzeichen: aktenzeichen,
                userId: document.getElementById('uId')?.value ?? '',
                stelle: stelleNummer,
                zulassungsnummer: zulassung,
                datumAntrag: document.getElementById('inputAntragsdatum')?.value || new Date().toISOString(),
                antragsart: document.getElementById('selectAntragsart')?.value ?? '',
                bescheidtyp: document.getElementById('selectVorgangstyp')?.value ?? '',
                gegenstand: document.getElementById('inputGegenstand')?.value ?? '',
                status: '???',
                betreff: document.getElementById('inputBetreff')?.value ?? '',
                bemerkung: document.getElementById('inputBemerkung')?.value ?? '',
                objektId: vorgangObjektId,
                aktenzustand: '???',
                erzeugtVon: document.getElementById('inputName')?.value ?? '',
                veroeffentlicht: false,
                abteilung,
                referat,
                antragVorhanden,
                bescheidNeufassung,
                ansprechpartner
            },
            ordner: {
                parentObjectId: vorgangObjektId,
                type: '???',
                creatorName: document.getElementById('inputName')?.value ?? '',
                modifier: document.getElementById('inputName')?.value ?? '',
                ownerName: document.getElementById('inputName')?.value ?? '',
                ownerPermit: 0,
                groupPermit: 0,
                worldPermit: 0,
                hauptsachgebiet: document.getElementById('inputSVA')?.value ?? '',
                sachgebiet: document.getElementById('inputSachgebiet')?.value ?? '',
                aktenzeichen,
                lfdNr: 1,
                bemerkungen: document.getElementById('inputBemerkung')?.value ?? '',
                status: '??',
                final: false,
                path: '/',
                colorHex: '#ffa500',
                aclName: null,
                aclDomain: null,
                isHidden: false,
                isDeleted: false,
                isPublic: true,
                isSigned: false
            }
        };
    } else {
        endpoint = '/bescheidbearbeitung/create-akte-vorgang';
        payload = {
            akte: {
                zulassungsnummer: zulassung,
                lfdNr: document.getElementById('lfdNr')?.value ?? '',
                hauptgruppeNummer: document.getElementById('inputSVA')?.value ?? '',
                nebengruppeNummer: document.getElementById('inputSachgebiet')?.value ?? '',
                zustand: '???',
                erzeugtVon: document.getElementById('inputName')?.value ?? '',
                geandertVon: document.getElementById('inputName')?.value ?? '',
                abteilung,
                referat
            },
            vorgang: {
                aktenzeichen,
                userId: document.getElementById('uId')?.value ?? '',
                stelle: stelleNummer,
                zulassungsnummer: zulassung,
                datumAntrag: document.getElementById('inputAntragsdatum')?.value || new Date().toISOString(),
                antragsart: document.getElementById('selectAntragsart')?.value ?? '',
                bescheidtyp: document.getElementById('selectVorgangstyp')?.value ?? '',
                gegenstand: document.getElementById('inputGegenstand')?.value ?? '',
                status: '???',
                betreff: document.getElementById('inputBetreff')?.value ?? '',
                bemerkung: document.getElementById('inputBemerkung')?.value ?? '',
                objektId: vorgangObjektId,
                aktenzustand: '???',
                erzeugtVon: document.getElementById('inputName')?.value ?? '',
                veroeffentlicht: false,
                abteilung,
                referat,
                antragVorhanden,
                bescheidNeufassung,
                ansprechpartner
            },
            ordner: {
                parentObjectId: vorgangObjektId,
                type: '???',
                creatorName: document.getElementById('inputName')?.value ?? '',
                modifier: document.getElementById('inputName')?.value ?? '',
                ownerName: document.getElementById('inputName')?.value ?? '',
                ownerPermit: 0,
                groupPermit: 0,
                worldPermit: 0,
                hauptsachgebiet: document.getElementById('inputSVA')?.value ?? '',
                sachgebiet: document.getElementById('inputSachgebiet')?.value ?? '',
                aktenzeichen,
                lfdNr: 1,
                bemerkungen: document.getElementById('inputBemerkung')?.value ?? '',
                status: '??',
                final: false,
                path: '/',
                colorHex: '#ffa500',
                aclName: null,
                aclDomain: null,
                isHidden: false,
                isDeleted: false,
                isPublic: true,
                isSigned: false
            }
        };
    }

    try {
        ladeAnzeige(true);

        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector(
                    'input[name="__RequestVerificationToken"]'
                )?.value ?? ''
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            await showAlert('Serverfehler beim Speichern.', 'error');
            return;
        }

        const result = await response.json();

        if (!result.success) {
            await showAlert('Fehler: ' + (result.detail ?? result.error ?? result.message ?? 'Unbekannter Fehler'), 'error');
            return;
        }

        const adressErfolgreich = await antragstellerSpeichern(aktenzeichen);

        if (!adressErfolgreich) {
            await showAlert('Gespeichert, aber Antragsteller-Adressen konnten nicht gespeichert werden.', 'warning');
        } else {
            await showAlert('Erfolgreich gespeichert!', 'success');

            // Neue Kontakte speichern.
            if (neueKontakte.length > 0) {
                await Promise.all(neueKontakte.map(daten =>
                    fetch('/bescheidbearbeitung/save-kontakt', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ ...daten, aktenzeichen })
                    }).then(r => r.json())
                ));
            }

            closeModal();

            if (typeof window.loadAblaufdatenPage === 'function') {
                window.loadAblaufdatenPage(1);
            }
        }

    } catch {
        await showAlert('Verbindungsfehler beim Speichern.', 'error');
    } finally {
        ladeAnzeige(false);
    }

    const objectId = history.state?.objectId;
    if (objectId) {
        window.location.href = `/bescheidbearbeitung?ObjectId=${objectId}`;
    }
}


// Lade-Anzeige für den Speichern-Button ein-/ausschalten. Parameter: aktiv — true = Spinner zeigen, false = Normal-Zustand.
function ladeAnzeige(aktiv) {
    const btn = document.getElementById('btnSaveAll');
    if (!btn) return;

    if (aktiv) {
        btn.disabled   = true;
        btn.innerHTML  = '<i class="fa-solid fa-spinner fa-spin"></i> Wird gespeichert…';
    } else {
        // Zustand nach Abschluss: Button-Status gemäß Validierung wiederherstellen.
        speichernButtonAktualisieren();
    }
}


// Vorgangsdaten aktualisieren (Edit-Modus).
async function updateVorgang(zulassung, aktenzeichen) {
    const btn = document.getElementById('btnSaveVorgang');

    const antragWert = document.querySelector('input[name="vorgang_antrag"]:checked')?.value;
    const bescheidWert = document.querySelector('input[name="vorgang_bescheid"]:checked')?.value;

    const antragVorhanden = antragWert === 'mit_antrag';
    const bescheidNeufassung = bescheidWert === 'neufassung';



    const geandertVon = document.getElementById('modalCurrentUser')?.value?.trim() ?? '';

    // Zulassungsnummer aus dem Hidden-Feld.
    const zulassungsnummer = document.getElementById('modalZulassungsnummerAkte')?.value ?? zulassung ?? '';

    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Wird gespeichert…';
    }
    const ansprechpartner = document.getElementById('inputAnsprechpartner')?.value ?? '';

    const payload = {
        aktenzeichen,
        zulassungsnummer,
        datumAntrag: document.getElementById('inputAntragsdatum')?.value || new Date().toISOString(),
        antragsart: document.getElementById('selectAntragsart')?.value ?? '',
        bescheidtyp: document.getElementById('selectVorgangstyp')?.value ?? '',
        gegenstand: document.getElementById('inputGegenstand')?.value ?? '',
        status: '???',
        betreff: document.getElementById('inputBetreff')?.value ?? '',
        bemerkung: document.getElementById('inputBemerkung')?.value ?? '',
        aktenzustand: '???',
        veroeffentlicht: false,
        erzeugtVon: document.getElementById('inputName')?.value ?? '',
        erzeugtAm: new Date().toISOString(),
        antragVorhanden,
        bescheidNeufassung,
        ansprechpartner,
        geandertVon,
        geandertAm: new Date().toISOString()
    };

    try {
        const response = await fetch(
            `/bescheidbearbeitung/update-vorgang?aktenzeichen=${encodeURIComponent(aktenzeichen)}`,
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector(
                        'input[name="__RequestVerificationToken"]'
                    )?.value ?? ''
                },
                body: JSON.stringify(payload)
            }
        );

        if (!response.ok) {
            await showAlert('Serverfehler beim Aktualisieren des Vorgangs.', 'error');
            return;
        }

        const result = await response.json();

        if (result.success) {
            if (btn) btn.innerHTML = '<i class="fa-solid fa-check"></i> Gespeichert';

            if (typeof window.loadAblaufdatenPage === 'function') {
                window.loadAblaufdatenPage(1);
            }

            setTimeout(() => {
                if (btn) {
                    btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Vorgangsdaten speichern';
                    validateTab1();
                }
            }, 1500);
        } else {
            await showAlert('Fehler: ' + (result.detail ?? result.error ?? 'Unbekannter Fehler'), 'error');
        }

    } catch {
        await showAlert('Verbindungsfehler beim Aktualisieren des Vorgangs.', 'error');
    } finally {
        if (btn && !btn.innerHTML.includes('check')) {
            btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Vorgangsdaten speichern';
            validateTab1();
        }
    }
}