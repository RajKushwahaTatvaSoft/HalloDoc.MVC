localStorage.setItem("isDark", false);

window.onload = function () {

    Boolean isDark = localStorage.getItem("isDark");
    if (isDark) {
        document.body.classList.add("dark-mode");
    }
}

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