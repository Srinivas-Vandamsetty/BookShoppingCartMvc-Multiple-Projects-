document.addEventListener("DOMContentLoaded", function () {
    const shippingSelect = document.getElementById("shippingMethod");
    const totalDisplay = document.getElementById("totalCostDisplay");
    const baseTotalInput = document.getElementById("baseTotalValue");

    const baseTotal = parseFloat(baseTotalInput?.value || 0);

    function updateTotal() {
        let shippingCost = 0;
        if (baseTotal <= 999 && shippingSelect) {
            shippingCost = parseFloat(shippingSelect.value);
        }

        const newTotal = (baseTotal + shippingCost).toFixed(2);
        totalDisplay.textContent = `₹${newTotal}`;
    }

    if (shippingSelect && baseTotal <= 999) {
        shippingSelect.addEventListener("change", updateTotal);
    }

    updateTotal();
});
