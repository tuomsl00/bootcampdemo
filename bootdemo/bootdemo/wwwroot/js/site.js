// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var k;
$(function () {
    $("#search").keypress((event) => {
        if (event.which == 32 && $("#search").val().length > 3) {
            searchForResults();
        }
        clearTimeout(k);
        if (event.which != 32) {
            k = setTimeout(() => searchForResults(), 2000);
        }
    });
});


function renderResult(field) {

    return `
                <div class="col-sm-12">
                <h4>`+ field["Title"] + `</h4>
                <p>`+ field["Description"] + `</p>
                <a href="`+ field["Url"] + `">` + field["Url"] + `</a>
                <p><small>Author: `+ field["Author"] + ` at ` + field["PublishedAt"] + `</small></p>
                </div>
            `;

}

function renderWords(i, field) {
    return `
                <div class="col">
                    `+ field + `
                </div>
            `;
}

function searchForResults() {
    $("#results").empty();
    var pubDates = [];
    $.getJSON("/Home/searchResults?searchTerm=" + $("#search").val().trim(), (result) => {
        $.each(result, (i, field) => {
            $("#results").append(renderResult(field));
            pubDates.push(field["PublishedAt"]);
        });
    }).done(() => searchForResultsNoCache(pubDates.sort().pop()));
}


function searchForResultsNoCache(latestDate) {

    $.getJSON("/Home/searchResults?searchTerm=" + $("#search").val().trim() + "&date=" + latestDate, (result) => {
        $.each(result, (i, field) => {
            $("#results").append(renderResult(field));
        });
    }).done(() => searchForWords()).
        fail(function (jqxhr, textStatus, error) {
            var err = textStatus + ", " + error;
            alert("error: " + jqxhr);
    });
    
}

function searchForWords() {
    $("#words").empty();
    console.log($("#search").val().trim());
    $.getJSON("/Home/searchWord/" + $("#search").val().trim(), (result) => {
        $.each(result, (i, field) => {
            $("#words").append(renderWords(i, field));
        });
    });
}
