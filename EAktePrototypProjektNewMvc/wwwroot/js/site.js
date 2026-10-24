
// Resize.
const resizer = document.getElementById('resizer');
const menu = document.getElementById('menu');
const content = document.querySelector('.content');

//To close nav.
const closeNav = document.getElementById('closeNav');

let isResizing = false;

// Start resizing.
if (resizer) {
    resizer.addEventListener('mousedown', function (e) {
        if (e.button !== 0) return;
        isResizing = true;
    });
}

// Resize.
document.addEventListener('mousemove', function (e) {
    if (!isResizing || e.buttons === 0) {
        isResizing = false;
        return;
    }

    let newWidth = e.clientX;

    if (newWidth < 100) newWidth = 100;
    if (newWidth > window.innerWidth - 100) newWidth = window.innerWidth - 100;

    menu.style.width = newWidth + 'px';
    content.style.marginLeft = newWidth + 'px';


});

// Stop resizing.
document.addEventListener('mouseup', function () {
    isResizing = false;
});

// Close nav.
if (closeNav) {
    closeNav.addEventListener("click", () => {
        let percent = window.innerWidth < 2000 ? 30 : 8;

        let width = window.innerHeight * (percent / 100);
        menu.style.width = width + "px";
    });
}


// Reload & rotate.
const buttonReload = document.getElementById("buttonReload");
const iconReload = document.getElementById("iconReload");

if (buttonReload) {
    buttonReload.addEventListener('click', () => {
        iconReload.classList.remove('rotated');

        setTimeout(() => {
            iconReload.classList.add('rotated');
        }, 10);

        setTimeout(() => {
            location.reload();
        }, 1000);
    }, 10);
}


// Toggle theme.
const toggleBtn = document.getElementById("toggleTheme");
const icon = document.getElementById("iconThemeToggle");

//if (!toggleBtn || !icon) return;

let rotation = 0;
const root = document.documentElement; // HTML.

const savedTheme = localStorage.getItem("theme");
if (savedTheme === "dark") {
    root.classList.add("dark-theme");
    icon.style.transform = "rotate(360deg)";
    rotation = 360;
}

// Toggle theme.
if (toggleBtn) {
    toggleBtn.addEventListener("click", () => {
        console.log("click");
        rotation += 360;
        icon.style.transform = `rotate(${rotation}deg)`;

        const isDark = root.classList.toggle("dark-theme");
        localStorage.setItem("theme", isDark ? "dark" : "light");
    })
}


// Admin mode.
const adminModeBtn = document.getElementById("admin-mode");

if (adminModeBtn) {
    adminModeBtn.addEventListener("click", async () => {
        try {
            const response = await fetch("/benutzer/toggle-admin-mode", {
                method: "POST",
                headers: { "Content-Type": "application/json" }
            });
            if (!response.ok) throw new Error("Server error");
            const data = await response.json(); // { adminMode: true/false }
            const isActive = data.adminMode;
            adminModeBtn.classList.toggle("active", isActive);
            adminModeBtn.querySelectorAll("*").forEach(child => {
                child.classList.toggle("active", isActive);
            });
            location.reload();
        } catch (err) {
            console.error("Can`t toggle admin mode:", err);
        }
    });
}




