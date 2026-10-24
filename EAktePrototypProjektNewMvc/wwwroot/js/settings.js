// Font size.
const fontSelect = document.getElementById("fontSizeSelector");

// Load.
const savedFont = localStorage.getItem('fontSize');
if (savedFont) {
    document.documentElement.style.fontSize = savedFont + 'rem';
    fontSelect.value = savedFont;
}

// Change.
fontSelect.addEventListener('change', () => {
    const fontSize = fontSelect.value;
    document.documentElement.style.fontSize = fontSize + 'rem';
    localStorage.setItem('fontSize', fontSize);
    updateHeaderHeight();
});

// Toggle settings.
const toggleSettingsBtn = document.getElementById("toggleSettings");
const settingsContainer = document.getElementById("settingsContainer");

if (toggleSettingsBtn && settingsContainer) {
    toggleSettingsBtn.addEventListener("click", () => {
        settingsContainer.classList.toggle("active");
    });
}

// Click outside.
document.addEventListener('click', (e) => {
    if (!settingsContainer || !toggleSettingsBtn) return;
    if (!settingsContainer.contains(e.target) && !toggleSettingsBtn.contains(e.target)) {
        settingsContainer.classList.remove('active');
    }
});

