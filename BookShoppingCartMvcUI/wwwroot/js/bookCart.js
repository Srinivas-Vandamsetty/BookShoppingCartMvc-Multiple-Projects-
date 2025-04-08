async function addToCart(bookId, btn) {
    var usernameEl = document.getElementById("username");
    if (usernameEl == null) {
        window.location.href = "/Identity/Account/Login";
        return;
    }

    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Adding...';
    btn.disabled = true;

    try {
        var response = await fetch(`/Cart/AddItem?bookId=${bookId}`);
        if (response.status == 200) {
            var result = await response.json();
            document.getElementById("cartCount").innerHTML = result;
            btn.innerHTML = '<i class="bi bi-cart-check"></i> Added';
            btn.classList.replace("btn-success", "btn-secondary");
        }
    } catch (err) {
        console.log(err);
        btn.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
        btn.disabled = false;
    }
}
