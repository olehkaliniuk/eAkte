// Globale Variable: ist das Modal aktuell geöffnet?
let isModalOpen = false;
// Akte oder Vorgang.
let currentObjectType = '';
// Edit oder Create.
let currentMode = '';

// Lädt den HTML-Inhalt per AJAX vom Server und zeigt das Modal an.
async function openModal(objectType, mode, objectId) {
    // Verhindert doppeltes Öffnen, falls Modal bereits sichtbar ist.
    if (isModalOpen) return;

    currentObjectType = objectType; // Objekt-Typ speichern ("akte" oder "vorgang").
    currentMode = mode;             // Modus speichern ("edit" oder "create").

    // ObjectId aus der aktuellen URL auslesen oder den übergebenen Wert verwenden.
    const resolvedObjectId = objectId ?? new URLSearchParams(window.location.search).get("ObjectId");

    const params = new URLSearchParams({
        ObjectId: resolvedObjectId,
        objectType: currentObjectType,
        mode: currentMode
    });

    // Modal-HTML vom Server laden.
    try {
        const response = await fetch(`/bescheidbearbeitung/get-modal-data-eakte?${params.toString()}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden des Modals.", "error");
            return;
        }

        const html = await response.text();

        // HTML in den vorbereiteten Container einfügen.
        const container = document.getElementById('modalAkteContainer');
        if (!container) {
            await showAlert("Modal-Container nicht gefunden.", "error");
            return;
        }
        container.innerHTML = html;

        // Alle <script> im geladenen HTML erneut ausführen.
        container.querySelectorAll('script').forEach(oldScript => {
            const newScript = document.createElement('script');
            newScript.textContent = oldScript.textContent;
            document.body.appendChild(newScript);
            document.body.removeChild(newScript);
        });

        // Modal-Element suchen.
        const modal = document.getElementById('fileModal');
        if (!modal) {
            await showAlert("Modal-Element nicht gefunden.", "error");
            return;
        }

        // Modal sichtbar machen.
        modal.style.display = 'flex';
        isModalOpen = true;

        // Drag-Funktion initialisieren.
        initDrag(modal);

        // Teilbereiche des Modals initialisieren.
        initAntragsteller(); // Tab 2: Antragsteller-Logik
        initKontakte();      // Tab 3: Kontakte-Tabelle
        initAblaufdaten();   // Tab 3: Ablaufdaten-Tabelle
        initGebuehren();  // Tab 4: Gebühren-Tabelle
        updateTabButtons();  // Tab-Buttons-Farbe initialisieren.

        if (currentMode == "edit") {
            validateTab1();
            validateTab2();
        }

    } catch {
        await showAlert("Netzwerkfehler beim Laden des Modals.", "error");
    }
}

// Modal schließen. Versteckt das Modal und setzt den Zustand zurück.
function closeModal() {
    validateTab1();
    validateTab2();

    document.getElementById('fileModal').style.display = 'none';
    document.getElementById('btnEditV').classList.remove('active'); // Edit-Button Vorgang zurücksetzen.
    isModalOpen = false;
}

// Drag-and-Drop initialisieren.
function initDrag(modal) {
    const header = modal.querySelector('#modalHeader');
    if (!header) return;

    let dragging = false; // Wird gerade gezogen?
    let offsetX = 0;      // Mausposition relativ zur Modal-Ecke (X).
    let offsetY = 0;      // Mausposition relativ zur Modal-Ecke (Y).

    // Ziehen beginnen: Offset berechnen.
    header.addEventListener('mousedown', e => {
        dragging = true;
        offsetX = e.clientX - modal.offsetLeft;
        offsetY = e.clientY - modal.offsetTop;
    });

    // Modal während des Ziehens verschieben.
    document.addEventListener('mousemove', e => {
        if (!dragging) return;
        modal.style.left = (e.clientX - offsetX) + 'px';
        modal.style.top  = (e.clientY - offsetY) + 'px';
    });

    // Ziehen beenden.
    document.addEventListener('mouseup', () => {
        dragging = false;
    });
}

// Tab wechseln.
function switchTab(name, btn) {
    const tabIndex = ['process', 'applicant', 'contacts'].indexOf(name) + 1;

    if (tabIndex === 2 && !tab1Fertig) return;
    if (tabIndex === 3 && (!tab1Fertig || !tab2Fertig)) return;

    // Alle Tab-Inhalte ausblenden.
    document.querySelectorAll('.tab-panel').forEach(el => el.classList.remove('active'));

    // Gewählten Tab-Inhalt einblenden.
    document.getElementById('tab-' + name).classList.add('active');

    // Alle Tab-Buttons deaktivieren.
    document.querySelectorAll('.modal-tab-btn').forEach(el => el.classList.remove('active'));

    // Gewählten Tab-Button aktivieren.
    btn.classList.add('active');
}

// Tab-Buttons sperren oder freischalten(css).
function updateTabButtons() {
    const tabs = ['process', 'applicant', 'contacts'];
    const allowed = [true, tab1Fertig, tab1Fertig && tab2Fertig];
    tabs.forEach((name, i) => {
        const btn = document.getElementById('tab-btn-' + name);
        if (!btn) return; 
        btn.classList.toggle('disabled', !allowed[i]);
    });
}

// Zu Tab "Antragsteller" wechseln.
function goToApplicant() {
    // Pflichtfeld-Prüfung für Tab 1.
    if (!tab1Fertig) {
        zeigeHinweisTab1();
        return;
    }
    // Tab-Button für "Antragsteller" programmatisch klicken.
    document.querySelector('.modal-tab-btn[onclick*="applicant"]').click();
}

// Zu Tab "Kontakte" wechseln.
function goToContacts() {
    // Pflichtfeld-Prüfung für Tab 2.
    if (!tab2Fertig) {
        zeigeHinweisTab2();
        return;
    }
    // Tab-Button für "Kontakte" programmatisch klicken.
    document.querySelector('.modal-tab-btn[onclick*="contacts"]').click();
}



