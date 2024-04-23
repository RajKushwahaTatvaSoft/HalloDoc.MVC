const validNameRegex = /^[A-Za-z\s]{1,}[\.]{0,1}[A-Za-z\s]{0,}$/;
const validEmailRegex = /^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$/;
const validPasswordRegex = /(?=^.{8,}$)((?=.*\d)|(?=.*\W+))(?![.\n])(?=.*[A-Z])(?=.*[a-z]).*$/;
const validNumberRegex = /^[0-9\+\-]+$/; // Only digits are allowed
const intlPhoneErrorMapping = ["Invalid number", "Invalid country code", "Too short", "Too long", "Invalid number"];

let todayDate = new Date().setHours(0, 0, 0, 0);

function validateShiftDate(elementId) {
    let dateElement = document.getElementById(elementId);

    if (!dateElement.value.trim()) {
        $('#shift-date-error').show();
        $('#shift-date-error').text("Please enter date");
        return false;
    }

    const selectedDate = new Date(dateElement.value);
    if (selectedDate < todayDate) {

        $('#shift-date-error').show();
        $('#shift-date-error').text("Shift date invalid");
        return false;
    }

    $('#shift-date-error').hide();
    return true;
}

function validateStartTime(startTimeId,dateId) {

    let dateElement = document.getElementById(dateId);
    let startTimeElement = document.getElementById(startTimeId);
    const selectedDate = new Date(dateElement.value);
    let currentTime = new Date();
    const startTime = startTimeElement.value;

    if (!startTime.trim()) {
        $('#start-time-error').show();
        $('#start-time-error').text("Please select start time");
        return false;
    }

    if (selectedDate.setHours(0, 0, 0, 0) < todayDate) {
        $('#start-time-error').show();
        $('#start-time-error').text("Can't create shift in past.");
        return false;
    }

    if (selectedDate.setHours(0, 0, 0, 0) == todayDate) {

        const userInputHours = parseInt(startTime.split(":")[0]);
        const userInputMinutes = parseInt(startTime.split(":")[1]);

        const userInputTimeObject = new Date(currentTime.getFullYear(), currentTime.getMonth(), currentTime.getDate(), userInputHours, userInputMinutes);

        if (userInputTimeObject.getTime() < currentTime.getTime()) {
            $('#start-time-error').show();
            $('#start-time-error').text("Shift can be created after the current time.");
            return false;
        }
    }

    $('#start-time-error').hide();
    return true;
}

function validateEndTime(endTimeId,startTimeId) {
    let startTimeElement = document.getElementById(startTimeId);
    let endTimeElement = document.getElementById(endTimeId);
    const startTime = startTimeElement.value;
    const endTime = endTimeElement.value;

    if (!endTime.trim()) {
        $('#end-time-error').show();
        $('#end-time-error').text("Please select end time");
        return false;
    }

    let timefrom = new Date();
    let timeFromHour = parseInt(startTime.split(":")[0]);
    let timeFromMinute = parseInt(startTime.split(":")[1]);
    timefrom.setHours((timeFromHour - 1 + 24) % 24);
    timefrom.setMinutes(timeFromMinute);

    let timeto = new Date();
    let timeToHour = parseInt(endTime.split(":")[0]);
    let timeToMinute = parseInt(endTime.split(":")[1]);
    timeto.setHours((timeToHour - 1 + 24) % 24);
    timeto.setMinutes(timeToMinute);

    console.log(startTime);
    console.log(endTime);

    if (timeto <= timefrom) {
        $('#end-time-error').show();
        $('#end-time-error').text("End time should be more than start time");
        return false;
    }

    $('#end-time-error').hide();
    return true;
}


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
    else if (!validNumberRegex.test(phoneInputElement.value)) {
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
        parentElementClasses.remove("invalid-field");
        parentElementClasses.remove('valid-field');
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

function validateDOB(element) {

    let parentElementClasses = element.parentElement.classList;
    let errorElement = $('#' + element.id).siblings('.error-msg')[0];

    if (!element.value.trim()) {
        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Please enter date of birth.";
        errorElement.style.display = "block";
        return false;
    }

    var selectedDate = new Date(element.value);
    var currentDate = new Date();
    if (selectedDate > currentDate) {

        parentElementClasses.remove('valid-field');
        parentElementClasses.add("invalid-field");

        errorElement.innerHTML = "Future citizens are not allowed yet.";
        errorElement.style.display = "block";

        return false;
    }

    parentElementClasses.remove('invalid-field');
    parentElementClasses.add('valid-field');

    errorElement.style.display = "none";

    return true;
}