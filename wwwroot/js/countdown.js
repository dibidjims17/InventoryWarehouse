document.addEventListener("DOMContentLoaded", function() {
    const el = document.getElementById("countdown");
    if (!el) return;

    const seconds = parseInt(el.dataset.seconds || "5", 10);
    const redirect = el.dataset.redirect || "/";

    let counter = seconds;
    el.textContent = counter;

    const interval = setInterval(() => {
        counter--;
        el.textContent = counter;
        if (counter <= 0) {
            clearInterval(interval);
            window.location.href = redirect;
        }
    }, 1000);
});
