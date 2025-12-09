document.addEventListener("DOMContentLoaded", () => {
    
    // Select Elements
    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("overlay"); // <-- NEW: Added overlay
    const hamburger = document.querySelector(".hamburger");
    const closeBtn = document.querySelector(".close-btn");

    // Check if critical elements exist before adding listeners
    if (!sidebar || !hamburger || !closeBtn || !overlay) {
        console.error("One or more essential header/sidebar elements are missing from the DOM.");
        return; 
    }

    // --- Sidebar Helper Functions ---

    const openSidebar = () => {
        sidebar.classList.add("open");
        overlay.style.display = "block"; // <-- NEW: Show overlay
    };

    const closeSidebar = () => {
        sidebar.classList.remove("open");
        overlay.style.display = "none"; // <-- NEW: Hide overlay
    };

    // --- Event Listeners ---

    // 1. Open sidebar (Hamburger click)
    hamburger.addEventListener("click", openSidebar);

    // 2. Close sidebar (Close button click)
    closeBtn.addEventListener("click", closeSidebar);

    // 3. Close sidebar (ESC key press)
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && sidebar.classList.contains("open")) {
            closeSidebar();
        }
    });

    // 4. Click outside sidebar to close (Includes overlay click)
    document.addEventListener("click", (e) => {
        // Check if the sidebar is open and the click target is NOT the sidebar, AND NOT the hamburger button
        if (sidebar.classList.contains("open") && 
            !sidebar.contains(e.target) && 
            !e.target.closest(".hamburger")) 
        {
            closeSidebar();
        }
    });

    // --- Global Functions (Required by HTML attributes) ---

    // Logout confirmation
    window.confirmLogout = () => {
        return confirm("Are you sure you want to log out?");
    };

    // Notification bell placeholder
    window.notifyBellClick = () => {
        console.log("Notification bell clicked");
        // TODO: Implement notification dropdown or other logic
    };
});