document.addEventListener("DOMContentLoaded", async () => {
    try {
        const response = await fetch('/Cart/GetCartItemIds');
        if (!response.ok) throw new Error("Failed to fetch cart items");

        const cartBookIds = await response.json();

        document.querySelectorAll('.addToCartButton').forEach(btn => {
            const bookId = parseInt(btn.dataset.bookId);

            if (cartBookIds.includes(bookId)) {
                updateButtonToAdded(btn);
                return;
            }

            btn.addEventListener('click', async () => {
                if (!document.getElementById("username")) {
                    window.location.href = "/Identity/Account/Login";
                    return;
                }

                setButtonLoading(btn);

                try {
                    const addResponse = await fetch(`/Cart/AddItem?bookId=${bookId}`);
                    if (addResponse.ok) {
                        const result = await addResponse.json();
                        document.getElementById("cartCount").innerHTML = result;
                        updateButtonToAdded(btn);
                    } else {
                        throw new Error("Failed to add item");
                    }
                } catch (err) {
                    console.error(err);
                    resetButton(btn);
                }
            });
        });

    } catch (err) {
        console.error("Error loading cart items", err);
    }
});

function updateButtonToAdded(btn) {
    btn.innerHTML = '<i class="bi bi-cart-check"></i> Added';
    btn.disabled = true;
    btn.classList.replace("btn-success", "btn-secondary");
}

function setButtonLoading(btn) {
    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Adding...';
    btn.disabled = true;
}

function resetButton(btn) {
    btn.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
    btn.disabled = false;
}
