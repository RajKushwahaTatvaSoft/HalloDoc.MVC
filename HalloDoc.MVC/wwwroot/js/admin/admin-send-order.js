$("#professionTypeList").change(function () {

    var type = $('option:selected', this).val();

    $('#businessFaxNumber').val("");
    $('#businessContact').val("");
    $('#businessEmail').val("");

    $.ajax({
        url: '/Guest/GetBusinessByType',
        type: 'POST',
        data: {
            professionType: type,
        },
        success: function (result) {

            $('#businessList').empty();
            $('#businessList').append($('<option>', {
                value: 0,
                text: '-- Select Business --',
                selected: true,
                disabled: true
            }));
            $.each(result, function (index, object) {

                $('#businessList').append($('<option>', {
                    value: object["businessId"],
                    text: object["businessName"]
                }));

            });
        },
        error: function (err) {
            console.error(err);
        }
    });
});

$('#businessList').change(function () {
    var vendorId = $('option:selected', this).val();

    console.log("business: " + vendorId);
    $.ajax({
        url: '/Guest/GetBusinessDetailsById',
        type: 'POST',
        data: {
            vendorId: vendorId,
        },
        success: function (result) {
            $('#businessFaxNumber').val(result["faxnumber"]);
            $('#businessContact').val(result["phonenumber"]);
            $('#businessEmail').val(result["email"]);
        },
        error: function (err) {
            console.error(err);
        }
    });
});