// Wiedervorlage
const wvState = { objectId: null, title: "" };

// HTML-Elemente des Overlays.
const wvEls = {
    overlay: document.getElementById("wvOverlay"),
    date: document.getElementById("wvDate"),
    time: document.getElementById("wvTime"),
    remark: document.getElementById("wvRemark"),
    submit: document.getElementById("wvSubmit"),
    cancel: document.getElementById("wvCancel")
};

// Öffnet das Wiedervorlage-Overlay und setzt die Vorbelegung.
function openWiedervorlage() {
    wvState.objectId = currentObjectId;

    const row = findTableRow(wvState.objectId);
    wvState.title = row?.dataset.filename || row?.dataset.name || "Dokument";

    // Vorbelegung: morgen, 08:00 Uhr.
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    wvEls.date.value = tomorrow.toISOString().slice(0, 10);
    wvEls.time.value = "08:00";
    wvEls.remark.value = "";

    wvEls.overlay.classList.add("open");
    wvEls.date.focus();
}

// Schließt das Wiedervorlage-Overlay.
function closeWiedervorlage() {
    wvEls.overlay.classList.remove("open");
}

// Erzeugt die ICS-Datei serverseitig und stößt den Download an.
async function submitWiedervorlage() {
    const date = wvEls.date.value;
    if (!date) {
        alert("Bitte ein Datum wählen.");
        return;
    }

    wvEls.submit.disabled = true;
    try {
        const res = await fetch("/bescheidbearbeitung/create-wiedervorlage", {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                objectId: wvState.objectId,
                title: wvState.title,
                date: date,
                time: wvEls.time.value || "08:00",
                remark: wvEls.remark.value
            })
        });

        if (!res.ok) {
            alert("Wiedervorlage konnte nicht erstellt werden.");
            return;
        }

        downloadBlob(await res.blob(), "Wiedervorlage.ics");
        closeWiedervorlage();
    } catch (err) {
        console.error(err);
        alert("Fehler beim Erstellen der Wiedervorlage.");
    } finally {
        wvEls.submit.disabled = false;
    }
}

// Löst den Download einer Blob-Datei aus.
function downloadBlob(blob, filename) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
}

// Klick auf Wiedervorlage im Kontextmenü öffnet das Overlay.
document.querySelector("#contextMenu .calendar").addEventListener("click", function (e) {
    e.stopPropagation();
    closeContextMenu();
    openWiedervorlage();
});

// Submit/ Cancel.
wvEls.submit.addEventListener("click", submitWiedervorlage);
wvEls.cancel.addEventListener("click", closeWiedervorlage);


// Klick außerhalb des Overlays schließt es.
wvEls.overlay.addEventListener("click", function (e) {
    if (e.target === this) closeWiedervorlage();
});

// Escape-Taste schließt das Overlay.
document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") closeWiedervorlage();
});