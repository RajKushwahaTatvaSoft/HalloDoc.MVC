var isLightMode = true

localStorage.setItem("isLightMode", true)

window.onload =
    function () {
        if (localStorage.getItem('isLightMode') === 'false') {
            setDarkMode();
        }
    }

function myFunction() {
    localStorage.setItem("isLightMode", !isLightMode)
    location.reload();
}

function setDarkMode() {
    var element = document.body;
    element.classList.toggle("dark-mode");
}