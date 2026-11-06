const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const script = fs.readFileSync(path.resolve(__dirname, '../../BookShoppingCartMvcUI/wwwroot/js/bookCart.js'), 'utf8');

async function page(signedIn, fetcher) {
    let ready;
    const button = { dataset: { bookId: '7' }, disabled: false, textContent: 'Add to Cart',
        addEventListener(event, handler) { if (event === 'click') this.click = handler; } };
    const count = { textContent: '0' };
    const container = { prepend(element) { this.error = element; } };
    const location = { href: '', pathname: '/Home/Index', search: '?sTerm=C%23' };
    const calls = [];
    const context = {
        URLSearchParams, location, console: { warn() {}, error() {} },
        fetch: async (...args) => { calls.push(args); return fetcher(...args); },
        document: {
            addEventListener(event, handler) { ready = handler; },
            getElementById(id) { return id === 'username' ? (signedIn ? {} : null) : id === 'cartCount' ? count : null; },
            querySelectorAll() { return [button]; },
            querySelector(selector) { return selector === '.container' ? container : { content: 'csrf-token' }; },
            createElement() { return { textContent: '', setAttribute() {}, classList: { remove() {}, add() {} } }; }
        }
    };
    vm.runInNewContext(script, context);
    await ready();
    return { button, count, container, location, calls };
}
test('anonymous buttons redirect to login without a failing membership request', async () => {
    const p = await page(false, () => { throw new Error('must not fetch'); });
    assert.equal(p.calls.length, 0);
    await p.button.click();
    assert.ok(p.location.href.startsWith('/Identity/Account/Login?returnUrl='));
});
test('membership failure leaves working click handlers and sends an antiforgery POST', async () => {
    const p = await page(true, async (url, options) => {
        if (url === '/Cart/GetCartItemIds') throw new Error('temporary membership failure');
        assert.equal(url, '/Cart/AddItem');
        assert.equal(options.method, 'POST');
        assert.equal(options.headers.RequestVerificationToken, 'csrf-token');
        assert.equal(options.body.get('bookId'), '7');
        return { ok: true, status: 200, json: async () => 3 };
    });
    assert.equal(p.button.disabled, false);
    await p.button.click();
    assert.equal(p.count.textContent, 3);
    assert.equal(p.button.disabled, true);
    assert.equal(p.button.textContent, 'Added');
});
test('add failure shows a message and restores the button for retry', async () => {
    const p = await page(true, async url => url === '/Cart/GetCartItemIds'
        ? { ok: true, json: async () => [] }
        : { ok: false, status: 409, json: async () => ({ message: 'Requested quantity is not available.' }) });
    await p.button.click();
    assert.equal(p.button.disabled, false);
    assert.equal(p.button.textContent, 'Add to Cart');
    assert.equal(p.container.error.textContent, 'Requested quantity is not available.');
});
