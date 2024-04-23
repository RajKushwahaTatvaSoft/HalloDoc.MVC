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

    let regionResult = validateRequired(document.getElementById("add-shift-region-list"));
    let phyResult = validateRequired(document.getElementById("add-shift-physician-list"));
    let shiftDateResult = validateShiftDate("add-shift-date-input");
    let startTimeResult = validateStartTime("add-shift-start-time", "add-shift-date-input");
    let endTimeResult = validateEndTime("add-shift-end-time", "add-shift-start-time");

    if (shiftDateResult && startTimeResult && endTimeResult && regionResult && phyResult) {
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
