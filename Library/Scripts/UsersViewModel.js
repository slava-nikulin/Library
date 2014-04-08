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

    self.totalPages = ko.computed(function () {
        return Math.ceil(self.totalLines() / self.pageSize());
    });

    var getLines = function () {
        $.ajax({
            url: "/api/Users/GetUsers",
            type: "GET",
            datatype: 'json',
            data: { searchKey: self.searchKey(), pageind: self.currentPageIndex(), pagesize: self.pageSize(), currcol: self.currentColumn(), sort: self.sortType() },
        }).done(function (data) {
            self.totalLines(data.TotalLines);
            self.lines(data.ResultLines);
            return data.ResultLines;
        });
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

    self.currentPage = ko.computed(function () {
        getLines();
    }).extend({ rateLimit: 50 });

    self.nextPage = function () {
        if (((self.currentPageIndex() + 1) * self.pageSize()) < self.totalLines()) {
            self.currentPageIndex(self.currentPageIndex() + 1);
        } else {
            self.currentPageIndex(0);
        }
    };

    self.previousPage = function () {
        if (self.currentPageIndex() > 0) {
            self.currentPageIndex(self.currentPageIndex() - 1);
        } else {
            self.currentPageIndex((Math.ceil(self.totalLines() / self.pageSize())) - 1);
        }
    };

    self.changePageSize = function (data, event) {
        var newPageSize = parseInt($(event.target).attr("data-size"), 10);
        if (((self.currentPageIndex()) * newPageSize) >= self.totalLines()) {
            self.currentPageIndex(0).pageSize(newPageSize);
        } else {
            self.pageSize(newPageSize);
        }
    };

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

    self.sortTable = function (data, event) {
        var sortBy = event.target.nodeName.toLowerCase() == "i" ?
            $(event.target).parent().attr("data-model-property") :
            $(event.target).attr("data-model-property");

        if (sortBy == "do-not-sort") {
            return;
        }

        if (self.currentColumn() == sortBy) {
            if (self.sortType() == 'asc') {
                self.iconType('glyphicon glyphicon-chevron-down');
                self.sortType('desc').currentColumn(sortBy);
            } else {
                self.iconType('glyphicon glyphicon-chevron-up');
                self.sortType('asc').currentColumn(sortBy);
            }
        } else {
            self.sortType("asc").currentColumn(sortBy);
            self.iconType('glyphicon glyphicon-chevron-up');
        }
    };

    self.issueBook = function(user) {
        alert("Not implemented yet!");
    };
};