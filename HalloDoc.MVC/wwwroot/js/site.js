function myFunction() {
    const body = document.body;
    body.classList.toggle('dark-mode');
    // Save user preference in localStorage
    const isDarkMode = body.classList.contains('dark-mode');
    localStorage.setItem('darkMode', isDarkMode);
}

// Function to initialize dark mode based on user preference
function initializeDarkMode() {
    const savedDarkMode = localStorage.getItem('darkMode');
    if (savedDarkMode !== null) {
        document.body.classList.toggle('dark-mode', savedDarkMode === 'true');
    }
}

//Initialize dark mode
initializeDarkMode();

function toggleDarkMode() {
    var element = document.body;
    //Boolean isDark = localStorage.getItem("isDark");
    isDark = !isDark;
    if (isDark) {
        element.classList.add("dark-mode");
        element.getElementsByClassName("table")[0].classList.add("table-dark");
    }
    else {
        element.classList.remove("dark-mode");
        element.getElementsByClassName("table")[0].classList.remove("table-dark");
    }

    //localStorage.setItem("isDark");
    //location.reload();
}
