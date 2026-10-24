// Toggle table info.
function toggleInfo(id, event) {
    event.stopPropagation();
    event.preventDefault();

    const row = document.getElementById('info-' + id);
    if (!row) return;

    const isTableRow = row.tagName === "TR";

    if (row.style.display === "none") {
        row.style.display = isTableRow ? "table-row" : "block";
    } else {
        row.style.display = "none";
    }
}

// Toggle versionen row for dokument.
function toggleAlleVersionen(objektId, clickedBtn) {
    const block = document.getElementById('versionen-block-' + objektId);
    const expandBar = document.getElementById('versionen-expand-' + objektId);
    const toggleBtn = document.querySelector(`.versionen-toggle-btn[onclick*="${objektId}"]`);
    const isExpanded = clickedBtn.classList.contains('expanded');

    const extraDataRows = block.querySelectorAll('tr.versionen-extra-row:not(.info-row)');
    extraDataRows.forEach(row => {
        row.style.display = isExpanded ? 'none' : '';
    });

    const extraInfoRows = block.querySelectorAll('tr.info-row.versionen-extra-row');
    extraInfoRows.forEach(row => {
        row.style.display = 'none';
    });

    const totalCount = extraDataRows.length + 1;

    [clickedBtn, expandBar, toggleBtn].forEach(el => {
        if (!el) return;
        el.classList.toggle('expanded', !isExpanded);
        if (el === expandBar) {
            el.querySelector('.versionen-expand-label').textContent = isExpanded
                ? `Alle ${totalCount} Versionen anzeigen`
                : 'Weniger anzeigen';
        }
    });
}