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
            if (!validateConciergeForm()) {
                event.preventDefault()
                event.stopPropagation()
            }

        }, false);

    }

    if (businessRequestForm != null) {

        businessRequestForm.addEventListener('submit', event => {
            if (!validateBusinessForm()) {
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



const patientPhoneInputElement = document.getElementById("patient-phone");
let patientIntlInput = window.intlTelInput(patientPhoneInputElement, {
    utilsScript:
        "https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/17.0.8/js/utils.js",
    preferredCountries: ["in"],
    separateDialCode: true,
    initialCountry: "in"
});


const otherPhoneInputElement = document.getElementById("other-phone");
let otherIntlInput;

if (otherPhoneInputElement != null) {
    otherIntlInput = window.intlTelInput(otherPhoneInputElement, {
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

    countryCodePatient.value = patientIntlInput.getSelectedCountryData().dialCode;

    if (otherPhoneInputElement != null) {
        countryCodeOther.value = otherIntlInput.getSelectedCountryData().dialCode;
    }

}

// ------------------- VALIDATION SECTION -----------------------

function validatePatientForm() {
    let firstNameElement = document.getElementById('patient-form-first-name');
    let lastNameElement = document.getElementById('patient-form-last-name');
    let emailElement = document.getElementById('patient-form-email');
    let passElement = document.getElementById('patient-password');
    let dobElement = document.getElementById('patient-dob');
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
    let phoneResult = validatePhoneNumber(patientPhoneInputElement, patientIntlInput);
    let dobResult = validateDOB(dobElement);

    if ($('#hiddendiv').is(":visible")) {
        passResult = validatePassword(passElement);
        confirmPassResult = validateConfirmPassword(confirmPassElement);
    }

    if (firstNameResult && lastNameResult && emailResult && phoneResult && passResult && confirmPassResult && stateResult && cityResult && dobResult) {
        return true;
    }

    return false;
}

function validateFamilyFriendForm() {

    let familyFirstNameElement = document.getElementById('family-friend-first-name');
    let familyLastNameElement = document.getElementById('family-friend-last-name');
    let familyEmailElement = document.getElementById('family-friend-email');
    let familyRelationElement = document.getElementById('family-friend-relation');
    let patientFirstNameElement = document.getElementById('family-friend-patient-first-name');
    let patientLastNameElement = document.getElementById('family-friend-patient-last-name');
    let patientEmailElement = document.getElementById('family-friend-patient-email');
    let patientStateElement = document.getElementById('patient-state');
    let patientCityElement = document.getElementById('patient-city');
    let patientDobElement = document.getElementById('family-friend-patient-dob');

    let familyFirstNameResult = validateFirstName(familyFirstNameElement);
    let familyLastNameResult = validateLastName(familyLastNameElement);
    let familyEmailResult = validateEmail(familyEmailElement);
    let familyRelationResult = validateRequired(familyRelationElement);
    let familyPhoneResult = validatePhoneNumber(otherPhoneInputElement, otherIntlInput);

    let patientFirstNameResult = validateFirstName(patientFirstNameElement);
    let patientLastNameResult = validateLastName(patientLastNameElement);
    let patientEmailResult = validateEmail(patientEmailElement);
    let patientPhoneResult = validatePhoneNumber(patientPhoneInputElement, patientIntlInput);
    let patientStateResult = validateState(patientStateElement);
    let patientCityResult = validateCity(patientCityElement);
    let patientDobResult = validateDOB(patientDobElement);

    if (familyFirstNameResult && familyLastNameResult && familyEmailResult && familyPhoneResult && familyRelationResult
        && patientFirstNameResult && patientLastNameResult && patientEmailResult && patientPhoneResult && patientStateResult && patientCityResult && patientDobResult) {
        return true;
    }

    return false;

}

function validateConciergeForm() {

    let conciergeFirstNameElement = document.getElementById('concierge-first-name');
    let conciergeLastNameElement = document.getElementById('concierge-last-name');
    let conciergeEmailElement = document.getElementById('concierge-email');
    let conciergeHotelOrProperty = document.getElementById('concierge-hotel-or-property-name');

    let patientFirstNameElement = document.getElementById('concierge-patient-first-name');
    let patientLastNameElement = document.getElementById('concierge-patient-last-name');
    let patientEmailElement = document.getElementById('concierge-patient-email');
    let patientStateElement = document.getElementById('patient-state');
    let patientCityElement = document.getElementById('patient-city');
    let patientDobElement = document.getElementById('concierge-patient-dob');

    let conciergeFirstNameResult = validateFirstName(conciergeFirstNameElement);
    let conciergeLastNameResult = validateLastName(conciergeLastNameElement);
    let conciergeEmailResult = validateEmail(conciergeEmailElement);
    let conciergeHotelResult = validateRequired(conciergeHotelOrProperty);
    let conciergePhoneResult = validatePhoneNumber(otherPhoneInputElement, otherIntlInput);

    let patientFirstNameResult = validateFirstName(patientFirstNameElement);
    let patientLastNameResult = validateLastName(patientLastNameElement);
    let patientEmailResult = validateEmail(patientEmailElement);
    let patientPhoneResult = validatePhoneNumber(patientPhoneInputElement, patientIntlInput);
    let patientStateResult = validateState(patientStateElement);
    let patientCityResult = validateCity(patientCityElement);
    let patientDobResult = validateDOB(patientDobElement);

    if (conciergeFirstNameResult && conciergeLastNameResult && conciergeEmailResult && conciergePhoneResult && conciergeHotelResult
        && patientFirstNameResult && patientLastNameResult && patientEmailResult && patientPhoneResult && patientStateResult && patientCityResult && patientDobResult) {
        return true;
    }

    return false;
}

function validateBusinessForm() {

    let businessFirstNameElement = document.getElementById('business-first-name');
    let businessLastNameElement = document.getElementById('business-last-name');
    let businessEmailElement = document.getElementById('business-email');
    let businessPropertyElement = document.getElementById('business-property-name');
    let businessCaseNumberElement = document.getElementById('business-case-number');

    let patientFirstNameElement = document.getElementById('business-patient-first-name');
    let patientLastNameElement = document.getElementById('business-patient-last-name');
    let patientEmailElement = document.getElementById('business-patient-email');
    let patientStateElement = document.getElementById('patient-state');
    let patientCityElement = document.getElementById('patient-city');
    let patientDobElement = document.getElementById('business-patient-dob');

    let businessFirstNameResult = validateFirstName(businessFirstNameElement);
    let businessLastNameResult = validateLastName(businessLastNameElement);
    let businessEmailResult = validateEmail(businessEmailElement);
    let businessPropertyResult = validateRequired(businessPropertyElement);
    let businessCaseNumberResult = validateRequired(businessCaseNumberElement);
    let businessPhoneResult = validatePhoneNumber(otherPhoneInputElement, otherIntlInput);

    let patientFirstNameResult = validateFirstName(patientFirstNameElement);
    let patientLastNameResult = validateLastName(patientLastNameElement);
    let patientEmailResult = validateEmail(patientEmailElement);
    let patientPhoneResult = validatePhoneNumber(patientPhoneInputElement, patientIntlInput);
    let patientStateResult = validateState(patientStateElement);
    let patientCityResult = validateCity(patientCityElement);
    let patientDobResult = validateDOB(patientDobElement);

    if (businessFirstNameResult && businessLastNameResult && businessEmailResult && businessPhoneResult && businessPropertyResult && businessCaseNumberResult
        && patientFirstNameResult && patientLastNameResult && patientEmailResult && patientPhoneResult && patientStateResult && patientCityResult && patientDobResult) {
        return true;
    }

    return false;
}
