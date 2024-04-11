const validNameRegex = /^[A-Za-z\s]{1,}[\.]{0,1}[A-Za-z\s]{0,}$/;
const validEmailRegex = /^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$/;
const validPasswordRegex = /(?=^.{8,}$)((?=.*\d)|(?=.*\W+))(?![.\n])(?=.*[A-Z])(?=.*[a-z]).*$/;
const intlPhoneErrorMapping = ["Invalid number", "Invalid country code", "Too short", "Too long", "Invalid number"];


function validateCity(element) {

    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please select city";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.add('valid-field');
    parentElementClasses.remove("invalid-field");

    errorElement.style.display = "none";

    return true;

}

function validateState(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please select state";
        errorElement.style.display = "block";
        return false;
    }


    parentElementClasses.add('valid-field');
    parentElementClasses.remove("invalid-field");


    errorElement.style.display = "none";

    return true;
}

function validatePassword(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter password";
        errorElement.style.display = "block";
        return false;
    }
    else if (!validPasswordRegex.test(element.value)) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Password must contain 1 capital, 1 small, 1 Special symbol and at least 8 characters";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.add('valid-field');
    parentElementClasses.remove("invalid-field");


    errorElement.style.display = "none";
    return true;
}

function validateConfirmPassword(element) {

    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter confirm password";
        errorElement.style.display = "block";

        return false;
    }
    else if ($('#patient-password').val() != $('#patient-confirm-password').val()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Password and Confirm Password should be same";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.add('valid-field');
    parentElementClasses.remove("invalid-field");


    errorElement.style.display = "none";
    return true;
}

function validatePhoneNumber(phoneInputElement, phoneIntlField) {

    let phoneInputDiv = phoneInputElement.closest(".form-floating");
    let phoneInputParentClasses = phoneInputDiv.classList;
    let errorElement = phoneInputDiv.querySelector(".error-msg");

    if (!phoneInputElement.value.trim()) {

        phoneInputParentClasses.remove('valid-field');
        phoneInputParentClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter phone number";
        errorElement.style.display = "block";
        return false;

    }
    else if (!phoneIntlField.isValidNumber()) {

        const errorCode = phoneIntlField.getValidationError();
        const msg = intlPhoneErrorMapping[errorCode] || "Invalid number";

        phoneInputParentClasses.remove('valid-field');
        phoneInputParentClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter valid phone number";
        errorElement.style.display = "block";

        return false;

    }

    phoneInputParentClasses.add('valid-field');
    phoneInputParentClasses.remove("invalid-field");

    errorElement.style.display = "none";
    return true;

}

function validateEmail(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter value";
        errorElement.style.display = "block";
        return false;
    }
    else if (!validEmailRegex.test(element.value)) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter valid email";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.remove('invalid-field');
    parentElementClasses.add('valid-field');

    errorElement.style.display = "none";

    return true;
}

function validateFirstName(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter your first name";
        errorElement.style.display = "block";

        return false;
    }
    else if (!validNameRegex.test(element.value)) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter valid name";
        errorElement.style.display = "block";

        return false;
    }


    parentElementClasses.remove('invalid-field');
    parentElementClasses.add('valid-field');

    errorElement.style.display = "none";

    return true;
}

function validateLastName(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        errorElement.style.display = "none";
        return true;
    }

    if (!validNameRegex.test(element.value)) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter valid name";
        errorElement.style.display = "block";

        return false;
    }

    parentElementClasses.remove('invalid-field');
    parentElementClasses.add('valid-field');

    errorElement.style.display = "none";

    return true;

}