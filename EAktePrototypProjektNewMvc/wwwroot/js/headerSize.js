function updateHeaderHeight() {
    const header = document.querySelector('header');
    let height = header.getBoundingClientRect().height;

    //Fix for small resolutions.
    if (window.innerWidth < 1920) {
        height = Math.ceil(height); 
    }

    document.documentElement.style.setProperty(
        '--header-height',
        height + 'px'
    );
}

window.addEventListener('load', updateHeaderHeight);
window.addEventListener('resize', updateHeaderHeight);
