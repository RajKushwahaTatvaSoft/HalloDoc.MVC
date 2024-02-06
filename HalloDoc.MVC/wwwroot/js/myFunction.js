function myFunction() {
    var element = document.body;
    Boolean; isDark = localStorage.getItem("isDark");
    isDark = !isDark;
    if (isDark) {
        element.classList.add("dark-mode");
    }
    else {
        element.classList.remove("dark-mode");
    }

    localStorage.setItem("isDark");
}
