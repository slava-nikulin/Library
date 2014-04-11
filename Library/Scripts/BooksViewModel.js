var BaseModel = function () {
    this.Author = "";
    this.BookId = 0;
    this.Description = "";
    this.ISBN = "";
    this.Name = "";
};

var BooksViewModel = function () {
    var self = this;
    self.lines = ko.observableArray();
    self.iconType = ko.observable("");
    self.searchKey = ko.observable("");
    self.currentPage = ko.observable();
    self.isNewLine = ko.observable(true);
    self.selectedLine = ko.observable();
    self.selectedIssuedBook = ko.observable();

    self.searchByKey = function () {
        self.searchKey($("#search-book-input").val());
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

    var getLines = function () {
        $.ajax({
            url: "/api/LibraryBooks/GetBooks?paging=" + ko.toJSON(self.pagingSorting.parameters) + '&search=' + self.searchKey(),
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.pagingSorting.totalLines(data.TotalLines);
            self.lines(data.ResultLines);
            return data.ResultBooks;
        });
    };

    self.saveLine = function (formElement) {
        $(formElement).validate();
        if ($(formElement).valid()) {
            $.post("/api/LibraryBooks/SaveBook", $(formElement).serialize(), null, "json")
            .done(function () {
                $("#add-book-modal").modal('hide');
                formElement.reset();
                getLines();
            });
        }
    };

    self.currentPage = ko.computed(function () {
        getLines();
    }).extend({ rateLimit: 50 });

    self.editLine = function (data) {
        var d;
        if (data) {
            d = data;
            self.isNewLine(false);
        } else {
            d = new BaseModel();
            self.isNewLine(true);
        }
        self.selectedLine(d);
    };

    self.deleteLine = function (bookData) {
        if (confirm("Are you sure you want tot delete this book? \n " + bookData.Name)) {
            var bookId = bookData.BookId;
            if (bookId) {
                $.ajax({
                    url: '/api/LibraryBooks/DeleteBook/' + bookId,
                    type: 'DELETE',
                }).done((function () {
                    if (self.pagingSorting.totalLines() - 1 == 0) {
                        getLines();
                    } else {
                        if (((self.pagingSorting.currentPageIndex()) * self.pagingSorting.pageSize()) >= self.pagingSorting.totalLines() - 1) {
                            self.pagingSorting.currentPageIndex(0);
                        } else {
                            getLines();
                        }
                    }
                }));
            }
            return true;
        } else {
            return false;
        }
    };


    self.getUserBookCollection = function () {
        $.ajax({
            url: "/api/LibraryBooks/GetUserBooks",
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.userBookCollection(data);
        });
    };

    self.userBookCollection = ko.observableArray();

    self.issueTheBook = function(data) {
        $.ajax({
            url: "/api/LibraryBooks/ChangeBookStatus?bookId=" + data.BookId + "&newSatus=2",
            type: "POST",
            datatype: 'json'
        }).done(function () {
            self.getUserBookCollection();
        });
    };

    self.returnTheBook = function () {
        if (self.selectedIssuedBook().Status == '2') {
            $("#custom-validation").html("Please, select new status");
        } else {
            $.ajax({
                url: "/api/LibraryBooks/ReturnTheBook?bookId=" + self.selectedIssuedBook().BookId + "&userId="
                    + self.selectedIssuedBook().UserId + "&newSatus=" + self.selectedIssuedBook().Status,
                type: "POST",
                datatype: 'json'
            }).done(function () {
                $("#ret-book-modal").modal('hide');
                self.getUserBookCollection();
            });
        }
    };
};