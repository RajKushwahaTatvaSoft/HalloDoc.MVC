
$('.admin-tab').removeClass("active");
$('#dashboard-tab').addClass("active");

var dashboardStatus = 0;
var type_filter = 0;
var search_filter = "";

$(".status-tab").click(function () {

    $(this).addClass("active");
    $(this).children("svg").attr('display', 'block');

    $('.status-tab').not(this).children("svg").attr('display', 'none');
    $('.status-tab').not(this).removeClass("active");

    var id = $(this).attr('id');

    if (id == 'status-new-tab') {
        dashboardStatus = 1;
        $('#status-text').text('(New)');
    }
    else if (id == 'status-pending-tab') {
        dashboardStatus = 2;
        $('#status-text').text('(Pending)');
    }
    else if (id == 'status-active-tab') {
        dashboardStatus = 3;
        $('#status-text').text('(Active)');
    }
    else if (id == 'status-conclude-tab') {
        dashboardStatus = 4;
        $('#status-text').text('(Conclude)');
    }
    else if (id == 'status-to-close-tab') {
        dashboardStatus = 5;
        $('#status-text').text('(To Close)');
    }
    else if (id == 'status-unpaid-tab') {
        dashboardStatus = 6;
        $('#status-text').text('(Unpaid)');
    }

    $.ajax({
        url: "/admin/partialtable",
        type: 'POST',
        data: { status: dashboardStatus, page: 1, typeFilter: 0, searchFilter: "" },
        success: function (result) {
            $('#partial-table').html(result);
        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
});

$("#status-new-tab").click();

// $("#search-filter").on("keyup", function () {
//     var value = $(this).val().toLowerCase();
//     $("#dashboard-table-body tr").filter(function () {
//         $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
//     });
// });


$("#search-filter").on("keyup", function () {
    var searchValue = $(this).val().toLowerCase();
    search_filter = searchValue;
});

$('.filter-options').click(function () {

    $(this).addClass("active-filter");
    $('.filter-options').not(this).removeClass("active-filter");

    var id = $(this).attr('id');
    if (id == 'filter-all') {
        type_filter = 0;
    }
    else if (id == 'filter-patient') {
        type_filter = 2;
    }
    else if (id == 'filter-friend') {
        type_filter = 3;
    }
    else if (id == 'filter-business') {
        type_filter = 1;
    }
    else if (id == 'filter-concierge') {
        type_filter = 4;
    }
    else if (id == 'filter-vip') {
        type_filter = 5;
    }

    $.ajax({
        url: "/admin/partialtable",
        type: 'POST',
        data: { status: dashboardStatus, page: 1, typeFilter: type_filter, searchFilter: search_filter },
        success: function (result) {
            $('#partial-table').html(result);
        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
});

function loadNextPage(currentpage) {

    $.ajax({
        url: "/admin/LoadNextPage",
        type: 'POST',
        data: { status: dashboardStatus, page: currentpage, typeFilter: type_filter, searchFilter: search_filter },
        success: function (result) {
            $('#partial-table').html(result);
        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
}

function loadPreviousPage(currentpage) {

    $.ajax({
        url: "/admin/LoadPreviousPage",
        type: 'POST',
        data: { status: dashboardStatus, page: currentpage, typeFilter: type_filter, searchFilter: search_filter },
        success: function (result) {
            $('#partial-table').html(result);
        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
}
