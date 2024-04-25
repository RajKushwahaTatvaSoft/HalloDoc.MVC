$('#edit-shift-modal-form').on('submit', function (event) {
    event.preventDefault();

    let shiftDateResult = validateShiftDate("view-shift-date-input");
    let startTimeResult = validateStartTime("view-shift-start-time", "view-shift-date-input");
    let endTimeResult = validateEndTime("view-shift-end-time", "view-shift-start-time");

    if (shiftDateResult && startTimeResult && endTimeResult) {

        let formData = $(this).serialize();
        $.ajax({
            url: '/Physician/EditShift',
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
