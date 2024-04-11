$(document).ready(function () {

    let patientRequestForm = document.getElementById('patient-request-form');
    let familyFriendRequestForm = document.getElementById('family-friend-request-form');
    let conciergeRequestForm = document.getElementById('concierge-request-form');
    let businessRequestForm = document.getElementById('business-request-form');

    if (patientRequestForm != null) {

        patientRequestForm.addEventListener('submit', event => {
            if (!validatePatientForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }


    if (familyFriendRequestForm != null) {

        familyFriendRequestForm.addEventListener('submit', event => {
            if (!validateFamilyFriendForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }


    if (conciergeRequestForm != null) {

        conciergeRequestForm.addEventListener('submit', event => {
            if (!conciergeRequestForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }

    if (businessRequestForm != null) {

        businessRequestForm.addEventListener('submit', event => {
            if (!businessRequestForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }
    $('#patient-form-email').on('blur', function () {
        var email = $(this).val();
        $.ajax({
            url: '/Guest/PatientCheckEmail',
            type: 'POST',
            data: { email: email },
            success: function (data) {
                if (!data.exists) {
                    $('#hiddendiv').show();
                    $('#patient-password').prop('required', true);
                    $('#patient-confirm-password').prop('required', true);
                }
                else {
                    $('#hiddendiv').hide();
                    $('#patient-password').prop('required', false);
                    $('#patient-confirm-password').prop('required', false);
                }
            }
        });
    });
});


$("#patient-state").change(function () {

    var regId = $('option:selected', this).val();

    $.ajax({
        url: '/Guest/GetCitiesByRegion',
        type: 'POST',
        data: {
            regionId: regId,
        },
        success: function (result) {
            console.log(result);
            $('#patient-city').empty();
            $('#patient-city').append($('<option>', {
                value: "",
                text: '-- Select City --',
                selected: true,
                disabled: true
            }));
            $.each(result, function (index, object) {

                $('#patient-city').append($('<option>', {
                    value: object["id"],
                    text: object["name"]
                }));

            });
        },
        error: function (err) {
            console.error(err);
        }
    });
});


// bind file-input-form click action to text-input-span
$('#text_input_span_id').click(function () {
    $("#file_input_id").trigger('click');
})
// bind file-input-form click action to text-input-form
$('#text_input_id').click(function () {
    $("#file_input_id").trigger('click');
})
// display file name in text-input-form
$("#file_input_id").change(function () {
    $('#text_input_id').val(this.value.replace(/C:\\fakepath\\/i, ''))
})



const phoneInputField = document.getElementById("patient-phone");
let patientPhoneInput = window.intlTelInput(phoneInputField, {
    utilsScript:
        "https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/17.0.8/js/utils.js",
    preferredCountries: ["in"],
    separateDialCode: true,
    initialCountry: "in"
});


const otherInputField = document.getElementById("other-phone");
let otherPhoneInput;

if (otherInputField != null) {
    otherPhoneInput = window.intlTelInput(otherInputField, {
        utilsScript:
            "https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/17.0.8/js/utils.js",
        preferredCountries: ["in"],
        separateDialCode: true,
        initialCountry: "in"
    });
}


function changeCountryCode() {

    var countryCodePatient = document.getElementById("patient-phone-country");
    var countryCodeOther = document.getElementById("other-phone-country");

    countryCodePatient.value = patientPhoneInput.getSelectedCountryData().dialCode;

    if (otherInputField != null) {
        countryCodeOther.value = otherPhoneInput.getSelectedCountryData().dialCode;
    }

}

// ------------------- VALIDATION SECTION -----------------------

function validatePatientForm() {
    let firstNameElement = document.getElementById('patient-form-first-name');
    let lastNameElement = document.getElementById('patient-form-last-name');
    let emailElement = document.getElementById('patient-form-email');
    let passElement = document.getElementById('patient-password');
    let confirmPassElement = document.getElementById('patient-confirm-password');
    let stateSelectElement = document.getElementById('patient-state');
    let citySelectElement = document.getElementById('patient-city');

    let firstNameResult = validateFirstName(firstNameElement);
    let lastNameResult = validateLastName(lastNameElement);
    let emailResult = validateEmail(emailElement);
    let passResult = true;
    let confirmPassResult = true;
    let stateResult = validateState(stateSelectElement);
    let cityResult = validateCity(citySelectElement);
    let phoneResult = validatePhoneNumber(phoneInputField, patientPhoneInput);

    if ($('#hiddendiv').is(":visible")) {
        passResult = validatePassword(passElement);
        confirmPassResult = validateConfirmPassword(confirmPassElement);
    }

    if (firstNameResult && lastNameResult && emailResult && phoneResult && passResult && confirmPassResult && stateResult && cityResult) {
        return true;
    }

    return false;
}

function validateFamilyFriendForm() {

    let familyFirstNameElement = document.getElementById('family-friend-first-name');
    let familyLastNameElement = document.getElementById('family-friend-last-name');
    let familyEmailElement = document.getElementById('family-friend-email');


    let firstNameResult = validateFirstName(familyFirstNameElement);
    let lastNameResult = validateLastName(familyLastNameElement);
    let emailResult = validateEmail(familyEmailElement);

    if (firstNameResult && lastNameResult && emailResult) {
        return false;
    }

    return false;

}

function validateConciergeForm() {
    return false;
}

function validateBusinessForm() {
    return false;
}
