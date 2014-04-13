var BaseModel = function () {
    this.LibraryUserId = 0;
    this.UserName = "";
    this.Email = "";
};

var UsersViewModel = function () {
    var self = this;
    self.lines = ko.observableArray();
    self.sortType = ko.observable("asc");
    self.currentColumn = ko.observable("");
    self.iconType = ko.observable("");
    self.totalLines = ko.observable(0);
    self.searchKey = ko.observable("");
    self.currentPage = ko.observable();
    self.pageSize = ko.observable(10);
    self.currentPageIndex = ko.observable(0);
    self.isNewLine = ko.observable(true);
    self.selectedLine = ko.observable();

    self.searchByKey = function () {
        self.searchKey($("#search-user-input").val());
    };

    self.saveLine = function (formElement) {
        $(formElement).validate();
        if ($(formElement).valid()) {
            $.post("/api/Users/SaveUser", $(formElement).serialize(), null, "json")
            .done(function () {
                $("#add-user-modal").modal('hide');
                formElement.reset();
                getLines();
            });
        }
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
            url: "/api/Users/GetUsers?paging=" + ko.toJSON(self.pagingSorting.parameters) + '&search=' + self.searchKey(),
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.pagingSorting.totalLines(data.TotalLines);
            self.lines(data.ResultLines);
            return data.ResultLines;
        });
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

    self.deleteLine = function (line) {
        if (confirm("Are you sure you want tot delete this user? \n " + line.UserName)) {
            var lineId = line.LibraryUserId;
            if (lineId) {
                $.ajax({
                    url: '/api/Users/DeleteUser/' + lineId,
                    type: 'DELETE',
                }).done((function () {
                    if (self.totalLines() - 1 == 0) {
                        getLines();
                    } else {
                        if (((self.currentPageIndex()) * self.pageSize()) >= self.totalLines() - 1) {
                            self.currentPageIndex(0);
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

    self.userReservedBooks = ko.observableArray();

    self.showInfo = function (user) {
        $.ajax({
            url: "/api/Users/GetUserInfo?userId=" + user.LibraryUserId,
            type: "GET",
            datatype: 'json'
        }).done(function (data) {
            self.userReservedBooks(data);
        });
    };
};