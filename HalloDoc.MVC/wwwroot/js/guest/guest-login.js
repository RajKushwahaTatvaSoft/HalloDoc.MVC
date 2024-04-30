$(document).ready(function () {

    let guestLoginForm = document.getElementById("guest-login-form");

    if (guestLoginForm != null) {

        guestLoginForm.addEventListener('submit', event => {
            if (!validateGuestLoginForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }
});

function togglePassword() {
    var text = document.getElementById("guest-login-password");
    var btn = document.getElementById("guest-login-toggle-pass-btn");

    if (text.type == "text") {
        text.type = "password";
        btn.src = "/images/password_not_visible.svg";
    }
    else {
        text.type = "text";
        btn.src = "/images/password_icon.svg";
    }

}

function validateGuestLoginForm() {
    let usernameResult = validateRequired("guest-login-username","Please enter username");
    let passwordResult = validateRequired("guest-login-password", "Please enter password");

    if (usernameResult && passwordResult) {
        return true;
    }

    return false;
}