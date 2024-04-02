$(document).ready(function () {

    $('#floatingInputemail').on('blur', function () {
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


(() => {
    'use strict'

    // Fetch all the forms we want to apply custom Bootstrap validation styles to
    const forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault()
                event.stopPropagation()
            }

            form.classList.add('was-validated')
        }, false)
    })
})()

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
var patientPhoneInput = window.intlTelInput(phoneInputField, {
    utilsScript:
        "https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/17.0.8/js/utils.js",
    preferredCountries: ["in"],
    separateDialCode: true,
    initialCountry: "in"
});


const otherInputField = document.getElementById("other-phone");

if (otherInputField != null) {
    var otherPhoneInput = window.intlTelInput(otherInputField, {
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
