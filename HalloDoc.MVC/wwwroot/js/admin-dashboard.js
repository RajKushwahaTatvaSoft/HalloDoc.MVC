$('.admin-tab').removeClass("active");
$('#dashboard-tab').addClass("active");

var dashboardStatus = 0;
var type_filter = 0;
var region_filter = 0;
var search_filter = "";

//Pagination And Backend Filters

$("#filter-regions").change(function () {
    region_filter = $('option:selected', this).val();
    loadPage();
});


$(".status-tab").click(function () {

    $(this).addClass("active");
    $(this).children("svg").attr('display', 'block');

    $('.status-tab').not(this).children("svg").attr('display', 'none');
    $('.status-tab').not(this).removeClass("active");

    var id = $(this).attr('id');

    if (id == 'status-new-tab') {
        dashboardStatus = 1;
        $('#status-text').text('(New)');
        $("#status-text").css("color", "#203f9a");
    }
    else if (id == 'status-pending-tab') {
        dashboardStatus = 2;
        $('#status-text').text('(Pending)');
        $("#status-text").css("color", "#00adef");
    }
    else if (id == 'status-active-tab') {
        dashboardStatus = 3;
        $('#status-text').text('(Active)');
        $("#status-text").css("color", "#228c20");
    }
    else if (id == 'status-conclude-tab') {
        dashboardStatus = 4;
        $('#status-text').text('(Conclude)');
        $("#status-text").css("color", "#da0f82");
    }
    else if (id == 'status-to-close-tab') {
        dashboardStatus = 5;
        $('#status-text').text('(To Close)');
        $("#status-text").css("color", "#0370d7");
    }
    else if (id == 'status-unpaid-tab') {
        dashboardStatus = 6;
        $('#status-text').text('(Unpaid)');
        $("#status-text").css("color", "#9966cd");
    }

    applyFilters();
});



var searchTimer;

$("#search-filter").on("keyup", function () {
    var searchValue = $(this).val().toLowerCase();
    search_filter = searchValue;

    clearTimeout(searchTimer);
    searchTimer = setTimeout(function () {
        applyFilters();
    }, 500);

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

    applyFilters();

});

function loadNextPage(currentpage) {
    loadPage(currentpage + 1);
}

function loadPreviousPage(currentpage) {
    loadPage(currentpage - 1);
}

function applyFilters() {
    loadPage(1);
}

function loadPageWithStatus(pageNo, status) {

    if (status == 1) {
        $('#status-new-tab').click();
    }
    else if (status == 2) {
        $('#status-pending-tab').click();
    }
    else if (status == 3) {
        $('#status-active-tab').click();
            }
    else if (status == 4) {
        $('#status-conclude-tab').click();

    }
    else if (status == 5) {
        $('#status-to-close-tab').click();

    }
    else if (status == 6) {
        $('#status-unpaid-tab').click();
    }

    loadPage(pageNo);
}

function loadPage(pageNo) {

    let loading_div = document.getElementById('loading-animation-div');
    loading_div.setAttribute('style', 'display:static !important;margin-top:200px;');

    $.ajax({
        url: "/Admin/PartialTable",
        type: 'POST',
        data: { status: dashboardStatus, page: pageNo, typeFilter: type_filter, searchFilter: search_filter, regionFilter: region_filter },
        success: function (result) {
            $('#partial-table').html(result);
        },
        complete: function () {
            loading_div.setAttribute('style', 'display:none !important;margin-top:200px;');
        },
        error: function (error) {
            console.log(error);
            alert('error fetching details')
        },
    });
}
