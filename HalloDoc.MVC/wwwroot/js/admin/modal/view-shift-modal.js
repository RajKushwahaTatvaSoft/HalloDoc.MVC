
$('#edit-shift-modal-form').on('submit', function (event) {
    event.preventDefault();

    let regionResult = validateRequired(document.getElementById("view-shift-region-list"));
    let phyResult = validateRequired(document.getElementById("view-shift-physician-list"));
    let shiftDateResult = validateShiftDate("view-shift-date-input");
    let startTimeResult = validateStartTime("view-shift-start-time","view-shift-date-input");
    let endTimeResult = validateEndTime("view-shift-end-time","view-shift-start-time");

    if (shiftDateResult && startTimeResult && endTimeResult && regionResult && phyResult) {

        var formData = $(this).serialize();
        $.ajax({
            url: '/Admin/EditShift',
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
