document.addEventListener("DOMContentLoaded", async () => {
    const signedIn = !!document.getElementById("username");
    const buttons = [...document.querySelectorAll(".addToCartButton")];
    const error = document.createElement("div");
    error.className = "alert alert-danger d-none";
    error.setAttribute("role", "alert");
    document.querySelector(".container")?.prepend(error);
    const showError = message => { error.textContent = message; error.classList.remove("d-none"); };
    const markAdded = btn => { btn.textContent = "Added"; btn.disabled = true; };
    for (const btn of buttons) {
        btn.addEventListener("click", async () => {
            if (!signedIn) { location.href = "/Identity/Account/Login?returnUrl=" + encodeURIComponent(location.pathname + location.search); return; }
            btn.disabled = true;
            btn.textContent = "Adding...";
            try {
                const token = document.querySelector('meta[name="csrf-token"]')?.content;
                const response = await fetch("/Cart/AddItem", {
                    method: "POST", headers: { "Accept": "application/json", "Content-Type": "application/x-www-form-urlencoded", "RequestVerificationToken": token ?? "" },
                    body: new URLSearchParams({ bookId: btn.dataset.bookId, qty: "1" })
                });
                if (response.status === 401) { location.href = "/Identity/Account/Login"; return; }
                const result = await response.json();
                if (!response.ok) throw new Error(result.message ?? result.title ?? "Unable to add this book.");
                const count = document.getElementById("cartCount");
                if (count) count.textContent = result;
                markAdded(btn);
            } catch (err) {
                btn.disabled = false;
                btn.textContent = "Add to Cart";
                showError(err.message ?? "Unable to add this book. Please retry.");
            }
        });
    }
    if (!signedIn) return;
    try {
        const response = await fetch("/Cart/GetCartItemIds", { headers: { Accept: "application/json" } });
        if (!response.ok) throw new Error("Unable to load cart membership. You can still retry adding a book.");
        const ids = await response.json();
        buttons.filter(btn => ids.includes(Number(btn.dataset.bookId))).forEach(markAdded);
    } catch (err) { showError(err.message); }
});
