document.addEventListener("DOMContentLoaded", async () => {
    if (!document.getElementById("username")) return;
    try {
        const response = await fetch("/Cart/GetTotalItemInCart", { headers: { Accept: "application/json" } });
        if (!response.ok) throw new Error("Unable to load cart count.");
        const element = document.getElementById("cartCount");
        if (element) element.textContent = await response.json();
    } catch (err) { console.warn(err.message); }
});
