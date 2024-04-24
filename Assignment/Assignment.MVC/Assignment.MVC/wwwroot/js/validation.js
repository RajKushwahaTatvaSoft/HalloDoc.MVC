const validNameRegex = /^[A-Za-z\s]{1,}[\.]{0,1}[A-Za-z\s]{0,}$/;
const validEmailRegex = /^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$/;
const validPasswordRegex = /(?=^.{8,}$)((?=.*\d)|(?=.*\W+))(?![.\n])(?=.*[A-Z])(?=.*[a-z]).*$/;
const validNumberRegex = /^[0-9\+\-]+$/; // Only digits are allowed
const validNumberWithSpacesRegex = /^[0-9\+\- ]+$/; // Only digits with spaces are allowed
const intlPhoneErrorMapping = ["Invalid number", "Invalid country code", "Too short", "Too long", "Invalid number"];

function validateRequired(element) {

    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "This field is required";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.add('valid-field');
    parentElementClasses.remove("invalid-field");

    errorElement.style.display = "none";

    return true;
}

function validateNumber(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter value";
        errorElement.style.display = "block";
        return false;
    }
    else if (!validNumberRegex.test(element.value)) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Only digits are allowed";
        errorElement.style.display = "block";
        return false;
    }

    parentElementClasses.remove('invalid-field');
    parentElementClasses.add('valid-field');

    errorElement.style.display = "none";

    return true;
}

function validateName(element) {
    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter name";
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
    else if (!validNumberWithSpacesRegex.test(phoneInputElement.value)) {
        phoneInputParentClasses.remove('valid-field');
        phoneInputParentClasses.add("invalid-field");

        errorElement.innerHTML = "Phone number should only contain numbers.";
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

        errorElement.innerHTML = "Please enter email";
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

function validateRadio(elementId, radioName) {

    let errorElement = $('#' + elementId).siblings('.error-msg')[0];

    if ($('input[type=radio][name=' + radioName + ']:checked').length == 0) {

        errorElement.innerHTML = "Please select gender";
        errorElement.style.display = "block";

        return false;
    }

    errorElement.style.display = "none";

    return true;
}

