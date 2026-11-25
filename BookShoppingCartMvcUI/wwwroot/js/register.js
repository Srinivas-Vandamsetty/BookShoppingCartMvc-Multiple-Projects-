(() => {
    const form = document.getElementById('bookverse-register');
    if (!form) return;
    const password = document.getElementById('Input_Password');
    const bars = document.querySelector('.bv-register__strength-bars');
    const strength = document.getElementById('password-strength');
    const submit = document.getElementById('register-submit');
    const buttonText = document.getElementById('register-button-text');
    const progress = document.getElementById('register-progress');
    const assessPassword = value => {
        if (!value) return { score: 0, label: 'Enter a password' };
        let score = 0;
        if (value.length >= 8) score++;
        if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score++;
        if (/\d/.test(value)) score++;
        if (/[^A-Za-z0-9]/.test(value)) score++;
        return { score, label: ['Very weak', 'Weak', 'Fair', 'Strong', 'Very strong'][Math.max(score - 1, 0)] };
    };
    password.addEventListener('input', () => {
        const result = assessPassword(password.value);
        bars.dataset.score = String(result.score);
        strength.textContent = result.label;
    });
    document.querySelectorAll('[data-password-toggle]').forEach(toggle => {
        toggle.addEventListener('click', () => {
            const input = document.getElementById(toggle.dataset.passwordToggle);
            const visible = input.type === 'password';
            input.type = visible ? 'text' : 'password';
            toggle.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');
            toggle.setAttribute('aria-pressed', String(visible));
            toggle.querySelector('i').className = visible ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });
    form.addEventListener('submit', event => {
        if (event.defaultPrevented || !form.checkValidity() || (window.jQuery && window.jQuery.validator && !window.jQuery(form).valid())) return;
        submit.disabled = true;
        form.setAttribute('aria-busy', 'true');
        buttonText.textContent = 'Creating account…';
        progress.textContent = 'Setting up your reading club account…';
    });
    window.addEventListener('pageshow', () => {
        submit.disabled = false;
        form.removeAttribute('aria-busy');
        buttonText.textContent = 'Create free account';
        progress.textContent = '';
    });
})();
