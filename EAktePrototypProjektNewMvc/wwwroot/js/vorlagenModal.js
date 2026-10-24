const modalVorlagen = document.getElementById("VorlagenCont");
const tableContainer = document.getElementById("tableContainer");

// Close modal when clicking outside of it.
document.addEventListener("click", function (e) {

    const node = e.target.closest(".js-tree-node");

    if (!node) return;

    e.preventDefault();

    modalVorlagen.style.display = "none";
    tableContainer.style.display = "block";
    document.getElementById("btnOpenVorlagen").classList.remove('active');

});

// Toggle modal.
function toggleVorlagenModal() {

    const frame = document.getElementById("vorlagenFrame");

    if (modalVorlagen.style.display === "block") {

        modalVorlagen.style.display = "none";
        tableContainer.style.display = "block";

    } else {
        tableContainer.style.display = "none";
        modalVorlagen.style.display = "block";

        if (!frame.src) {
            frame.src = "https://dreif.com/vorlagenverwaltung";
        }
    }
}

