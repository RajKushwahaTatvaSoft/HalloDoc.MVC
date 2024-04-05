$('.admin-tab').removeClass("active");
$('#record-tab').addClass("active");

function loadSearchRecordTable(pageNo) {

    $.ajax({
        url: "/Admin/SearchRecordPartialTable",
        type: 'POST',
        data: {
            pageNo: pageNo,
            requestStatus: $('#search-record-request-status').val(),
            patientName: $('#search-record-patient-name').val(),
            requestType: $('#search-record-request-type').val(),
            phoneNumber: $('#search-record-phone-number').val(),
            fromDateOfService: $('#search-record-from-date-service').val(),
            toDateOfService: $('#search-record-to-date-service').val(),
            providerName: $('#search-record-provider-name').val(),
            patientEmail: $('#search-record-patient-email').val(),
        },
        success: function (result) {
            $('#search-record-partial-table').html(result);
        },
        complete: function () {

        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
}

$('#search-record-search-btn').click();