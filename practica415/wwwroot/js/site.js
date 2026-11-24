document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            if (!window.confirm(form.dataset.confirm)) event.preventDefault();
        });
    });

    document.querySelectorAll(".reconcile-form").forEach(form => {
        const input = form.querySelector(".actual-quantity");
        const differenceCell = form.closest("tr")?.querySelector(".difference-cell");
        const systemQuantity = Number(form.dataset.systemQuantity);

        const updateDifference = () => {
            if (!differenceCell) return;
            const difference = Number(input.value) - systemQuantity;
            differenceCell.textContent = difference > 0 ? `+${difference}` : String(difference);
            differenceCell.classList.toggle("text-success", difference > 0);
            differenceCell.classList.toggle("text-danger", difference < 0);
        };

        input.addEventListener("input", updateDifference);
        updateDifference();
    });

    const currentPath = window.location.pathname.toLowerCase();
    if (currentPath === "/") {
        document.querySelector('.side-link[href*="Dashboard"]')?.classList.add("active");
        document.querySelector('.mobile-nav a[href*="Dashboard"]')?.classList.add("active");
    }
    document.querySelectorAll(".side-link, .mobile-nav a").forEach(link => {
        const href = new URL(link.href, window.location.origin).pathname.toLowerCase();
        if (href === currentPath || (href !== "/" && currentPath.startsWith(`${href}/`)) || (href.endsWith("/index") && href.slice(0, -6) === currentPath)) {
            link.classList.add("active");
        }
    });
});
