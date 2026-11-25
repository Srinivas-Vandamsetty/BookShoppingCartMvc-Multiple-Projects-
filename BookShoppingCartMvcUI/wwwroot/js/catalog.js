(() => {
    const grid = document.getElementById('catalog-grid');
    if (!grid) return;
    const buttons = document.querySelectorAll('[data-catalog-view]');
    const savedView = localStorage.getItem('bookverse-catalog-view') || 'grid';
    const setView = view => {
        grid.classList.toggle('is-list', view === 'list');
        buttons.forEach(button => button.classList.toggle('is-active', button.dataset.catalogView === view));
        localStorage.setItem('bookverse-catalog-view', view);
    };
    buttons.forEach(button => button.addEventListener('click', () => setView(button.dataset.catalogView)));
    setView(savedView);
})();
