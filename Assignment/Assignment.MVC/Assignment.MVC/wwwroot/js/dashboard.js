$(document).ready(function () {
    loadPartialTable(1);
});

let search_filter = "";
let page_size = 5;

let searchTimer;
let patientIntlInput;
let patientPhoneInputElement;

$("#dashboard-patient-search").on("keyup", function (e) {
    search_filter = $(this).val().toLowerCase();

    clearTimeout(searchTimer);

    if (e.key === 'Enter' || e.keyCode === 13) {
        loadPartialTable(1)
    }
    else {
        searchTimer = setTimeout(function () {
            loadPartialTable(1)
        }, 1000);
    }
});

function loadPartialTable(pageNo) {

    $.ajax({
        url: "/Home/LoadDashboardPartialTable",
        data: { pageNo: pageNo, searchFilter : search_filter, pageSize : page_size },
        type: 'GET',
        success: function (result) {
            $('#dashboard-partial-table-div').html(result);
        },
        error: function (error) {
            console.log(error);
            alert('Error Fetching Data')
        },
    });
}

$('#add-patient-btn').click(function () {
    $.ajax({
        url: "/Home/LoadAddPatientModal",
        data: {},
        type: 'GET',
        success: function (result) {
            $('#modal-div').html(result);
            $('#addPatientModal').modal('show');
        },
        error: function (error) {
            console.log(error);
            alert('Error Fetching Data')
        },
    });
});

