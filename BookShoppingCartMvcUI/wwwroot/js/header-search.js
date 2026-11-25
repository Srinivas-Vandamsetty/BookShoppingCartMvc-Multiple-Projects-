(() => {
    const search = document.querySelector('[data-book-search]');
    if (!search) return;

    const toggle = search.querySelector('.bookverse-search__toggle');
    const panel = search.querySelector('.bookverse-search__panel');
    const input = search.querySelector('#headerBookSearch');
    const suggestions = search.querySelector('#bookSearchSuggestions');
    let requestController;
    let debounceTimer;

    const close = () => {
        panel.hidden = true;
        toggle.setAttribute('aria-expanded', 'false');
        suggestions.replaceChildren();
    };

    const open = () => {
        panel.hidden = false;
        toggle.setAttribute('aria-expanded', 'true');
        input.focus();
    };

    const showMessage = message => {
        const item = document.createElement('p');
        item.className = 'bookverse-search__message';
        item.textContent = message;
        suggestions.replaceChildren(item);
    };

    const render = books => {
        suggestions.replaceChildren();
        if (!books.length) {
            showMessage('No matching books found.');
            return;
        }

        for (const book of books) {
            const link = document.createElement('a');
            link.className = 'bookverse-search__result';
            link.href = `/Book/BookDetails?id=${encodeURIComponent(book.id)}`;
            link.setAttribute('role', 'option');

            const image = document.createElement('img');
            image.alt = '';
            image.src = book.image ? `/images/${encodeURIComponent(book.image)}` : '/images/NoImage.png';
            image.onerror = () => { image.src = '/images/NoImage.png'; };

            const text = document.createElement('span');
            const title = document.createElement('strong');
            title.textContent = book.bookName;
            const author = document.createElement('small');
            author.textContent = book.authorName;
            text.append(title, author);
            link.append(image, text);
            suggestions.append(link);
        }
    };

    const searchBooks = async () => {
        const term = input.value.trim();
        if (term.length < 2) {
            suggestions.replaceChildren();
            return;
        }

        requestController?.abort();
        requestController = new AbortController();
        showMessage('Searching…');

        try {
            const response = await fetch(`/Home/HeaderSearch?term=${encodeURIComponent(term)}`, {
                signal: requestController.signal,
                headers: { Accept: 'application/json' }
            });
            if (!response.ok) throw new Error('Search request failed');
            render(await response.json());
        } catch (error) {
            if (error.name !== 'AbortError') showMessage('Search is unavailable. Please try again.');
        }
    };

    toggle.addEventListener('click', () => panel.hidden ? open() : close());
    input.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(searchBooks, 250);
    });
    input.addEventListener('keydown', event => {
        if (event.key === 'Escape') close();
    });
    document.addEventListener('click', event => {
        if (!search.contains(event.target)) close();
    });
})();
