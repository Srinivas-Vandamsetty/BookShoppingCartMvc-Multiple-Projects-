const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const script = fs.readFileSync(path.resolve(__dirname, '../../BookShoppingCartMvcUI/wwwroot/js/login.js'), 'utf8');
function page(valid = true) {
    function element() {
        return { handlers: {}, attributes: {}, addEventListener(name, fn) { this.handlers[name] = fn; },
            setAttribute(name, value) { this.attributes[name] = value; }, removeAttribute(name) { delete this.attributes[name]; } };
    }
    const elements = Object.fromEntries(['bookverse-login','Input_Password','toggle-password','login-submit','login-button-text','login-progress'].map(id => [id,element()]));
    elements['bookverse-login'].checkValidity = () => valid;
    elements.Input_Password.type = 'password';
    elements['login-submit'].disabled = false;
    const window = element();
    vm.runInNewContext(script, { document: { getElementById: id => elements[id] }, window });
    return { elements, window };
}
test('password visibility has accessible state and resets after browser back navigation', () => {
    const { elements:e, window } = page();
    e['toggle-password'].handlers.click();
    assert.equal(e.Input_Password.type, 'text');
    assert.equal(e['toggle-password'].attributes['aria-pressed'], 'true');
    window.handlers.pageshow();
    assert.equal(e.Input_Password.type, 'password');
    assert.equal(e['toggle-password'].attributes['aria-label'], 'Show password');
});
test('invalid submissions leave the button available for correction', () => {
    const { elements:e } = page(false);
    e['bookverse-login'].handlers.submit({defaultPrevented:false});
    assert.equal(e['login-submit'].disabled, false);
    assert.equal(e['bookverse-login'].attributes['aria-busy'], undefined);
});
test('valid submit shows progress and back navigation restores an interactive form', () => {
    const { elements:e, window } = page();
    e['bookverse-login'].handlers.submit({defaultPrevented:false});
    assert.equal(e['login-submit'].disabled, true);
    assert.equal(e['bookverse-login'].attributes['aria-busy'], 'true');
    assert.equal(e['login-button-text'].textContent, 'Signing in…');
    window.handlers.pageshow();
    assert.equal(e['login-submit'].disabled, false);
    assert.equal(e['login-button-text'].textContent, 'Sign in');
});
