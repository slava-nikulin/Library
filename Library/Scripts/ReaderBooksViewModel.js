
var BasicUserBookModel = function () {
    this.BookId = 0;
    this.EndDate = "";
    this.LibraryUserId = 0;
    this.EndDate = "";
};

var ReaderBooksViewModel = function () {
    var self = this;
    self.lines = ko.observableArray();
    self.iconType = ko.observable("");
    self.currentPage = ko.observable();
    self.isNewLine = ko.observable(true);
    self.selectedLine = ko.observable();

    self.searchKey = ko.observable("");
    self.searchByKey = function () {
        self.searchKey($("#search-borrow-book-input").val());
    };

    self.pagingSorting = {};
    self.pagingSorting.parameters = {
        PageSize: ko.observable(10),
        CurrentPageIndex: ko.observable(0),
        SortType: ko.observable("asc"),
        CurrentColumn: ko.observable("")
    };
    self.pagingSorting.totalLines = ko.observable(0);
    self.pagingSorting.totalPages = ko.computed(function () {
        return Math.ceil(self.pagingSorting.totalLines() / self.pagingSorting.parameters.PageSize());
    });
    self.pagingSorting.previousPage = function () {
        if (self.pagingSorting.parameters.CurrentPageIndex() > 0) {
            self.pagingSorting.parameters.CurrentPageIndex(self.pagingSorting.parameters.CurrentPageIndex() - 1);
        } else {
            self.pagingSorting.parameters.CurrentPageIndex((Math.ceil(self.pagingSorting.totalLines() / self.pagingSorting.parameters.PageSize())) - 1);
        }
    };
    self.pagingSorting.nextPage = function () {
        if (((self.pagingSorting.parameters.CurrentPageIndex() + 1) * self.pagingSorting.parameters.PageSize()) < self.pagingSorting.totalLines()) {
            self.pagingSorting.parameters.CurrentPageIndex(self.pagingSorting.parameters.CurrentPageIndex() + 1);
        } else {
            self.pagingSorting.parameters.CurrentPageIndex(0);
        }
    };
    self.pagingSorting.changePageSize = function (data, event) {
        var newPageSize = parseInt($(event.target).attr("data-size"), 10);
        if (((self.pagingSorting.parameters.CurrentPageIndex()) * newPageSize) >= self.pagingSorting.totalLines()) {
            self.pagingSorting.parameters.CurrentPageIndex(0).PageSize(newPageSize);
        } else {
            self.pagingSorting.parameters.PageSize(newPageSize);
        }
    };

    self.pagingSorting.sortTable = function (data, event) {
        var sortBy = event.target.nodeName.toLowerCase() == "i" ?
            $(event.target).parent().attr("data-model-property") :
            $(event.target).attr("data-model-property");

        if (sortBy == "do-not-sort") {
            return;
        }

        if (self.pagingSorting.parameters.CurrentColumn() == sortBy) {
            if (self.pagingSorting.parameters.SortType() == 'asc') {
                self.iconType('glyphicon glyphicon-chevron-down');
                self.pagingSorting.parameters.SortType('desc').CurrentColumn(sortBy);
            } else {
                self.iconType('glyphicon glyphicon-chevron-up');
                self.pagingSorting.parameters.SortType('asc').CurrentColumn(sortBy);
            }
        } else {
            self.pagingSorting.parameters.SortType("asc").CurrentColumn(sortBy);
            self.iconType('glyphicon glyphicon-chevron-up');
        }
    };

    self.getLines = function () {
        $.ajax({
            url: "/api/BorrowingBooks/GetBooks?paging=" + ko.toJSON(self.pagingSorting.parameters) + '&search=' + self.searchKey(),
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.pagingSorting.totalLines(data.TotalLines);
            self.lines(data.ResultLines);
            return data.ResultLines;
        });
    };

    self.currentPage = ko.computed(function () {
        self.getLines();
    }).extend({ rateLimit: 50 });


    self.getSetUnselectableDates = function (data) {
        self.selectedLine(data);
        $.ajax({
            url: "/api/BorrowingBooks/GetBookBorrowingData?bookId=" + data.BookId,
            type: "GET",
            datatype: 'json'
        }).done(function (disabledDates) {
            var a = disabledDates;
            $("#datepickerTo").datepicker({
                dateFormat: 'dd-mm-yy',
                beforeShowDay: function (date) {
                    var string = jQuery.datepicker.formatDate('dd-mm-yy', date);
                    return [disabledDates.indexOf(string) == -1];
                }
            });
            $("#datepickerFrom").datepicker({
                dateFormat: 'dd-mm-yy',
                beforeShowDay: function (date) {
                    var string = jQuery.datepicker.formatDate('dd-mm-yy', date);
                    return [disabledDates.indexOf(string) == -1];
                }
            });
        });
    };

    self.reserveBook = function (formElement) {
        if ($("#borrow-book-form").valid()) {
            $.post("/api/BorrowingBooks/ReserveTheBook", $(formElement).serialize(), null, "json")
            .done(function (success) {
                if (success == true) {
                    $("#borrow-book-modal").modal('hide');
                    formElement.reset();
                    self.getUserBookCollection();
                } else {
                    alert("Inputed dates are incorrect, sorry.");
                }
            });
        }
    };

    self.getUserBookCollection = function () {
        $.ajax({
            url: "/api/BorrowingBooks/GetUserBooks",
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.userBookCollection(data);
        });
    };

    self.userBookCollection = ko.observableArray();
};