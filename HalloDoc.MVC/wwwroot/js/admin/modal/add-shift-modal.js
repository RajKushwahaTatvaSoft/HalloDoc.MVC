$('#is-repeat-switch').change(function () {
    if (this.checked) {
        $('#is-repeat-div').show();
    }
    else {
        $('#is-repeat-div').hide();
    }
});

$("#add-shift-region-list").change(function () {
    var type = $('option:selected', this).val();
    console.log(type);

    FetchPhyListByRegion(type, "add-shift-physician-list");
});

$('#add-shift-modal-form').on('submit', function (event) {
    event.preventDefault();

    debugger;
    let shiftDateResult = validateShiftDate();
    let startTimeResult = validateStartTime();
    let endTimeResult = validateEndTime();

    if (shiftDateResult && startTimeResult && endTimeResult) {
        var formData = $(this).serialize();

        $.ajax({
            url: '/Admin/AddShift',
            type: 'POST',
            data: formData,
            success: function (result) {
                if (result) {
                    $('#addShiftModal').modal('hide');
                    location.reload();
                }
            },
            error: function (err) {
                console.error(err);
            }
        });

    }

});

let todayDate = new Date().setHours(0, 0, 0, 0);

function validateShiftDate() {
    let dateElement = document.getElementById("add-shift-date-input");

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

function validateStartTime() {

    let dateElement = document.getElementById("add-shift-date-input");
    let startTimeElement = document.getElementById("add-shift-start-time");
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

function validateEndTime() {
    let startTimeElement = document.getElementById("add-shift-start-time");
    let endTimeElement = document.getElementById("add-shift-end-time");
    const startTime = startTimeElement.value;
    const endTime = endTimeElement.value;

    if (!endTime.trim()) {
        $('#end-time-error').show();
        $('#start-time-error').text("Please select start time");
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

    if (timeto < timefrom) {
        $('#end-time-error').show();
        $('#end-time-error').text("End time should be more than start time");
        return false;
    }

    $('#end-time-error').hide();
    return true;
}
