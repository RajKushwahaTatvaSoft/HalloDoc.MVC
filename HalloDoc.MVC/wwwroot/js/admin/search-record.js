$('.admin-tab').removeClass("active");
$('#record-tab').addClass("active");

let request_status_filter = 0;
let patient_name_filter = "";
let request_type_filter = 0;
let phone_number_filter = "";
let from_date_service_filter = null;
let to_date_service_filter = null;
let provider_name_filter = "";
let patient_email_filter = "";

function loadSearchRecordTable(pageNo) {

    $.ajax({
        url: "/Admin/SearchRecordPartialTable",
        type: 'POST',
        data: {
            pageNo: pageNo,
            requestStatus: request_status_filter,
            patientName: patient_name_filter,
            requestType: request_type_filter,
            phoneNumber: phone_number_filter,
            fromDateOfService: from_date_service_filter,
            toDateOfService: to_date_service_filter,
            providerName: provider_name_filter,
            patientEmail: patient_email_filter,
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

$('#export-data-to-excel').click(function () {
    let searchRecordExportUrl = "https://localhost:7161/Admin/ExportSearchRecordToExcel?patientName=" + patient_name_filter +
        "&requestStatus=" + request_status_filter +
        "&requestType=" + request_type_filter +
        "&phoneNumber=" + phone_number_filter +
        "&fromDateOfService=" + from_date_service_filter +
        "&toDateOfService=" + to_date_service_filter +
        "&providerName=" + provider_name_filter +
        "&patientEmail=" + patient_email_filter;
    console.log(searchRecordExportUrl);

    location.href = searchRecordExportUrl;

});

$('#search-record-search-btn').click(function () {
    request_status_filter = $('#search-record-request-status').val();
    patient_name_filter = $('#search-record-patient-name').val();
    request_type_filter = $('#search-record-request-type').val();
    phone_number_filter = $('#search-record-phone-number').val();
    from_date_service_filter = $('#search-record-from-date-service').val();
    to_date_service_filter = $('#search-record-to-date-service').val();
    provider_name_filter = $('#search-record-provider-name').val();
    patient_email_filter = $('#search-record-patient-email').val();

    loadSearchRecordTable(1);
});

$('#search-record-search-btn').click();