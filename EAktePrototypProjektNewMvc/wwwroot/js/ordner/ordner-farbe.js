// Global – werden direkt aus HTML aufgerufen.
function togglePanel() {
    document.getElementById('colorPanel').classList.toggle('open');
}

// Verarbeitet die Eingabe eines Hex-Farbwerts.
function onHexInput(val) {
    if (/^#[0-9A-Fa-f]{6}$/.test(val)) selectColor(val);
}

// Wählt eine Farbe aus der Palette aus.
function selectColor(hex) {
    if (!/^#[0-9A-Fa-f]{6}$/.test(hex)) return;
    document.getElementById('mainSwatch').style.background = hex;
    document.getElementById('mainHex').textContent = hex.toUpperCase();
    document.getElementById('hexInput').value = hex.toUpperCase();
    document.getElementById('inputFarbe').value = hex;
    document.getElementById('nativeColor').value = hex;
    document.querySelectorAll('.palette-swatch').forEach(s => {
        s.classList.toggle('selected',
            s.style.background === hexToRgb(hex) ||
            s.title.toLowerCase() === hex.toLowerCase()
        );
    });
}

// Konvertiert einen Hex-Farbwert in RGB.
function hexToRgb(hex) {
    const r = parseInt(hex.slice(1, 3), 16), g = parseInt(hex.slice(3, 5), 16), b = parseInt(hex.slice(5, 7), 16);
    return `rgb(${r}, ${g}, ${b})`;
}

// Wird nach dem Fetch aufgerufen, wenn DOM bereit ist.
function initColorPicker() {
    const palette = [
        "#E24B4A", "#D85A30", "#EF9F27", "#639922", "#1D9E75", "#178ADD", "#534AB7", "#D4537E", "#888780",
        "#F09595", "#F0997B", "#FAC775", "#97C459", "#5DCAA5", "#85B7EB", "#AFA9EC", "#ED93B1", "#B4B2A9",
        "#B8342F", "#A8471F", "#C77D14", "#43701A", "#137557", "#125E9E", "#3A3383", "#A6355C", "#5F5E58",
        "#7A2A28", "#7A3A1A", "#8C6318", "#375A1E", "#1C5642", "#1F4A75", "#463F8F", "#7A3350", "#4A4944"
    ];

    const grid = document.getElementById('paletteGrid');
    if (!grid) return;
    grid.innerHTML = '';

    palette.forEach(c => {
        const s = document.createElement('div');
        s.className = 'palette-swatch';
        s.style.background = c;
        s.title = c;
        s.onclick = () => selectColor(c);
        grid.appendChild(s);
    });

    const currentColor = document.getElementById('inputFarbe')?.value || '#4F8EF7';
    selectColor(currentColor);

    document.removeEventListener('click', colorPickerOutsideClick);
    document.addEventListener('click', colorPickerOutsideClick);
}

// Schließt das Farbauswahl-Panel, wenn außerhalb geklickt wird.
function colorPickerOutsideClick(e) {
    const wrapper = document.getElementById('pickerWrapper');
    if (wrapper && !wrapper.contains(e.target))
        document.getElementById('colorPanel')?.classList.remove('open');
}