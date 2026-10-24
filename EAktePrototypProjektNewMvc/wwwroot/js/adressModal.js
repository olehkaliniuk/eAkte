//Modal.
document.addEventListener("DOMContentLoaded", function () {

    const modalAdresse = document.getElementById("adresseModal");
    if (!modalAdresse) return; 
    const header = modalAdresse.querySelector(".modal-header");

    // Open.
    window.toggleAdresseModal = function () {
        const btn = document.getElementById("btnOpenAdress");
        if (modalAdresse.style.display === "block") {
            modalAdresse.style.display = "none";
            btn.classList.remove('active'); // deaktivieren.
        } else {
            modalAdresse.style.display = "block";
            btn.classList.add('active'); // aktivieren.
            loadAdressen();
        }
    }

    // Close.
    document.getElementById("closeAdresseModal").onclick = () => {
        modalAdresse.style.display = "none";
        document.getElementById("btnOpenAdress").classList.remove('active');
    };

    // Drag.
    function initDrag(modal) {
        const header = modal.querySelector('.modal-header');
        if (!header) return;

        let dragging = false;
        let offsetX = 0;
        let offsetY = 0;

        header.addEventListener('mousedown', e => {
            dragging = true;
            offsetX = e.clientX - modal.offsetLeft;
            offsetY = e.clientY - modal.offsetTop;
        });

        document.addEventListener('mousemove', e => {
            if (!dragging) return;
            modal.style.left = (e.clientX - offsetX) + 'px';
            modal.style.top = (e.clientY - offsetY) + 'px';
        });

        document.addEventListener('mouseup', () => {
            dragging = false;
        });
    }

    initDrag(modalAdresse);

});

//Load table with pagination.
function loadAdressen() {
    loadPageA(1)
}

function getVal(id) {
    const el = document.getElementById(id);
    return el ? el.value : "";
}

// Pagination and search.
async function loadPageA(page = 1, sortColumn = null, sortDirection = null) {

    const pageSize = parseInt(
        document.getElementById('pageSizeSelectAdresse')?.value || 10, 10
    );

    const currentSortColumn =
        sortColumn || document.getElementById("currentSortColumnAdresse")?.value || "Id";

    const currentSortDirection =
        sortDirection || document.getElementById("currentSortDirectionAdresse")?.value || "asc";

    const query = new URLSearchParams({
        Id: getVal('searchId'),
        Land: getVal('searchLand'),
        Strasse: getVal('searchStrasse'),
        Ort: getVal('searchOrt'),
        StateOrPrefecture: getVal('searchState'),
        PLZ: getVal('searchPLZ'),
        Gebaude: getVal('searchGebaude'),
        Wohnung: getVal('searchWohnung'),
        Organisation: getVal('searchOrganisation'),
        Name: getVal('searchName'),
        Bezeichnung: getVal('searchBezeichnung'),
        Iban: getVal('searchIban'),
        Bic: getVal('searchBic'),
        Ansprechpartner: getVal('searchAnsprechpartner'),
        AnsprechpartnerTel: getVal('searchTel'),
        AnsprechpartnerEmail: getVal('searchEmail'),
        RechnungsStrasse: getVal('searchReStrasse'),
        RechnungsOrt: getVal('searchReOrt'),
        RechnungsPLZ: getVal('searchRePLZ'),
        RechnungsLand: getVal('searchReLand'),
        RechnungsStateOrPrefecture: getVal('searchReState'),
        RechnungsOrganisation: getVal('searchReOrg'),
        RechnungsName: getVal('searchReName'),
        Gesamtadresse: getVal('searchGesamt'),
        Fax: getVal('searchFax'),
        IstInsolvent: getVal('searchIstInsolvent'),
        IstAktiv: getVal('searchIstAktiv'),
        page: page,
        pageSize: pageSize,
        sortColumn: currentSortColumn,
        sortDirection: currentSortDirection
    });

    try {
        const response = await fetch(`/address/load-address?${query.toString()}`);
        if (!response.ok) {
            await showAlert("Fehler beim Laden der Adressen.", "error");
            return;
        }
        const html = await response.text();
        document.getElementById('adresseModalContent').innerHTML = html;
        attachEventsAdresse();
    } catch {
        await showAlert("Netzwerkfehler beim Laden der Adressen.", "error");
    }
}

function sortTableAdresse(column) {

    const currentColumn =
        document.getElementById("currentSortColumnAdresse")?.value;

    const currentDirection =
        document.getElementById("currentSortDirectionAdresse")?.value || "asc";

    const newDirection =
        currentColumn === column && currentDirection === "asc"
            ? "desc"
            : "asc";

    loadPageA(1, column, newDirection);
}

// Events pagiantion.
function attachEventsAdresse() {

    const inputs = document.querySelectorAll('.search input');
    const pageSizeSelect = document.getElementById('pageSizeSelectAdresse');
    const resetBtn = document.getElementById('resetBtnAdresse');
    const currentPage = document.getElementById('currentPageAdresse');

    // Auto search for select (bool filters)
    const selects = document.querySelectorAll('.search select');

    selects.forEach(select => {
        select.removeEventListener('change', handleSelectChange);
        select.addEventListener('change', handleSelectChange);
    });

    function handleSelectChange() {
        loadPageA(1);
    }

    // ENter search.
    inputs.forEach(input => {
        input.removeEventListener('keypress', handleSearch); 
        input.addEventListener('keypress', handleSearch);
    });

    function handleSearch(e) {
        if (e.key === 'Enter') {
            loadPageA(1);
        }
    }

    // pageSize.
    if (pageSizeSelect) {
        pageSizeSelect.removeEventListener('change', handlePageSize);
        pageSizeSelect.addEventListener('change', handlePageSize);
    }

    function handlePageSize() {
        loadPageA(1);
    }

    // currentPage.
    if (currentPage) {
        currentPage.removeEventListener('input', handlePageInput);
        currentPage.addEventListener('input', handlePageInput);
    }

    function handlePageInput() {
        let page = parseInt(currentPage.value, 10);
        const max = parseInt(currentPage.max, 10);

        if (!page || page < 1) page = 1;
        if (page > max) page = max;

        currentPage.value = page;
        loadPageA(page);
    }

    // Reset.
    if (resetBtn) {
        resetBtn.removeEventListener('click', handleReset);
        resetBtn.addEventListener('click', handleReset);
    }

    function handleReset() {

        // Clean inputs.
        inputs.forEach(i => i.value = "");

        // Clean select.
        document.querySelectorAll('.search select')
            .forEach(s => s.value = "");

        if (pageSizeSelect)
            pageSizeSelect.value = 10;

        // Reset sorting to default
        loadPageA(1, "Id", "asc");
    }
}

// Adresse auswählen und in aktive Antragsteller-Blöcke einfügen.
function selectAdresse(id) {
    // Nur ausführen, wenn das Modal (CreateAkte) sichtbar ist (nicht im Hintergrund).
    const fileModal = document.getElementById("fileModal");
    if (fileModal && window.getComputedStyle(fileModal).display === "none") {
        return;
    }


    fetch(`/bescheidbearbeitung/get-address-by-id/${id}`)
        .then(res => {
            if (!res.ok) {
                showAlert("Fehler beim Laden der Adresse.", "error");
                return null;
            }
            return res.json();
        })
        .then(data => {
            if (!data) return;

            // Alle aktivierten Antragsteller-Blöcke befüllen.
            document.querySelectorAll('.applicant-block').forEach(block => {
                const checkbox = block.querySelector('input[type="checkbox"]');
                if (!checkbox.checked) return;

                block.querySelector('.antragsteller-adresse').value =
                    `${data.strasse} ${data.gebaude}, ${data.plz} ${data.ort}, ${data.land}`;
                block.querySelector('.antragsteller-adresszusatz').value =
                    data.organisation ?? '';
                block.querySelector('.antragsteller-tel').value =
                    data.ansprechpartnerTel ?? '';
                block.querySelector('.antragsteller-fax').value =
                    data.fax ?? '';
                block.querySelector('.antragsteller-email').value =
                    data.ansprechpartnerEmail ?? '';
            });

            validateTab2();

            // close.
            const modalAdresse = document.getElementById("adresseModal");
            modalAdresse.style.display = "none";
            document.getElementById("btnOpenAdress").classList.remove('active');
        })
        .catch(() => {
            showAlert("Netzwerkfehler beim Laden der Adresse.", "error");
        });
}