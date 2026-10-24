$(document).ajaxError(function (event, xhr, settings, error) {
    if (xhr.status === 409) return;
    console.error("AJAX error:", {
        url: settings.url,
        status: xhr.status,
        response: xhr.responseText
    });

    if (xhr.status === 401) {
        // If on Login page.
        if (window.location.pathname.toLowerCase().includes("login")) {
            return;
        }
        // Session expired - redirect to login.
        window.location.replace("/Login/Login");
        return;
    }

    if (xhr.status === 403) {
        // Access denied.
        alert("You do not have permission to perform this action.");
        return;
    }
});