(() => {
    const form = document.getElementById('bookverse-login');
    if (!form) return;
    const password = document.getElementById('Password');
    const toggle = document.getElementById('toggle-password');
    const submit = document.getElementById('login-submit');
    const label = document.getElementById('login-button-text');
    const progress = document.getElementById('login-progress');
    toggle.hidden = false;
    toggle.addEventListener('click', () => {
        const visible = password.type === 'password';
        password.type = visible ? 'text' : 'password';
        toggle.textContent = visible ? 'Hide' : 'Show';
        toggle.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');
        toggle.setAttribute('aria-pressed', String(visible));
    });
    form.addEventListener('submit', event => {
        if (event.defaultPrevented || !form.checkValidity() || (window.jQuery && window.jQuery.validator && !window.jQuery(form).valid())) return;
        submit.disabled = true;
        form.setAttribute('aria-busy', 'true');
        label.textContent = 'Signing in…';
        progress.textContent = 'Creating your secure session…';
    });
    window.addEventListener('pageshow', () => {
        submit.disabled = false;
        form.removeAttribute('aria-busy');
        label.textContent = 'Sign in';
        progress.textContent = '';
        password.type = 'password';
    });
})();
