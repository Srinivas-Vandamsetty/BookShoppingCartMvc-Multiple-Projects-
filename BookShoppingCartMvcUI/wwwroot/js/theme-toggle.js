(() => {
    const storageKey = "bookverse-theme";
    const root = document.documentElement;
    const button = document.querySelector("[data-theme-toggle]");

    const preferredTheme = () => {
        try {
            return localStorage.getItem(storageKey) || document.body.dataset.themeDefault || "light";
        } catch {
            return document.body.dataset.themeDefault || "light";
        }
    };

    const applyTheme = (theme) => {
        root.dataset.theme = theme;
        const darkMode = theme === "dark";
        if (!button) return;

        const icon = button.querySelector("i");
        icon?.classList.toggle("bi-moon-stars-fill", !darkMode);
        icon?.classList.toggle("bi-sun-fill", darkMode);
        const nextMode = darkMode ? "light" : "dark";
        button.setAttribute("aria-label", `Switch to ${nextMode} mode`);
        button.setAttribute("title", `Switch to ${nextMode} mode`);
        button.setAttribute("aria-pressed", String(darkMode));
    };

    applyTheme(preferredTheme());
    button?.addEventListener("click", () => {
        const nextTheme = root.dataset.theme === "dark" ? "light" : "dark";
        try { localStorage.setItem(storageKey, nextTheme); } catch { /* Storage can be disabled by the browser. */ }
        applyTheme(nextTheme);
    });
})();
