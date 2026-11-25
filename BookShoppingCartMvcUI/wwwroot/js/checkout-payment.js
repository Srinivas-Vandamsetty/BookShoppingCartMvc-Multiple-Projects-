(() => {
    const options = [...document.querySelectorAll("[data-payment-option]")];
    const note = document.querySelector("[data-payment-note]");
    const cardDetails = document.querySelector("[data-card-details]");
    const upiDetails = document.querySelector("[data-upi-details]");
    if (!options.length || !note) return;

    const messages = {
        cash: "Cash on Delivery: your order is placed now and payment is collected when it is delivered.",
        creditcard: "Card: this project uses sandbox authorization. A live card gateway must be configured before production use.",
        upi: "UPI: this project uses sandbox authorization. A live UPI provider must be configured before production use."
    };

    const refresh = () => {
        const selected = options.find(option => option.checked);
        options.forEach(option => option.closest(".bv-payment-option")?.classList.toggle("is-selected", option.checked));
        note.textContent = selected ? messages[selected.value] ?? "" : "Choose a payment method to continue.";
        if (cardDetails) cardDetails.hidden = selected?.value !== "creditcard";
        if (upiDetails) upiDetails.hidden = selected?.value !== "upi";
    };

    options.forEach(option => option.addEventListener("change", refresh));
    refresh();
})();
