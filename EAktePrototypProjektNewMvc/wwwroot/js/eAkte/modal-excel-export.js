function exportTable(type) {
    var tableId = type === 'kontakte' ? 'kontakteTable' : 'ablaufdatenTable';
    var filename = type === 'kontakte' ? 'Kontakte.xlsx' : 'Ablaufdaten.xlsx';
    var editRowClass = type === 'kontakte' ? '.k-row-edit' : '.a-row-edit';

    var table = document.getElementById(tableId);
    if (!table) return;

    var clone = table.cloneNode(true);

    // Suchzeile (erste tr im thead) entfernen.
    var thead = clone.querySelector('thead');
    if (thead && thead.rows.length > 0) {
        thead.deleteRow(0);
    }

    // Edit-Zeilen entfernen.
    clone.querySelectorAll(editRowClass).forEach(row => row.remove());

    // Aktionen-Spalte (letzte) aus jeder Zeile entfernen.
    clone.querySelectorAll('tr').forEach(row => {
        if (row.children.length > 0) {
            row.removeChild(row.lastElementChild);
        }
    });

    var workbook = XLSX.utils.table_to_book(clone, { sheet: 'Sheet1' });
    XLSX.writeFile(workbook, filename);
}