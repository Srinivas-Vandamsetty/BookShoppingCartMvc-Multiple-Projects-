document.addEventListener("DOMContentLoaded", function () {
    loadCartCount();
});

async function loadCartCount() {
    try {
        var response = await fetch(`/Cart/GetTotalItemInCart`);
        if (response.status == 200) {
            var result = await response.json();
            var cartCountEl = document.getElementById("cartCount");
            if (cartCountEl) {
                cartCountEl.innerHTML = result;
            }
        }
    } catch (err) {
        console.log(err);
    }
}
