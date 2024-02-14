localStorage.setItem("isDark", false);

var isDark = false;
//window.onload = function () {
//    //Boolean isDark = localStorage.getItem("isDark");
//    if (isDark) {
//        document.body.classList.add("dark-mode");
//    }
//}
function myFunction() {
    const body = document.body;
    body.classList.toggle('dark-mode');
    // Save user preference in localStorage
    const isDarkMode = body.classList.contains('dark-mode');
    localStorage.setItem('darkMode', isDarkMode);
}

// Function to initialize dark mode based on user preference
function initializeDarkMode() {
    const prefersDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const savedDarkMode = localStorage.getItem('darkMode');

    if (savedDarkMode !== null) {
        document.body.classList.toggle('dark-mode', savedDarkMode === 'true');
    } else {
        document.body.classList.toggle('dark-mode', prefersDarkMode);
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

function togglePassword() {
    var text = document.getElementById("login-pass-text");
    var btn = document.getElementById("login-pass-btn");

    if (text.type == "text") {
        text.type = "password";
        btn.src = "/images/password_not_visible.svg";
    }
    else {
        text.type = "text";
        btn.src = "/images/password_icon.svg";
    }

}