async function initMandantSwitcher() {
    try {
        const response = await fetch('/Mandant/GetAllMandanten');
    if (!response.ok) return;

    const mandanten = await response.json();
    const select = document.getElementById('mandantSelect');

    mandanten.forEach(m => {
        const option = document.createElement('option');
        option.value = m.id;
        option.textContent = m.name;
        select.appendChild(option);
    });

    // Aktuell aktiven Mandanten abfragen und im Dropdown vorauswählen.
    const activeResponse = await fetch('/Mandant/GetActiveMandant');
    if (activeResponse.ok) {
        const active = await activeResponse.json();
        select.value = active.mandantId;
    }

    document.getElementById('mandant-switcher').style.display = 'block';

    select.addEventListener('change', async () => {
        await fetch('/Mandant/SwitchMandant', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `mandantId=${select.value}`
        });
        window.location.href = window.location.pathname;
    });
    } catch (err) {
        console.error('Mandant-Switcher konnte nicht geladen werden:', err);
    }
}

initMandantSwitcher();
